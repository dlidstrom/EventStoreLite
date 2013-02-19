using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace EventStoreLite.IoC
{
    /// <summary>
    /// Installs the event store into a Castle Windsor container.
    /// </summary>
    public class EventStoreInstaller : IWindsorInstaller
    {
        private readonly IEnumerable<IEventHandler> handlers;
        private readonly IEnumerable<Type> readModelTypes;
        private readonly IEnumerable<Type> handlerTypes;

        /// <summary>
        /// Initializes a new instance of the EventStoreInstaller class.
        /// Use this constructor to register event handler types.
        /// </summary>
        /// <param name="readModelTypes">List of read model types.</param>
        /// <param name="handlerTypes">List of event handler types.</param>
        public EventStoreInstaller(IEnumerable<Type> readModelTypes, IEnumerable<Type> handlerTypes)
        {
            if (readModelTypes == null) throw new ArgumentNullException("readModelTypes");
            if (handlerTypes == null) throw new ArgumentNullException("handlerTypes");
            this.readModelTypes = readModelTypes;
            this.handlerTypes = handlerTypes;
        }

        /// <summary>
        /// Initializes a new instance of the EventStoreInstaller class.
        /// Use this constructor to register event handler instances.
        /// </summary>
        /// <param name="readModelTypes">List of read model types.</param>
        /// <param name="handlers">List of event handler instances.</param>
        public EventStoreInstaller(IEnumerable<Type> readModelTypes, IEnumerable<IEventHandler> handlers)
        {
            if (readModelTypes == null) throw new ArgumentNullException("readModelTypes");
            if (handlers == null) throw new ArgumentNullException("handlers");
            this.readModelTypes = readModelTypes;
            this.handlers = handlers;
        }

        /// <summary>
        /// Installs the event store and the handler types or instances to the specified container.
        /// </summary>
        /// <param name="container">Container instance.</param>
        /// <param name="store">Configuration store.</param>
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<EventStore>()
                         .UsingFactoryMethod<EventStore>(x => CreateEventStore(container))
                         .LifestyleSingleton());

            if (this.handlerTypes != null)
            {
                foreach (var type in handlerTypes.Where(x => x.IsClass && x.IsAbstract == false))
                {
                    RegisterEventTypes(container, type);
                }
            }

            if (this.handlers != null)
            {
                foreach (var handler in handlers)
                {
                    RegisterEventTypes(container, handler.GetType(), handler);
                }
            }
        }

        private EventStore CreateEventStore(IWindsorContainer container)
        {
            if (handlerTypes != null)
                return new EventStore(new WindsorServiceLocator(container)).Initialize(handlerTypes);

            return new EventStore(new WindsorServiceLocator(container)).Initialize(handlers.Select(x => x.GetType()));
        }

        private static void RegisterEventTypes(IWindsorContainer container, Type type, object instance = null)
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
                             .Named(string.Format("{0}<{1}>", type.FullName, genericArguments));
                if (instance != null) registration.Instance(instance);
                else
                {
                    registration.ImplementedBy(type).LifestyleTransient();
                }

                container.Register(registration);
            }
        }
    }
}