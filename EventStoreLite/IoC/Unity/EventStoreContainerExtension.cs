using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Practices.Unity;

namespace EventStoreLite.IoC.Unity
{
    /// <summary>
    /// Installs the event store into a Unity container.
    /// </summary>
    public class EventStoreContainerExtension : UnityContainerExtension
    {
        private readonly IEnumerable<IEventHandler> handlers;
        private readonly IEnumerable<Type> handlerTypes;

        private EventStoreContainerExtension(IEnumerable<Type> handlerTypes)
        {
            this.handlerTypes = handlerTypes;
        }

        private EventStoreContainerExtension(IEnumerable<IEventHandler> handlers)
        {
            this.handlers = handlers;
        }

        /// <summary>
        /// Installs event handlers from the specified assembly.
        /// </summary>
        /// <param name="assembly">Assembly with event handlers.</param>
        /// <returns>Event store installer for Unity.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static EventStoreContainerExtension FromAssembly(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");
            return new EventStoreContainerExtension(assembly.GetTypes());
        }

        /// <summary>
        /// Installs the specified event handler types.
        /// </summary>
        /// <param name="handlerTypes">Event handler types.</param>
        /// <returns>Event store installer for Unity.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static EventStoreContainerExtension FromHandlerTypes(IEnumerable<Type> handlerTypes)
        {
            if (handlerTypes == null) throw new ArgumentNullException("handlerTypes");
            return new EventStoreContainerExtension(handlerTypes);
        }

        /// <summary>
        /// Installs the specified event handler instances.
        /// </summary>
        /// <param name="handlers">Event handler instances.</param>
        /// <returns>Event store installer for Unity.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static EventStoreContainerExtension FromHandlerInstances(IEnumerable<IEventHandler> handlers)
        {
            if (handlers == null) throw new ArgumentNullException("handlers");
            return new EventStoreContainerExtension(handlers);
        }

        protected override void Initialize()
        {
            this.Container.RegisterType<EventStore>(new InjectionFactory(this.CreateEventStore));
            if (this.handlerTypes != null)
            {
                foreach (var type in this.handlerTypes.Where(x => x.IsClass && x.IsAbstract == false))
                {
                    RegisterEventTypes(this.Container, type);
                }
            }

            if (this.handlers != null)
            {
                foreach (var handler in this.handlers)
                {
                    RegisterEventTypes(this.Container, handler.GetType(), handler);
                }
            }
        }

        private EventStore CreateEventStore(IUnityContainer arg)
        {
            if (this.handlerTypes != null)
                return new EventStore(new UnityServiceLocator(arg)).Initialize(this.handlerTypes);

            return new EventStore(new UnityServiceLocator(this.Container)).Initialize(this.handlers.Select(x => x.GetType()));
        }

        private static void RegisterEventTypes(IUnityContainer container, Type type, object instance = null)
        {
            var interfaces = type.GetInterfaces();
            foreach (var i in interfaces.Where(x => x.IsGenericType))
            {
                var genericTypeDefinition = i.GetGenericTypeDefinition();
                if (!typeof(IEventHandler<>).IsAssignableFrom(genericTypeDefinition)) continue;

                var genericArguments = string.Join(
                    ", ", i.GetGenericArguments().Select(x => x.ToString()));
                var name = string.Format("{0}<{1}>", type.FullName, genericArguments);
                if (instance != null)
                {
                    container.RegisterInstance(i, name, instance);
                }
                else
                {
                    container.RegisterType(i, type, name, new TransientLifetimeManager());
                }
            }
        }
    }
}