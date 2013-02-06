namespace EventStoreLite.Test
{
    using System;
    using NUnit.Framework;
    using Raven.Client.Embedded;

    [TestFixture]
    public class EventStore_Store
    {
        [Test]
        public void CanStoreAggregate()
        {
            // Arrange
            var customer = new Customer("Name of customer");

            // Act
            WithEventStore(
                session =>
                    {
                        session.Store(customer);
                        var stored = session.Load<Customer>(customer.Id);
                        // Assert
                        Assert.That(stored, Is.Not.Null);
                    });
        }

        [Test]
        public void PublishesEvents()
        {
            // Arrange
            var customer = new Customer("My name");

            var eventDispatcher = new EventDispatcher();
            string id = null;
            eventDispatcher.Register<CustomerInitialized, Customer>(x => id = x.AggregateId);

            // Act
            WithEventStore(
                eventDispatcher,
                session =>
                    {
                        session.Store(customer);
                        session.SaveChanges();
                    });

            // Assert
            Assert.That(id, Is.Not.Null);
        }

        private static void WithEventStore(EventDispatcher dispatcher, Action<EventStore> action)
        {
            using (var documentStore = new EmbeddableDocumentStore { RunInMemory = true }.Initialize())
            using (var session = new EventStore(documentStore.OpenSession(), dispatcher))
            {
                action.Invoke(session);
            }
        }

        private static void WithEventStore(Action<EventStore> action)
        {
            WithEventStore(new EventDispatcher(), action);
        }
    }
}
