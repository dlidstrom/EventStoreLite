namespace EventStoreLite
{
    /// <summary>
    /// Marker interface.
    /// </summary>
    public interface IEventHandler
    {
    }

    /// <summary>
    /// Used to create event handlers for a specific event type.
    /// </summary>
    /// <typeparam name="TEvent">Event type.</typeparam>
    public interface IEventHandler<in TEvent> : IEventHandler where TEvent : IDomainEvent
    {
        /// <summary>
        /// Handle the event.
        /// </summary>
        /// <param name="e">Event instance.</param>
        void Handle(TEvent e);
    }
}
