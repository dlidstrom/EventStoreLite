namespace EventStoreLite.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

        private class CustomerInitializedHandler : IEventHandler<CustomerInitialized>,
            IEventHandler<CustomerNameChanged>
        {
            public string AggregateId { get; set; }

            public void Handle(CustomerInitialized e)
            {
                AggregateId = e.AggregateId;
            }

            public void Handle(CustomerNameChanged e)
            {
            }
        }

        private static IEnumerable<Type> TypesImplementingInterface(Type desiredType)
        {
            return AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(desiredType.IsAssignableFrom);

        }
        [Test]
        public void PublishesEvents()
        {
            // Arrange
            var customer = new Customer("My name");
            var handler = new CustomerInitializedHandler();

            var eventDispatcher = new EventDispatcher();
            eventDispatcher.RegisterHandler<CustomerInitialized>(handler);
            eventDispatcher.RegisterHandler<CustomerNameChanged>(handler);

            // Act
            WithEventStore(
                eventDispatcher,
                session =>
                    {
                        session.Store(customer);
                        session.SaveChanges();
                    });

            // Assert
            Assert.That(handler.AggregateId, Is.Not.Null);
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
