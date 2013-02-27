using System;
using NUnit.Framework;

namespace EventStoreLite.Test
{
    [TestFixture]
    public class EventStore_LoadToAnotherAggregateRootClass : TestBase
    {
        private class Aggregate1 : AggregateRoot
        {
            public Aggregate1()
            {
                this.ApplyChange(new AggregateInitialized("some value"));
            }

            private void Apply(AggregateInitialized e)
            {
                Value = e.Value;
            }

            public string Value { get; set; }
        }

        private class Aggregate2 : AggregateRoot
        {
            private void Apply(AggregateInitialized e)
            {
                Initialized = e.Value;
            }

            public string Initialized { get; set; }
        }

        private class AggregateInitialized : Event
        {
            public AggregateInitialized(string value)
            {
                Value = value;
            }

            public string Value { get; private set; }
        }

        [Test]
        public void CanLoadToAnotherAggregateType()
        {
            // Arrange
            var container = CreateContainer();
            var aggregate1 = new Aggregate1();
            WithEventStore(container, session => session.Store(aggregate1));

            // Act
            Aggregate2 aggregate2 = null;
            WithEventStore(container, session => aggregate2 = session.Load<Aggregate2>(aggregate1.Id));

            // Assert
            Assert.That(aggregate2, Is.Not.Null);
            Assert.That(aggregate2.Initialized, Is.EqualTo(aggregate1.Value));
        }
    }
}