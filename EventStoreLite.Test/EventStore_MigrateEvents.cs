using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace EventStoreLite.Test
{
    [TestFixture]
    public class EventStore_MigrateEvents : TestBase
    {
        private class FirstEventMigrator : IEventMigrator
        {
            public IEnumerable<IDomainEvent> Migrate(IDomainEvent @event, string aggregateId)
            {
                if (@event is AggregateCreated)
                {
                    yield return new AggregateHistory { Change = "Created1" };
                    yield return new AggregateHistory { Change = "Created2" };
                }

                if (@event is AggregateChanged)
                {
                    yield return new AggregateHistory { Change = "Changed1" };
                    yield return new AggregateHistory { Change = "Changed2" };
                }
            }

            public DateTime DefinedOn()
            {
                return new DateTime(2012, 1, 1);
            }
        }

        private class SecondEventMigrator : IEventMigrator
        {
            public IEnumerable<IDomainEvent> Migrate(IDomainEvent @event, string aggregateId)
            {
                if (@event is AggregateHistory)
                {
                    var aggregateHistory = @event as AggregateHistory;
                    if (aggregateHistory.Change.EndsWith("1")) aggregateHistory.Change += "A";
                }

                yield return @event;
            }

            public DateTime DefinedOn()
            {
                return new DateTime(2013, 1, 1);
            }
        }

        private class History : AggregateRoot
        {
            private History()
            {
                Events = new List<string>();
            }

            public List<string> Events { get; private set; }

            public void Apply(AggregateHistory e)
            {
                Events.Add(e.Change);
            }
        }

        [Test]
        public void CanMigrateEvents()
        {
            // Arrange
            var container = CreateContainer();
            var eventStore = container.Resolve<EventStore>();
            var aggregate = new Aggregate();
            aggregate.Change();
            WithEventStore(container, session => session.Store(aggregate));

            // Act
            var eventMigrators = new List<IEventMigrator>
                                     {
                                         // deliberately wrong order here
                                         new SecondEventMigrator(),
                                         new FirstEventMigrator()
                                     };
            eventStore.MigrateEvents(eventMigrators);

            // Assert
            History history = null;
            WithEventStore(container, session => history = session.Load<History>(aggregate.Id));
            Assert.That(history.Events, Is.Not.Null);
            Assert.That(history.Events.Count, Is.EqualTo(4));
            Assert.That(history.Events[0], Is.EqualTo("Created1A"));
            Assert.That(history.Events[1], Is.EqualTo("Created2"));
            Assert.That(history.Events[2], Is.EqualTo("Changed1A"));
            Assert.That(history.Events[3], Is.EqualTo("Changed2"));
        }
    }
}
