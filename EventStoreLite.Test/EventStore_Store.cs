using System;
using System.Diagnostics;
using System.Linq;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using NUnit.Framework;
using Raven.Client;
using SampleDomain.Domain;

namespace EventStoreLite.Test
{
    [TestFixture]
    public class EventStore_Store : TestBase
    {
        [Test]
        public void StoresAggregateInUnitOfWork()
        {
            // Arrange
            var customer = new Customer("Name of customer");

            // Act
            WithEventStore(
                CreateContainer(new[] { typeof(CustomerInitializedHandler) }),
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
            var container = CreateContainer(new[] { typeof(CustomerInitializedHandler) });

            // Act
            WithEventStore(container, s => s.Store(customer));

            // Assert
            var session = container.Resolve<IDocumentSession>();
            var vm = session.Query<CustomerViewModel>().Customize(x => x.WaitForNonStaleResults()).SingleOrDefault();
            Assert.That(vm, Is.Not.Null);
            Debug.Assert(vm != null, "vm != null");
            Assert.That(vm.Name, Is.EqualTo("My name"));
        }

        private static void WithEventStore(
            IWindsorContainer container,
            Action<IEventStoreSession> action)
        {
            var eventStore = container.Resolve<EventStore>();
            var documentSession = container.Resolve<IDocumentSession>();
            var eventStoreSession = eventStore.OpenSession(documentSession);
            action.Invoke(eventStoreSession);

            // this will also save the document session
            eventStoreSession.SaveChanges();
        }
    }
}
