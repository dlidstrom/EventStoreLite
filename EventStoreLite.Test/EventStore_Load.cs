﻿using NUnit.Framework;
using Raven.Client;

namespace EventStoreLite.Test
{
    [TestFixture]
    public class EventStore_Load : TestBase
    {
        private class AggregateCreated : Event { }
        private class AggregateChanged : Event { }

        private class Aggregate : AggregateRoot
        {
            public Aggregate()
            {
                this.ApplyChange(new AggregateCreated());
            }
            public void Change()
            {
                this.ApplyChange(new AggregateChanged());
            }
            private void Apply(AggregateChanged e)
            {
                Changed = true;
            }

            public bool Changed { get; private set; }
        }

        [Test]
        public void PlacesAggregateInUnitOfWork()
        {
            // Arrange
            var aggregate = new Aggregate();
            var container = CreateContainer();
            WithEventStore(container, session => session.Store(aggregate));

            // Act
            WithEventStore(container, session =>
                {
                    var a = session.Load<Aggregate>(aggregate.Id);
                    a.Change();
                });

            // Assert
            var documentStore = container.Resolve<IDocumentStore>();
            using (var session = documentStore.OpenSession())
            {
                var es = container.Resolve<EventStore>().OpenSession(documentStore, session);
                var a = es.Load<Aggregate>(aggregate.Id);
                Assert.That(a, Is.Not.Null);
                Assert.That(a.Changed, Is.True);
            }
        }
    }
}