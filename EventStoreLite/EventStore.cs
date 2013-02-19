using System;
using System.Collections.Generic;
using System.Linq;
using EventStoreLite.Indexes;
using EventStoreLite.IoC;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Linq;

namespace EventStoreLite
{
    /// <summary>
    /// Represents the event store. Use this class to create event store sessions.
    /// Typically, an instance of this class should be a singleton in your application.
    /// </summary>
    public class EventStore
    {
        private static readonly object InitLock = new object();
        private readonly IServiceLocator container;
        private bool initialized;

        internal EventStore(IServiceLocator container)
        {
            this.container = container;
        }

        internal EventStore Initialize(IEnumerable<Type> readModelTypes)
        {
            lock (InitLock)
            {
                if (!this.initialized)
                {
                    var documentStore = (IDocumentStore)this.container.Resolve(typeof(IDocumentStore));
                    new ReadModelIndex(readModelTypes).Execute(documentStore);
                    new WriteModelIndex().Execute(documentStore);
                    this.initialized = true;
                }
            }

            return this;
        }

        /// <summary>
        /// Opens a new event store session.
        /// </summary>
        /// <param name="session">Document session.</param>
        /// <returns>Event store session.</returns>
        public IEventStoreSession OpenSession(IDocumentSession session)
        {
            return new EventStoreSession(session, new EventDispatcher(this.container));
        }

        /// <summary>
        /// Rebuilds all read models. This is a potentially lengthy operation!
        /// </summary>
        public void RebuildReadModels()
        {
            var documentStore = (IDocumentStore)this.container.Resolve(typeof(IDocumentStore));
            using (var documentSession = documentStore.OpenSession())
            {
                // allow indexing to take its time
                documentSession.Query<IReadModel>("ReadModelIndex")
                               .Customize(
                                   x => x.WaitForNonStaleResultsAsOf(DateTime.Now.AddSeconds(15)))
// ReSharper disable ReturnValueOfPureMethodIsNotUsed Workaround to force indexing
                               .FirstOrDefault();
// ReSharper restore ReturnValueOfPureMethodIsNotUsed
                documentStore.DatabaseCommands.DeleteByIndex("ReadModelIndex", new IndexQuery());
            }

            var dispatcher = new EventDispatcher(this.container);
            var current = 0;
            while (true)
            {
                var session = (IDocumentSession)this.container.Resolve(typeof(IDocumentSession));
                try
                {
                    // allow indexing to take its time
                    var q =
                        session.Query<IAggregate, WriteModelIndex>()
                               .Customize(
                                   x => x.WaitForNonStaleResultsAsOf(DateTime.Now.AddSeconds(15)));

                    var aggregates = q.Skip(current).Take(128).ToList();
                    if (aggregates.Count == 0) break;
                    foreach (var e in aggregates.SelectMany(aggregate => aggregate.GetHistory()))
                    {
                        dispatcher.Dispatch(e);
                    }

                    session.SaveChanges();
                    current += aggregates.Count;
                }
                finally
                {
                    this.container.Release(session);
                }
            }
        }
    }
}
