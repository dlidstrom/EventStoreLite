using System;

namespace EventStoreLite
{
    /// <summary>
    /// Represents a domain event.
    /// </summary>
    public interface IDomainEvent
    {
        /// <summary>
        /// Gets the event time stamp.
        /// </summary>
        DateTimeOffset TimeStamp { get; }
    }
}
