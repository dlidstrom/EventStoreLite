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
            var stream = this.documentSession.Load<EventStream>(id);
            if (stream != null)
            {
                var instance = (TAggregate)FormatterServices.GetUninitializedObject(typeof(TAggregate));
                instance.LoadFromHistory(stream.History);
                this.unitOfWork.Add(new EventStreamAndAggregateRoot(stream, instance));
                return instance;
            }

            return null;
        }

        public void Store(AggregateRoot aggregate)
        {
            var typeTagName = documentStore.Conventions.GetTypeTagName(aggregate.GetType());
            var hilo = new HiLoKeyGenerator("EventStreams", 4);
            var eventStream = new EventStream();
            var id = hilo.GenerateDocumentKey(documentStore.DatabaseCommands, documentStore.Conventions, eventStream);
            eventStream.Id = string.Format("EventStreams/{0}/{1}", typeTagName, id.Substring(id.LastIndexOf('/') + 1));
            this.documentSession.Store(eventStream);
            aggregate.SetId(eventStream.Id);
            this.unitOfWork.Add(new EventStreamAndAggregateRoot(eventStream, aggregate));
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
            }

            this.documentSession.SaveChanges();
            this.unitOfWork.Clear();
        }
    }
}