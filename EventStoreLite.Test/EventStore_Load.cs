using NUnit.Framework;
using Raven.Client;
using Raven.Imports.Newtonsoft.Json;

namespace EventStoreLite.Test
{
    [TestFixture]
    public class EventStore_Load : TestBase
    {
        private class AggregateCreated : Event<Aggregate> { }
        private class AggregateChanged : Event<Aggregate> { }

        private class Aggregate : AggregateRoot<Aggregate>
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

            [JsonIgnore]
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
            using (var session = container.Resolve<IDocumentStore>().OpenSession())
            {
                var es = container.Resolve<EventStore>().OpenSession(session);
                var a = es.Load<Aggregate>(aggregate.Id);
                Assert.That(a, Is.Not.Null);
                Assert.That(a.Changed, Is.True);
            }
        }
    }
}