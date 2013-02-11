using System.Linq;
using System.Reflection;
using EventStoreLite.Indexes;
using Raven.Abstractions.Data;
using Raven.Client.Linq;

namespace EventStoreLite
{
    using System;
    using System.Collections.Generic;
    using Raven.Client;

    public class EventStore : IDisposable
    {
        private readonly ISet<IAggregate> unitOfWork
            = new HashSet<IAggregate>(ObjectReferenceEqualityComparer<object>.Default);

        private readonly IDocumentStore documentStore;
        private readonly IDocumentSession documentSession;
        private readonly EventDispatcher dispatcher;

        public EventStore(
            IDocumentStore documentStore,
            IDocumentSession documentSession,
            EventDispatcher dispatcher,
            Assembly assembly)
        {
            if (documentStore == null) throw new ArgumentNullException("documentStore");
            if (documentSession == null) throw new ArgumentNullException("documentSession");
            if (dispatcher == null) throw new ArgumentNullException("dispatcher");
            if (assembly == null) throw new ArgumentNullException("assembly");
            this.documentStore = documentStore;
            this.documentSession = documentSession;
            this.dispatcher = dispatcher;
            new ReadModelIndex(assembly).Execute(documentStore);
            new WriteModelIndex().Execute(documentStore);
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
            metadata.Add("AggregateRoot", true);
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
                    this.dispatcher.Dispatch(pendingEvent);
                }

                aggregate.MarkChangesAsCommitted();
            }

            this.documentSession.SaveChanges();
            this.unitOfWork.Clear();
        }

        public void RebuildReadModels()
        {
            this.documentStore.DatabaseCommands.DeleteByIndex("ReadModelIndex", new IndexQuery());

            var current = 0;
            while (true)
            {
                using (var session = documentStore.OpenSession())
                {
                    var q = session.Query<IAggregate, WriteModelIndex>();

                    RavenQueryStatistics stats;
                    var aggregates = q.Statistics(out stats).Skip(current).Take(11).ToList();
                    if (aggregates.Count == 0) break;
                    foreach (var e in aggregates.SelectMany(aggregate => aggregate.GetHistory()))
                    {
                        this.dispatcher.Dispatch(e);
                    }

                    session.SaveChanges();
                    current += aggregates.Count;
                }
            }
        }

        public void Dispose()
        {
            this.documentSession.Dispose();
        }
    }
}
