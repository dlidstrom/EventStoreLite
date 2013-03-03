using System.Reflection;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using EventStoreLite.IoC.Castle;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Embedded;

namespace EventStoreLite.Test
{
    [TestFixture]
    public class EventStoreSession_Separator : TestBase
    {
        [Test]
        public void RespectRavenConventions()
        {
            // Arrange
            var container = new WindsorContainer();
            container.Register(Component.For<IDocumentStore>().UsingFactoryMethod(CreateDocumentStore));
            container.Register(
                Component.For<IDocumentSession>().UsingFactoryMethod(k => k.Resolve<IDocumentStore>().OpenSession()).LifestyleTransient());
            container.Install(EventStoreInstaller.FromAssembly(Assembly.GetExecutingAssembly()));
            var eventStore = container.Resolve<EventStore>();

            // Act
            var eventStoreSession = eventStore.OpenSession(
                container.Resolve<IDocumentStore>(), container.Resolve<IDocumentSession>());
            var aggregate = new Aggregate();
            eventStoreSession.Store(aggregate);
            eventStoreSession.SaveChanges();

            // Assert
            Assert.That(aggregate.Id, Is.EqualTo("EventStreams-Aggregates-1"));
        }

        private static IDocumentStore CreateDocumentStore(IKernel k)
        {
            var documentStore = new EmbeddableDocumentStore { RunInMemory = true }.Initialize();
            documentStore.Conventions.IdentityPartsSeparator = "-";
            return documentStore;
        }
    }
}
