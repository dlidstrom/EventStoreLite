using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using EventStoreLite;
using EventStoreLite.IoC;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Embedded;
using SampleDomain.Domain;
using SampleDomain.ViewModels;
using System;

namespace App
{
    public static class Program
    {
        public static void Main()
        {
            try
            {
                Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void Run()
        {
            using (var container = CreateContainer())
            {
                WithEventStoreSession(container, CreateDomainObject);

                // query the view model
                WithSession(container, ShowNames);

                // rebuild read models
                WithEventStore(container, x => x.RebuildReadModels());

                // query the view model
                WithSession(container, ShowNames);
            }
        }

        private static void WithEventStore(IWindsorContainer container, Action<EventStore> action)
        {
            using (container.BeginScope())
            {
                var eventStore = container.Resolve<EventStore>();
                action.Invoke(eventStore);
            }
        }

        private static void WithEventStore(IWindsorContainer container, Action<IDocumentSession, IEventStoreSession> action)
        {
            using (container.BeginScope())
            {
                var store = container.Resolve<IDocumentStore>();
                var session = container.Resolve<IDocumentSession>();
                var eventStore = container.Resolve<EventStore>();
                var eventStoreSession = eventStore.OpenSession(store, session);
                action.Invoke(session, eventStoreSession);
                eventStoreSession.SaveChanges();
                container.Release(session);
            }
        }

        private static void WithEventStoreSession(IWindsorContainer container, Action<IEventStoreSession> action)
        {
            WithEventStore(container, (s, e) => action.Invoke(e));
        }

        private static void WithSession(
            IWindsorContainer container, Action<IDocumentSession> action)
        {
            using (container.BeginScope())
            {
                var session = container.Resolve<IDocumentSession>();
                action.Invoke(session);
                session.SaveChanges();
                container.Release(session);
            }
        }

        private static void ShowNames(IDocumentSession session)
        {
            Console.WriteLine("Changes:");
            var vm = session.Load<NamesViewModel>(NamesViewModel.DatabaseId);
            if (vm != null)
                vm.Names.ForEach(Console.WriteLine);
            else
            {
                Console.WriteLine("No names found in db");
            }
        }

        private static void CreateDomainObject(IEventStoreSession session)
        {
            var existingCustomer = session.Load<Customer>("EventStreams/Customers/5");
            if (existingCustomer != null)
                existingCustomer.PrintName(Console.Out);
            else
            {
                var customer = new Customer("Daniel Lidström");
                customer.ChangeName("Per Daniel Lidström");
                session.Store(customer);
            }
        }

        private static IWindsorContainer CreateContainer()
        {
            var container = new WindsorContainer();
            var documentStoreComponent =
                Component.For<IDocumentStore>()
                         .UsingFactoryMethod(CreateDocumentStore)
                         .LifestyleSingleton();
            var sessionComponent =
                Component.For<IDocumentSession>()
                         .UsingFactoryMethod(
                             kernel => kernel.Resolve<IDocumentStore>().OpenSession())
                         .LifestyleScoped();
            container.Register(
                documentStoreComponent,
                sessionComponent);
            container.Install(
                EventStoreInstaller.FromAssembly(typeof(CustomerInitialized).Assembly));
            return container;
        }

        private static IDocumentStore CreateDocumentStore()
        {
            //return new EmbeddableDocumentStore { RunInMemory = true }.Initialize();
            return new DocumentStore { Url = "http://localhost:8082", DefaultDatabase = "App" }.Initialize();
        }
    }
}
