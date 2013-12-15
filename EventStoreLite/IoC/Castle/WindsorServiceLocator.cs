using System;
using Castle.Windsor;

namespace EventStoreLite.IoC.Castle
{
    /// <summary>
    /// Service locator that uses Castle Windsor.
    /// </summary>
    public class WindsorServiceLocator : IServiceLocator
    {
        private readonly IWindsorContainer container;

        /// <summary>
        /// Initializes a new instance of the WindsorServiceLocator class.
        /// </summary>
        /// <param name="container"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public WindsorServiceLocator(IWindsorContainer container)
        {
            if (container == null) throw new ArgumentNullException("container");
            this.container = container;
        }

        /// <summary>
        /// Resolve component.
        /// </summary>
        /// <param name="type">Service type.</param>
        /// <returns>Service instance.</returns>
        public object Resolve(Type type)
        {
            return container.Resolve(type);
        }

        /// <summary>
        /// Resolve all components registered for the specified service type.
        /// </summary>
        /// <param name="type">Service type.</param>
        /// <returns>Components.</returns>
        public Array ResolveAll(Type type)
        {
            return container.ResolveAll(type);
        }

        /// <summary>
        /// Release component.
        /// </summary>
        /// <param name="o">Component instance.</param>
        public void Release(object o)
        {
            container.Release(o);
        }
    }
}