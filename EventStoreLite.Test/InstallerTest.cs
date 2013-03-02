using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using EventStoreLite.IoC.Castle;
using EventStoreLite.IoC.Unity;
using Microsoft.Practices.Unity;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Embedded;

namespace EventStoreLite.Test
{
    [TestFixture]
    public class InstallerTest
    {
        [Test]
        public void InstallsHandlerTypesUsingCastleWindsor()
        {
            // Arrange
            var container = new WindsorContainer();
            container.Register(
                Component.For<IDocumentStore>()
                         .Instance(new EmbeddableDocumentStore { RunInMemory = true }.Initialize()));
            // Act
            var installer = EventStoreInstaller.FromHandlerTypes(new[] { typeof(AggregateHandler) });
            container.Install(installer);

            // Assert
            VerifyContainer(container.Resolve);
        }

        [Test]
        public void InstallsHandlerInstancesUsingCastleWindsor()
        {
            // Arrange
            var container = new WindsorContainer();
            container.Register(
                Component.For<IDocumentStore>()
                         .Instance(new EmbeddableDocumentStore { RunInMemory = true }.Initialize()));
            // Act
            var installer = EventStoreInstaller.FromHandlerInstances(new[] { new AggregateHandler() });
            container.Install(installer);

            // Assert
            VerifyContainer(container.Resolve);
        }

        [Test]
        public void InstallsHandlerTypesUsingUnity()
        {
            // Arrange
            var container = new UnityContainer();
            container.RegisterInstance(new EmbeddableDocumentStore { RunInMemory = true }.Initialize());

            // Act
            var extension = EventStoreContainerExtension.FromHandlerTypes(new[] { typeof(AggregateHandler) });
            container.AddExtension(extension);

            // Assert
            VerifyContainer(t => container.Resolve(t));
        }

        [Test]
        public void InstallsHandlerInstancesUsingUnity()
        {
            // Arrange
            var container = new UnityContainer();
            container.RegisterInstance(new EmbeddableDocumentStore { RunInMemory = true }.Initialize());

            // Act
            var extension = EventStoreContainerExtension.FromHandlerInstances(new[] { new AggregateHandler() });
            container.AddExtension(extension);

            // Assert
            VerifyContainer(t => container.Resolve(t));
        }

        private static void VerifyContainer(Func<Type, object> resolver)
        {
            //Assert.That(resolver.Invoke(typeof(EventStore)), Is.Not.Null);
            Assert.That(resolver.Invoke(typeof(IEventHandler<AggregateChanged>)), Is.Not.Null);
        }
    }
}
