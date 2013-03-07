using System;
using System.Collections.Generic;
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
            var stream = this.documentSession.Load<EventStream>(id);
            if (stream != null)
            {
                var instance = (TAggregate)FormatterServices.GetUninitializedObject(typeof(TAggregate));
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
            var hilo = new HiLoKeyGenerator("EventStreams", 4);
            var id = hilo.GenerateDocumentKey(this.documentStore.DatabaseCommands, this.documentStore.Conventions, eventStream);
            var identityPartsSeparator = this.documentStore.Conventions.IdentityPartsSeparator;
            var lastIndexOf = id.LastIndexOf(identityPartsSeparator, StringComparison.Ordinal);
            eventStream.Id = string.Format(
                "EventStreams{2}{0}{2}{1}", typeTagName, id.Substring(lastIndexOf + 1), identityPartsSeparator);
        }

        public void SaveChanges()
        {
            foreach (var entry in this.unitOfWork)
            {
                var aggregateRoot = entry.AggregateRoot;
                var eventStream = entry.EventStream;
                foreach (var pendingEvent in aggregateRoot.GetUncommittedChanges())
                {
                    var asDynamic = pendingEvent.AsDynamic();
                    asDynamic.SetTimeStamp(DateTimeOffset.Now);
                    this.dispatcher.Dispatch(pendingEvent, eventStream.Id);
                    eventStream.History.Add(pendingEvent);
                }

                aggregateRoot.ClearUncommittedChanges();
            }

            this.documentSession.SaveChanges();
        }
    }
}