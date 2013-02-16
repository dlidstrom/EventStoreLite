using System;
using EventStoreLite.Infrastructure;
using EventStoreLite.IoC;

namespace EventStoreLite
{
    /// <summary>
    /// Used to dispatch events to event handlers.
    /// </summary>
    internal class EventDispatcher
    {
        private readonly IServiceLocator container;

        public EventDispatcher(IServiceLocator container)
        {
            if (container == null) throw new ArgumentNullException("container");
            this.container = container;
        }

        public void Dispatch(IDomainEvent e)
        {
            var type = typeof(IEventHandler<>).MakeGenericType(e.GetType());
            var handlers = this.container.ResolveAll(type);
            foreach (var handler in handlers)
            {
                handler.AsDynamic().Handle(e);
            }
        }
    }
}
