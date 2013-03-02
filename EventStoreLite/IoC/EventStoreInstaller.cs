using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private readonly IEnumerable<Type> handlerTypes;

        /// <summary>
        /// Installs event handlers from the specified assembly.
        /// </summary>
        /// <param name="assembly">Assembly with event handlers.</param>
        /// <returns>Event store installer for Castle Windsor.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static EventStoreInstaller FromAssembly(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");
            return new EventStoreInstaller(assembly.GetTypes());
        }

        /// <summary>
        /// Installs the specified event handler types.
        /// </summary>
        /// <param name="handlerTypes">Event handler types.</param>
        /// <returns>Event store installer for Castle Windsor.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static EventStoreInstaller FromHandlerTypes(IEnumerable<Type> handlerTypes)
        {
            if (handlerTypes == null) throw new ArgumentNullException("handlerTypes");
            return new EventStoreInstaller(handlerTypes);
        }

        /// <summary>
        /// Installs the specified event handler instances.
        /// </summary>
        /// <param name="handlers">Event handler instances.</param>
        /// <returns>Event store installer for Castle Windsor.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static EventStoreInstaller FromHandlerInstances(IEnumerable<IEventHandler> handlers)
        {
            if (handlers == null) throw new ArgumentNullException("handlers");
            return new EventStoreInstaller(handlers);
        }

        private EventStoreInstaller(IEnumerable<Type> handlerTypes)
        {
            if (handlerTypes == null) throw new ArgumentNullException("handlerTypes");
            this.handlerTypes = handlerTypes;
        }

        private EventStoreInstaller(IEnumerable<IEventHandler> handlers)
        {
            if (handlers == null) throw new ArgumentNullException("handlers");
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