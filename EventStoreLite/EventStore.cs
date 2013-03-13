using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            if (container == null) throw new ArgumentNullException("container");
            this.container = container;
        }

        internal EventStore Initialize(IEnumerable<Type> readModelTypes)
        {
            if (readModelTypes == null) throw new ArgumentNullException("readModelTypes");

            lock (InitLock)
            {
                if (!this.initialized)
                {
                    var documentStore = (IDocumentStore)this.container.Resolve(typeof(IDocumentStore));
                    new ReadModelIndex(readModelTypes).Execute(documentStore);
                    new EventsIndex().Execute(documentStore);

                    this.initialized = true;
                }
            }

            return this;
        }

        /// <summary>
        /// Opens a new event store session.
        /// </summary>
        /// <param name="documentStore">Document store.</param>
        /// <param name="session">Document session.</param>
        /// <returns>Event store session.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public IEventStoreSession OpenSession(IDocumentStore documentStore, IDocumentSession session)
        {
            if (documentStore == null) throw new ArgumentNullException("documentStore");
            if (session == null) throw new ArgumentNullException("session");

            return new EventStoreSession(documentStore, session, new EventDispatcher(this.container));
        }

        /// <summary>
        /// Rebuilds all read models. This is a potentially lengthy operation!
        /// </summary>
        public static void ReplayEvents(IServiceLocator locator)
        {
            IDocumentStore documentStore = null;
            try
            {
                documentStore = (IDocumentStore)locator.Resolve(typeof(IDocumentStore));
                DoReplayEvents(locator, documentStore);
            }
            finally
            {
                if (documentStore != null)
                   locator.Release(documentStore);
            }
        }

        /// <summary>
        /// Migrates all store events. This will simply pass all events from all aggregates
        /// into the specified migrators. There is a chance of events coming in the wrong
        /// order, so don't rely on them coming in the order they were raised. They will,
        /// however, come in the correct order for each individual aggregate.
        /// </summary>
        /// <param name="eventMigrators">Event migrators.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void MigrateEvents(IEnumerable<IEventMigrator> eventMigrators)
        {
            if (eventMigrators == null) throw new ArgumentNullException("eventMigrators");

            // order by defined date
            eventMigrators = eventMigrators.OrderBy(x => x.DefinedOn()).ToList();

            var current = 0;
            while (true)
            {
                var session = (IDocumentSession)this.container.Resolve(typeof(IDocumentSession));
                try
                {
                    // allow indexing to take its time
                    var q =
                        session.Query<EventStream>()
                               .Customize(x => x.WaitForNonStaleResultsAsOf(DateTime.Now.AddSeconds(15)));

                    var eventStreams = q.Skip(current).Take(128).ToList();
                    if (eventStreams.Count == 0) break;
                    foreach (var eventStream in eventStreams)
                    {
                        var newHistory = new List<IDomainEvent>();
                        foreach (var domainEvent in eventStream.History)
                        {
                            var oldEvents = new List<IDomainEvent> { domainEvent };
                            foreach (var eventMigrator in eventMigrators)
                            {
                                var newEvents = new List<IDomainEvent>();
                                foreach (var migratedEvent in oldEvents)
                                {
                                    newEvents.AddRange(eventMigrator.Migrate(migratedEvent, eventStream.Id));
                                }

                                oldEvents = newEvents;
                            }

                            newHistory.AddRange(oldEvents);
                        }

                        eventStream.History = newHistory;
                    }

                    session.SaveChanges();
                    current += eventStreams.Count;
                }
                finally
                {
                    this.container.Release(session);
                }
            }
        }

        private static void DoReplayEvents(IServiceLocator locator, IDocumentStore documentStore)
        {
            // wait for indexing to complete
            WaitForIndexing(documentStore);

            // delete all read models
            documentStore.DatabaseCommands.DeleteByIndex("ReadModelIndex", new IndexQuery());

            // load all event streams and dispatch events
            var dispatcher = new EventDispatcher(locator);

            var current = 0;
            while (true)
            {
                IDocumentSession session = null;

                try
                {
                    session = (IDocumentSession)locator.Resolve(typeof(IDocumentSession));
                    var eventsQuery =
                        session.Query<EventsIndex.Result, EventsIndex>()
                               .Customize(
                                   x => x.WaitForNonStaleResultsAsOf(DateTime.Now.AddSeconds(15)))
                               .OrderBy(x => x.ChangeSequence);
                    var results = eventsQuery.Skip(current).Take(128).ToList();
                    if (results.Count == 0) break;
                    foreach (var result in eventsQuery)
                    {
                        var changeSequence = result.ChangeSequence;
                        var ids = result.Id.Select(x => x.Id);
                        var streams = session.Load<EventStream>(ids);

                        var events = from stream in streams
                                     from @event in stream.History
                                     where @event.ChangeSequence == changeSequence
                                     orderby @event.TimeStamp
                                     select new { stream.Id, Event = @event };
                        foreach (var item in events)
                        {
                            dispatcher.Dispatch(item.Event, item.Id);
                        }
                    }

                    session.SaveChanges();
                    current += results.Count;
                }
                finally
                {
                    if (session != null)
                        locator.Release(session);
                }
            }
        }

        private static void WaitForIndexing(IDocumentStore documentStore)
        {
            var indexingTask = Task.Factory.StartNew(
                () =>
                {
                    while (true)
                    {
                        var s = documentStore.DatabaseCommands.GetStatistics().StaleIndexes;
                        if (!s.Contains("ReadModelIndex"))
                        {
                            break;
                        }
                        Thread.Sleep(500);
                    }
                });
            indexingTask.Wait(15000);
        }
    }
}
