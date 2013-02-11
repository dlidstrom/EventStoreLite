using System.Linq;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using EventStoreLite;
using Raven.Client;
using SampleDomain.Domain;
using SampleDomain.Handlers;
using SampleDomain.ViewModels;
using System;
using Raven.Client.Document;

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
                new Benchmark().Run(container);
                return;
                WithEventStore(container, CreateDomainObject);

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
                var store = container.Resolve<IDocumentStore>();
                var session = container.Resolve<IDocumentSession>();
                var eventStore = container.Resolve<EventStore>();
                action.Invoke(eventStore);
                eventStore.SaveChanges();
                session.SaveChanges();
                container.Release(eventStore);
                container.Release(session);
                container.Release(store);
            }
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
            vm.Names.ForEach(Console.WriteLine);
        }

        private static void CreateDomainObject(EventStore store)
        {
            var existingCustomer = store.Load<Customer>("customers/1");
            if (existingCustomer != null)
                existingCustomer.PrintName(Console.Out);
            else
            {
                var customer = new Customer("Daniel Lidström");
                customer.ChangeName("Per Daniel Lidström");
                store.Store(customer);
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
            var eventStoreComponent =
                Component.For<EventStore>()
                         .UsingFactoryMethod(
                             kernel =>
                             new EventStore(
                                 kernel.Resolve<IDocumentStore>(),
                                 kernel.Resolve<IDocumentSession>(),
                                 kernel.Resolve<EventDispatcher>(),
                                 typeof(CustomerHandler).Assembly))
                         .LifestyleTransient();
            var eventDispatcherComponent =
                Component.For<EventDispatcher>()
                         .UsingFactoryMethod(kernel => new EventDispatcher(kernel));
            container.Register(
                documentStoreComponent,
                sessionComponent,
                eventStoreComponent,
                eventDispatcherComponent).Install(new HandlersInstaller());
            return container;
        }

        private class HandlersInstaller : IWindsorInstaller
        {
            public void Install(IWindsorContainer container, IConfigurationStore store)
            {
                var types = typeof(Customer).Assembly.GetTypes();
                foreach (var type in types.Where(x => x.IsClass && x.IsAbstract == false))
                {
                    RegisterEventTypes(container, type);
                }
            }

            private static void RegisterEventTypes(IWindsorContainer container, Type type)
            {
                var interfaces = type.GetInterfaces();
                foreach (var i in interfaces.Where(x => x.IsGenericType))
                {
                    var genericTypeDefinition = i.GetGenericTypeDefinition();
                    if (!typeof(IEventHandler<>).IsAssignableFrom(genericTypeDefinition)) continue;
                    var genericArguments = string.Join(
                        ", ", i.GetGenericArguments().Select(x => x.ToString()));
                    var registration =
                        Component.For(i)
                                 .ImplementedBy(type)
                                 .LifestyleTransient()
                                 .Named(string.Format("{0}<{1}>", type.FullName, genericArguments));
                    container.Register(registration);
                }
            }
        }

        private static IDocumentStore CreateDocumentStore()
        {
            return new DocumentStore { Url = "http://localhost:8082" }.Initialize();
        }
    }
}
