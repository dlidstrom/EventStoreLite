using System;

namespace EventStoreLite.IoC
{
    /// <summary>
    /// Used to make event store container-agnostic.
    /// </summary>
    public interface IServiceLocator
    {
        object Resolve(Type type);

        Array ResolveAll(Type type);

        void Release(object o);
    }
}
