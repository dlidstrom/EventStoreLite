using NUnit.Framework;
using Raven.Client;

namespace EventStoreLite.Test
{
    [TestFixture]
    public class EventStore_Load : TestBase
    {
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

        [Test]
        public void LoadsSecondFromUnitOfWork()
        {
            // Arrange
            var aggregate = new Aggregate();
            var container = CreateContainer();
            var eventStore = container.Resolve<EventStore>();
            var eventStoreSession = eventStore.OpenSession(
                container.Resolve<IDocumentStore>(), container.Resolve<IDocumentSession>());
            eventStoreSession.Store(aggregate);

            // Act
            var secondAggregate = eventStoreSession.Load<Aggregate>(aggregate.Id);

            // Assert
            Assert.That(secondAggregate, Is.SameAs(aggregate));
        }
    }
}