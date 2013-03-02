using System;
using System.Collections.Generic;
using System.Reflection;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using EventStoreLite.IoC.Castle;
using Raven.Client;
using Raven.Client.Embedded;

namespace EventStoreLite.Test
{
    public abstract class TestBase
    {
        protected static IWindsorContainer CreateContainer(IEnumerable<IEventHandler> handlers)
        {
            var container = RegisterRaven();
            container.Install(EventStoreInstaller.FromHandlerInstances(handlers));
            return container;
        }

        protected static IWindsorContainer CreateContainer(IEnumerable<Type> handlerTypes)
        {
            var container = RegisterRaven();
            container.Install(EventStoreInstaller.FromHandlerTypes(handlerTypes));
            return container;
        }

        protected static void WithEventStore(
            IWindsorContainer container,
            Action<IEventStoreSession> action)
        {
            var documentStore = container.Resolve<IDocumentStore>();
            var eventStore = container.Resolve<EventStore>();
            var documentSession = container.Resolve<IDocumentSession>();
            var eventStoreSession = eventStore.OpenSession(documentStore, documentSession);
            action.Invoke(eventStoreSession);

            // this will also save the document session
            eventStoreSession.SaveChanges();
        }

        private static WindsorContainer RegisterRaven()
        {
            var container = new WindsorContainer();
            container.Register(
                Component.For<IDocumentStore>()
                         .UsingFactoryMethod(
                             () => new EmbeddableDocumentStore { RunInMemory = true }.Initialize())
                         .LifestyleSingleton());
            container.Register(
                Component.For<IDocumentSession>()
                         .UsingFactoryMethod(k => k.Resolve<IDocumentStore>().OpenSession())
                         .LifestyleSingleton());
            return container;
        }

        protected static IWindsorContainer CreateContainer()
        {
            return CreateContainer(Assembly.GetExecutingAssembly().GetTypes());
        }
    }
}