using System;
using System.Linq;
using Microsoft.Practices.Unity;

namespace EventStoreLite.IoC.Unity
{
    /// <summary>
    /// Service locator that uses Unity.
    /// </summary>
    public class UnityServiceLocator : IServiceLocator
    {
        private readonly IUnityContainer unityContainer;

        /// <summary>
        /// Initializes a new instance of the UnityServiceLocator class.
        /// </summary>
        /// <param name="unityContainer">Unity container.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public UnityServiceLocator(IUnityContainer unityContainer)
        {
            if (unityContainer == null) throw new ArgumentNullException("unityContainer");
            this.unityContainer = unityContainer;
        }

        /// <summary>
        /// Resolve component.
        /// </summary>
        /// <param name="type">Service type.</param>
        /// <returns>Service instance.</returns>
        public object Resolve(Type type)
        {
            try
            {
                return this.unityContainer.Resolve(type);
            }
            catch (ResolutionFailedException)
            {
                var instances = this.unityContainer.ResolveAll(type).ToArray();
                if (instances.Length == 1) return instances[0];
                throw;
            }
        }

        /// <summary>
        /// Resolve all components registered for the specified service type.
        /// </summary>
        /// <param name="type">Service type.</param>
        /// <returns>Components.</returns>
        public Array ResolveAll(Type type)
        {
            return this.unityContainer.ResolveAll(type).ToArray();
        }

        /// <summary>
        /// Release component.
        /// </summary>
        /// <param name="o">Component instance.</param>
        public void Release(object o)
        {
        }
    }
}