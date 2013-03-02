using System;
using Castle.Windsor;

namespace EventStoreLite.IoC.Castle
{
    internal class WindsorServiceLocator : IServiceLocator
    {
        private readonly IWindsorContainer container;

        public WindsorServiceLocator(IWindsorContainer container)
        {
            if (container == null) throw new ArgumentNullException("container");
            this.container = container;
        }

        public object Resolve(Type type)
        {
            return this.container.Resolve(type);
        }

        public Array ResolveAll(Type type)
        {
            return this.container.ResolveAll(type);
        }

        public void Release(object o)
        {
            this.container.Release(o);
        }
    }
}