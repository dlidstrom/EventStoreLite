using System;

namespace EventStoreLite.IoC
{
    /// <summary>
    /// Used to make event store container-agnostic.
    /// </summary>
    public interface IServiceLocator
    {
        /// <summary>
        /// Resolve component.
        /// </summary>
        /// <param name="type">Service type.</param>
        /// <returns>Service instance.</returns>
        object Resolve(Type type);

        /// <summary>
        /// Resolve all components registered for the specified service type.
        /// </summary>
        /// <param name="type">Service type.</param>
        /// <returns>Components.</returns>
        Array ResolveAll(Type type);

        /// <summary>
        /// Release component.
        /// </summary>
        /// <param name="o">Component instance.</param>
        void Release(object o);
    }
}
