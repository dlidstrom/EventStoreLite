using System;
using System.Linq;
using Microsoft.Practices.Unity;

namespace EventStoreLite.IoC.Unity
{
    internal class UnityServiceLocator : IServiceLocator
    {
        private readonly IUnityContainer unityContainer;

        public UnityServiceLocator(IUnityContainer unityContainer)
        {
            this.unityContainer = unityContainer;
        }

        public object Resolve(Type type)
        {
            return this.unityContainer.Resolve(type);
        }

        public Array ResolveAll(Type type)
        {
            return this.unityContainer.ResolveAll(type).ToArray();
        }

        public void Release(object o)
        {
        }
    }
}