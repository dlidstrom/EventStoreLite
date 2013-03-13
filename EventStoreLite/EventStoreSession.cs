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
        private int currentChangeSequence;

        public EventStoreSession(IDocumentStore documentStore, IDocumentSession documentSession, EventDispatcher dispatcher)
        {
            if (documentStore == null) throw new ArgumentNullException("documentStore");
            if (documentSession == null) throw new ArgumentNullException("documentSession");
            if (dispatcher == null) throw new ArgumentNullException("dispatcher");

            this.documentStore = documentStore;
            this.documentSession = documentSession;
            this.dispatcher = dispatcher;
            this.currentChangeSequence = this.GenerateCommitSequence();
        }

        public TAggregate Load<TAggregate>(string id) where TAggregate : AggregateRoot
        {
            if (id == null) throw new ArgumentNullException("id");

            EventStreamAndAggregateRoot unitOfWorkInstance;
            if (entitiesByKey.TryGetValue(id, out unitOfWorkInstance))
                return (TAggregate)unitOfWorkInstance.AggregateRoot;
            var stream = this.documentSession.Load<EventStream>(id);
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
                this.unitOfWork.Add(eventStreamAndAggregateRoot);
                this.entitiesByKey.Add(id, eventStreamAndAggregateRoot);
                return instance;
            }

            return null;
        }

        public void Store(AggregateRoot aggregate)
        {
            if (aggregate == null) throw new ArgumentNullException("aggregate");

            var eventStream = new EventStream();
            this.GenerateId(eventStream, aggregate);
            this.documentSession.Store(eventStream);
            aggregate.SetId(eventStream.Id);
            var eventStreamAndAggregateRoot = new EventStreamAndAggregateRoot(eventStream, aggregate);
            this.unitOfWork.Add(eventStreamAndAggregateRoot);
            this.entitiesByKey.Add(eventStream.Id, eventStreamAndAggregateRoot);
        }

        private void GenerateId(EventStream eventStream, AggregateRoot aggregate)
        {
            var typeTagName = documentStore.Conventions.GetTypeTagName(aggregate.GetType());
            var id = this.eventStreamsHiLoKeyGenerator.GenerateDocumentKey(
                this.documentStore.DatabaseCommands, this.documentStore.Conventions, eventStream);
            var identityPartsSeparator = this.documentStore.Conventions.IdentityPartsSeparator;
            var lastIndexOf = id.LastIndexOf(identityPartsSeparator, StringComparison.Ordinal);
            eventStream.Id = string.Format(
                "EventStreams{2}{0}{2}{1}", typeTagName, id.Substring(lastIndexOf + 1), identityPartsSeparator);
        }

        private int GenerateCommitSequence()
        {
            var id = this.changeSequenceHiLoKeyGenerator.GenerateDocumentKey(
                this.documentStore.DatabaseCommands, this.documentStore.Conventions, null);
            var identityPartsSeparator = this.documentStore.Conventions.IdentityPartsSeparator;
            var lastIndexOf = id.LastIndexOf(identityPartsSeparator, StringComparison.Ordinal);
            return int.Parse(id.Substring(lastIndexOf + 1));
        }

        public void SaveChanges()
        {
            var aggregatesAndEvents = from entry in this.unitOfWork
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
            foreach (var aggregatesAndEvent in aggregatesAndEvents)
            {
                var pendingEvent = aggregatesAndEvent.Event;
                var eventStream = aggregatesAndEvent.EventStream;
                var asDynamic = pendingEvent.AsDynamic();
                asDynamic.SetChangeSequence(this.currentChangeSequence);
                this.dispatcher.Dispatch(pendingEvent, eventStream.Id);
                eventStream.History.Add(pendingEvent);
            }

            foreach (var aggregateRoot in unitOfWork.Select(x => x.AggregateRoot))
            {
                aggregateRoot.ClearUncommittedChanges();
            }

            this.documentSession.SaveChanges();
            this.currentChangeSequence = this.GenerateCommitSequence();
        }
    }
}