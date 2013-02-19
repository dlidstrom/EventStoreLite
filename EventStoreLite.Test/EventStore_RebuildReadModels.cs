using NUnit.Framework;
using Raven.Client;
using SampleDomain.Domain;

namespace EventStoreLite.Test
{
    [TestFixture]
    public class EventStore_RebuildReadModels : TestBase
    {
        [Test]
        public void CleansAllExistingReadModels()
        {
            // Arrange
            var container = CreateContainer();
            var eventStore = container.Resolve<EventStore>();
            var viewModel = new CustomerViewModel();
            var documentSession = container.Resolve<IDocumentSession>();
            documentSession.Store(viewModel);
            documentSession.SaveChanges();
            documentSession.Advanced.Evict(viewModel);

            // Act
            eventStore.RebuildReadModels();

            // Assert
            Assert.That(documentSession.Load<CustomerViewModel>(viewModel.Id), Is.Null);
        }

        [Test]
        public void DispatchesAllEvents()
        {
            // Arrange
            var called = 0;
            var container = CreateContainer(new[] { new Handler { Callback = () => called ++ } });
            var eventStore = container.Resolve<EventStore>();
            var documentSession = container.Resolve<IDocumentSession>();
            var eventStoreSession = eventStore.OpenSession(documentSession);
            var aggregate = new Customer("Customer name");
            eventStoreSession.Store(aggregate);
            eventStoreSession.SaveChanges();

            // Act
            Assert.That(called, Is.EqualTo(1));
            eventStore.RebuildReadModels();

            // Assert
            Assert.That(called, Is.EqualTo(2));
        }

        private class ReadModel : IReadModel
        {
            public string Id { get; private set; }
        }

        [Test]
        public void CanCleanSeveralReadModels()
        {
            // Arrange
            var container = CreateContainer(new IEventHandler[0]);
            var eventStore = container.Resolve<EventStore>();
            var documentSession = container.Resolve<IDocumentSession>();
            documentSession.Store(new ReadModel());
            documentSession.Store(new ReadModel());
            documentSession.SaveChanges();

            // Act & Assert
            eventStore.RebuildReadModels();
        }
    }
}
