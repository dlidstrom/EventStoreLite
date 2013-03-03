using EventStoreLite.IoC.Unity;
using Microsoft.Practices.Unity;
using NUnit.Framework;
using Raven.Client.Embedded;
using UnityServiceLocator = EventStoreLite.IoC.Unity.UnityServiceLocator;

namespace EventStoreLite.Test
{
    [TestFixture]
    public class UnityInstallerTest : InstallerTest
    {
        [Test]
        public void InstallsHandlerType()
        {
            // Arrange
            var container = new UnityContainer();
            container.RegisterInstance(new EmbeddableDocumentStore { RunInMemory = true }.Initialize());

            // Act
            var handlerTypes = new[] { typeof(AggregateHandler) };
            var extension = EventStoreContainerExtension.FromHandlerTypes(handlerTypes);
            container.AddExtension(extension);

            // Assert
            VerifyContainer(new UnityServiceLocator(container), 1);
        }

        [Test]
        public void InstallsHandlerTypes()
        {
            // Arrange
            var container = new UnityContainer();
            container.RegisterInstance(new EmbeddableDocumentStore { RunInMemory = true }.Initialize());

            // Act
            var handlerTypes = new[] { typeof(AggregateHandler), typeof(AnotherAggregateHandler) };
            var extension = EventStoreContainerExtension.FromHandlerTypes(handlerTypes);
            container.AddExtension(extension);

            // Assert
            VerifyContainer(new UnityServiceLocator(container), 2);
        }

        [Test]
        public void InstallsHandlerInstance()
        {
            // Arrange
            var container = new UnityContainer();
            container.RegisterInstance(new EmbeddableDocumentStore { RunInMemory = true }.Initialize());

            // Act
            var aggregateHandlers = new IEventHandler[] { new AggregateHandler() };
            var extension = EventStoreContainerExtension.FromHandlerInstances(aggregateHandlers);
            container.AddExtension(extension);

            // Assert
            VerifyContainer(new UnityServiceLocator(container), 1);
        }

        [Test]
        public void InstallsHandlerInstances()
        {
            // Arrange
            var container = new UnityContainer();
            container.RegisterInstance(new EmbeddableDocumentStore { RunInMemory = true }.Initialize());

            // Act
            var aggregateHandlers = new IEventHandler[] { new AggregateHandler(), new AnotherAggregateHandler() };
            var extension = EventStoreContainerExtension.FromHandlerInstances(aggregateHandlers);
            container.AddExtension(extension);

            // Assert
            VerifyContainer(new UnityServiceLocator(container), 2);
        }
    }
}