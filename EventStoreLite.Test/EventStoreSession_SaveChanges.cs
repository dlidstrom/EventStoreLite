using NUnit.Framework;
using Raven.Client;

namespace EventStoreLite.Test
{
    [TestFixture]
    public class EventStoreSession_SaveChanges : TestBase
    {
        [Test]
        public void CanSaveChangesSeveralTimes()
        {
            // Arrange
            var handler = new AggregateHandler();
            var container = CreateContainer(new[] { handler });
            var aggregate = new Aggregate();
            aggregate.Change();
            var eventStore = container.Resolve<EventStore>();
            var eventStoreSession = eventStore.OpenSession(
                container.Resolve<IDocumentStore>(), container.Resolve<IDocumentSession>());
            eventStoreSession.Store(aggregate);

            // Act
            eventStoreSession.SaveChanges();
            Assert.That(aggregate.GetUncommittedChanges().Length, Is.EqualTo(0));
            Assert.That(handler.Changes, Is.EqualTo(1));
            aggregate.Change();
            eventStoreSession.SaveChanges();

            // Assert
            Assert.That(handler.Changes, Is.EqualTo(2));
        }
    }
}
