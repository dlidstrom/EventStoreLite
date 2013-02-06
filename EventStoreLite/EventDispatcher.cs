namespace EventStoreLite
{
    using System;
    using System.Collections.Generic;

    public class EventDispatcher
    {
        private readonly IDictionary<Type, Action<IDomainEvent>> handlers
            = new Dictionary<Type, Action<IDomainEvent>>();

        public void Register<TEvent, TAggregate>(Action<TEvent> handler)
            where TEvent : Event<TAggregate>
        {
            // re-wrap delegate
            var type = typeof(TEvent);
            this.handlers[type] = @event => handler(@event as TEvent);
        }

        public void Dispatch(IDomainEvent message)
        {
            var type = message.GetType();

            Action<IDomainEvent> handler;
            if (this.handlers.TryGetValue(type, out handler))
                handler(message);
        }
    }
}
