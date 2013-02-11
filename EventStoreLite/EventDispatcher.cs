using System;
using Castle.MicroKernel;
using EventStoreLite.Infrastructure;

namespace EventStoreLite
{
    public class EventDispatcher
    {
        private readonly IKernel kernel;

        public EventDispatcher(IKernel kernel)
        {
            if (kernel == null) throw new ArgumentNullException("kernel");
            this.kernel = kernel;
        }

        public void Dispatch(IDomainEvent e)
        {
            var type = typeof(IEventHandler<>).MakeGenericType(e.GetType());
            var handlers = this.kernel.ResolveAll(type);
            foreach (var handler in handlers)
            {
                handler.AsDynamic().Handle(e);
                kernel.ReleaseComponent(handler);
            }
        }
    }
}
