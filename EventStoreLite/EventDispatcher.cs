namespace EventStoreLite
{
    using System;
    using System.Collections.Generic;

    public class EventDispatcher
    {
        private readonly Dictionary<Type, Action<IDomainEvent>> handlers
            = new Dictionary<Type, Action<IDomainEvent>>(); 

        public void RegisterHandler<TEvent>(IEventHandler<TEvent> handler)
            where TEvent : class, IDomainEvent
        {
            var type = typeof(TEvent);
            this.handlers[type] = @event => handler.Handle(@event as TEvent);
        }

        public void Dispatch(IDomainEvent message)
        {
            var type = message.GetType();
            Action<IDomainEvent> handler;
            if (this.handlers.TryGetValue(type, out handler))
                handler.Invoke(message);
        }
    }
}
