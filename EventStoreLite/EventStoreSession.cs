using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using EventStoreLite.Infrastructure;
using Raven.Client;
using Raven.Client.Document;

namespace EventStoreLite
{
    internal class EventStoreSession : IEventStoreSession
    {
        private readonly Dictionary<string, EventStreamAndAggregateRoot> entitiesByKey =
            new Dictionary<string, EventStreamAndAggregateRoot>();

        private readonly HashSet<EventStreamAndAggregateRoot> unitOfWork
            = new HashSet<EventStreamAndAggregateRoot>(ObjectReferenceEqualityComparer<object>.Default);

        private readonly IDocumentStore documentStore;
        private readonly IDocumentSession documentSession;
        private readonly EventDispatcher dispatcher;
        private readonly HiLoKeyGenerator eventStreamsHiLoKeyGenerator = new HiLoKeyGenerator("EventStreams", 4);
        private readonly HiLoKeyGenerator changeSequenceHiLoKeyGenerator = new HiLoKeyGenerator("ChangeSequence", 4);

        public EventStoreSession(IDocumentStore documentStore, IDocumentSession documentSession, EventDispatcher dispatcher)
        {
            if (documentStore == null) throw new ArgumentNullException("documentStore");
            if (documentSession == null) throw new ArgumentNullException("documentSession");
            if (dispatcher == null) throw new ArgumentNullException("dispatcher");

            this.documentStore = documentStore;
            this.documentSession = documentSession;
            this.dispatcher = dispatcher;
        }

        public TAggregate Load<TAggregate>(string id) where TAggregate : AggregateRoot
        {
            if (id == null) throw new ArgumentNullException("id");

            EventStreamAndAggregateRoot unitOfWorkInstance;
            if (entitiesByKey.TryGetValue(id, out unitOfWorkInstance))
                return (TAggregate)unitOfWorkInstance.AggregateRoot;
            var stream = documentSession.Load<EventStream>(id);
            if (stream != null)
            {
                TAggregate instance;

                // attempt to call default constructor
                // if none found, create uninitialized object
                var ctor =
                    typeof(TAggregate).GetConstructor(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        Type.EmptyTypes,
                        null);
                if (ctor != null) instance = (TAggregate)ctor.Invoke(null);
                else instance = (TAggregate)FormatterServices.GetUninitializedObject(typeof(TAggregate));

                instance.LoadFromHistory(stream.History);
                var eventStreamAndAggregateRoot = new EventStreamAndAggregateRoot(stream, instance);
                unitOfWork.Add(eventStreamAndAggregateRoot);
                entitiesByKey.Add(id, eventStreamAndAggregateRoot);
                return instance;
            }

            return null;
        }

        public void Store(AggregateRoot aggregate)
        {
            if (aggregate == null) throw new ArgumentNullException("aggregate");

            var eventStream = new EventStream();
            GenerateId(eventStream, aggregate);
            documentSession.Store(eventStream);
            aggregate.SetId(eventStream.Id);
            var eventStreamAndAggregateRoot = new EventStreamAndAggregateRoot(eventStream, aggregate);
            unitOfWork.Add(eventStreamAndAggregateRoot);
            entitiesByKey.Add(eventStream.Id, eventStreamAndAggregateRoot);
        }

        public void SaveChanges()
        {
            var aggregatesAndEvents = from entry in unitOfWork
                                      let aggregateRoot = entry.AggregateRoot
                                      let eventStream = entry.EventStream
                                      from @event in aggregateRoot.GetUncommittedChanges()
                                      orderby @event.TimeStamp
                                      select
                                          new
                                          {
                                              EventStream = eventStream,
                                              Event = @event
                                          };
            var currentChangeSequence = GenerateChangeSequence();
            foreach (var aggregatesAndEvent in aggregatesAndEvents)
            {
                var pendingEvent = aggregatesAndEvent.Event;
                var eventStream = aggregatesAndEvent.EventStream;
                var asDynamic = pendingEvent.AsDynamic();
                asDynamic.SetChangeSequence(currentChangeSequence);
                dispatcher.Dispatch(pendingEvent, eventStream.Id);
                eventStream.History.Add(pendingEvent);
            }

            foreach (var aggregateRoot in unitOfWork.Select(x => x.AggregateRoot))
            {
                aggregateRoot.ClearUncommittedChanges();
            }

            documentSession.SaveChanges();
        }

        private void GenerateId(EventStream eventStream, AggregateRoot aggregate)
        {
            var typeTagName = documentStore.Conventions.GetTypeTagName(aggregate.GetType());
            var id = eventStreamsHiLoKeyGenerator.GenerateDocumentKey(
                documentStore.DatabaseCommands, documentStore.Conventions, eventStream);
            var identityPartsSeparator = documentStore.Conventions.IdentityPartsSeparator;
            var lastIndexOf = id.LastIndexOf(identityPartsSeparator, StringComparison.Ordinal);
            eventStream.Id = string.Format(
                "EventStreams{2}{0}{2}{1}", typeTagName, id.Substring(lastIndexOf + 1), identityPartsSeparator);
        }

        private int GenerateChangeSequence()
        {
            var id = changeSequenceHiLoKeyGenerator.GenerateDocumentKey(
                documentStore.DatabaseCommands, documentStore.Conventions, null);
            var identityPartsSeparator = documentStore.Conventions.IdentityPartsSeparator;
            var lastIndexOf = id.LastIndexOf(identityPartsSeparator, StringComparison.Ordinal);
            return int.Parse(id.Substring(lastIndexOf + 1));
        }
    }
}