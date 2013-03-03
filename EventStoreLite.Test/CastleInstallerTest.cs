using Castle.MicroKernel.Registration;
using Castle.Windsor;
using EventStoreLite.IoC.Castle;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Embedded;

namespace EventStoreLite.Test
{
    [TestFixture]
    public class CastleInstallerTest : InstallerTest
    {
        [Test]
        public void InstallsHandlerTypes()
        {
            // Arrange
            var container = new WindsorContainer();
            container.Register(
                Component.For<IDocumentStore>()
                         .Instance(new EmbeddableDocumentStore { RunInMemory = true }.Initialize()));
            // Act
            var handlerTypes = new[] { typeof(AggregateHandler), typeof(AnotherAggregateHandler) };
            var installer = EventStoreInstaller.FromHandlerTypes(handlerTypes);
            container.Install(installer);

            // Assert
            VerifyContainer(new WindsorServiceLocator(container), 2);
        }

        [Test]
        public void InstallsHandlerType()
        {
            // Arrange
            var container = new WindsorContainer();
            container.Register(
                Component.For<IDocumentStore>()
                         .Instance(new EmbeddableDocumentStore { RunInMemory = true }.Initialize()));
            // Act
            var handlerTypes = new[] { typeof(AggregateHandler) };
            var installer = EventStoreInstaller.FromHandlerTypes(handlerTypes);
            container.Install(installer);

            // Assert
            VerifyContainer(new WindsorServiceLocator(container), 1);
        }

        [Test]
        public void InstallsHandlerInstances()
        {
            // Arrange
            var container = new WindsorContainer();
            container.Register(
                Component.For<IDocumentStore>()
                         .Instance(new EmbeddableDocumentStore { RunInMemory = true }.Initialize()));
            // Act
            var aggregateHandlers = new IEventHandler[] { new AggregateHandler(), new AnotherAggregateHandler() };
            var installer = EventStoreInstaller.FromHandlerInstances(aggregateHandlers);
            container.Install(installer);

            // Assert
            VerifyContainer(new WindsorServiceLocator(container), 2);
        }

        [Test]
        public void InstallsHandlerInstance()
        {
            // Arrange
            var container = new WindsorContainer();
            container.Register(
                Component.For<IDocumentStore>()
                         .Instance(new EmbeddableDocumentStore { RunInMemory = true }.Initialize()));
            // Act
            var aggregateHandlers = new IEventHandler[] { new AggregateHandler() };
            var installer = EventStoreInstaller.FromHandlerInstances(aggregateHandlers);
            container.Install(installer);

            // Assert
            VerifyContainer(new WindsorServiceLocator(container), 1);
        }
    }
}