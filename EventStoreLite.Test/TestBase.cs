using System;
using System.Collections.Generic;
using System.Reflection;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using EventStoreLite.IoC;
using Raven.Client;
using Raven.Client.Embedded;

namespace EventStoreLite.Test
{
    public abstract class TestBase
    {
        protected static IWindsorContainer CreateContainer(IEnumerable<IEventHandler> handlers)
        {
            var container = RegisterRaven();
            container.Install(new EventStoreInstaller(handlers));
            return container;
        }

        protected static IWindsorContainer CreateContainer(IEnumerable<Type> types)
        {
            var container = RegisterRaven();
            container.Install(new EventStoreInstaller(types));
            return container;
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