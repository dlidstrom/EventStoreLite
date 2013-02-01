using System;
using System.Collections.Generic;
using Raven.Client;

namespace RavenEventStore
{
    public class EventStore : IDisposable
    {
        private readonly ISet<IAggregate> unitOfWork
            = new HashSet<IAggregate>(ObjectReferenceEqualityComparer<object>.Default);
        private readonly IDocumentSession session;

        public EventStore(IDocumentSession session)
        {
            if (session == null) throw new ArgumentNullException("session");
            this.session = session;
        }

        public TAggregate Load<TAggregate>(ValueType id) where TAggregate : AggregateRoot<TAggregate>
        {
            var instance = this.session.Load<TAggregate>(id);
            if (instance != null)
                instance.LoadFromHistory();
            return instance;
        }

        public void Store<TAggregate>(AggregateRoot<TAggregate> aggregate) where TAggregate : class
        {
            this.unitOfWork.Add(aggregate);
            this.session.Store(aggregate);
        }

        public void Dispose()
        {
            this.session.Dispose();
        }

        public void SaveChanges()
        {
            foreach (var aggregate in this.unitOfWork)
            {
                foreach (var pendingEvent in aggregate.GetUncommittedChanges())
                {
                    if (string.IsNullOrEmpty(pendingEvent.AggregateId))
                        pendingEvent.AggregateId = aggregate.Id;

                    pendingEvent.TimeStamp = DateTimeOffset.Now;
                }

                aggregate.MarkChangesAsCommitted();
            }

            this.session.SaveChanges();

            this.unitOfWork.Clear();
        }
    }
}
