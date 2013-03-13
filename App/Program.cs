using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using EventStoreLite;
using EventStoreLite.IoC.Castle;
using Raven.Client;
using Raven.Client.Document;
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
            using (var childContainer = CreateChildContainer(container))
            {
                WithEventStoreSession(container, x => { });
                // rebuild read models
                var windsorServiceLocator = new WindsorServiceLocator(childContainer);
                WithEventStore(container, x => EventStore.ReplayEvents(windsorServiceLocator));

                container.RemoveChildContainer(childContainer);
                return;

                // query the view model
                WithSession(container, ShowNames);

                // rebuild read models
                WithEventStore(container, x => EventStore.ReplayEvents(new WindsorServiceLocator(container.GetChildContainer("ReplayEvents"))));

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

        private static readonly Random random = new Random();
        private static void CreateDomainObject(IEventStoreSession session)
        {
            /*var existingCustomer = session.Load<Customer>("EventStreams/Customers/1");
            if (existingCustomer != null)
                existingCustomer.PrintName(Console.Out);
            else*/
            {
                var n = random.Next(10);
                for (int i = 0; i < n; i++)
                {
                    var customer = new Customer("Daniel Lidström" + random.Next(5));
                    customer.ChangeName("Per Daniel Lidström" + random.Next(20, 40));
                    session.Store(customer);
                    if (random.Next(3) > 1)
                        session.SaveChanges();
                }
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
            container.Register(documentStoreComponent, sessionComponent);
            container.Install(
                EventStoreInstaller.FromAssembly(typeof(CustomerInitialized).Assembly));
            return container;
        }

        private static IWindsorContainer CreateChildContainer(IWindsorContainer parent)
        {
            var sessionComponent =
                Component.For<IDocumentSession>()
                         .UsingFactoryMethod(
                             kernel => kernel.Resolve<IDocumentStore>().OpenSession())
                         .LifestyleTransient();
            var container = new WindsorContainer();
            container.Register(sessionComponent);
            parent.AddChildContainer(container);
            return container;
        }

        private static IDocumentStore CreateDocumentStore()
        {
            //return new EmbeddableDocumentStore { RunInMemory = true }.Initialize();
            return new DocumentStore { Url = "http://localhost:8082", DefaultDatabase = "App" }.Initialize();
        }
    }
}
