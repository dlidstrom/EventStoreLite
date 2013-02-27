using System;

namespace EventStoreLite
{
    /// <summary>
    /// Event base class type.
    /// </summary>
    public abstract class Event : IDomainEvent
    {
        /// <summary>
        /// Gets the event time stamp.
        /// </summary>
        public DateTimeOffset TimeStamp { get; private set; }

        internal void SetTimeStamp(DateTimeOffset dateTimeOffset)
        {
            TimeStamp = dateTimeOffset;
        }
    }
}
