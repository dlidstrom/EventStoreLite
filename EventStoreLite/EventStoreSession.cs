using System;
using System.Collections.Generic;
using EventStoreLite.Infrastructure;
using Raven.Client;

namespace EventStoreLite
{
    internal class EventStoreSession : IEventStoreSession
    {
        private readonly HashSet<IAggregate> unitOfWork
            = new HashSet<IAggregate>(ObjectReferenceEqualityComparer<object>.Default);
        private readonly IDocumentSession documentSession;
        private readonly EventDispatcher dispatcher;

        public EventStoreSession(IDocumentSession documentSession, EventDispatcher dispatcher)
        {
            if (documentSession == null) throw new ArgumentNullException("documentSession");
            if (dispatcher == null) throw new ArgumentNullException("dispatcher");

            this.documentSession = documentSession;
            this.dispatcher = dispatcher;
        }

        public TAggregate Load<TAggregate>(string id) where TAggregate : AggregateRoot<TAggregate>
        {
            var instance = this.documentSession.Load<TAggregate>(id);
            if (instance != null)
                instance.LoadFromHistory();
            return instance;
        }

        public void Store<TAggregate>(AggregateRoot<TAggregate> aggregate) where TAggregate : class
        {
            this.unitOfWork.Add(aggregate);
            this.documentSession.Store(aggregate);
            var metadata = this.documentSession.Advanced.GetMetadataFor(aggregate);
            metadata.Add("Aggregate-Root", true);
        }

        public void SaveChanges()
        {
            foreach (var aggregate in this.unitOfWork)
            {
                foreach (var pendingEvent in aggregate.GetUncommittedChanges())
                {
                    var asDynamic = pendingEvent.AsDynamic();
                    if (string.IsNullOrEmpty(pendingEvent.AggregateId))
                        asDynamic.SetAggregateId(aggregate.Id);

                    asDynamic.SetTimeStamp(DateTimeOffset.Now);
                    this.dispatcher.Dispatch(pendingEvent);
                }

                aggregate.AsDynamic().MarkChangesAsCommitted();
            }

            this.documentSession.SaveChanges();
            this.unitOfWork.Clear();
        }
    }
}