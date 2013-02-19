using System;

namespace EventStoreLite
{
    /// <summary>
    /// Event base class type.
    /// </summary>
    /// <typeparam name="TAggregate">Used to tie the event to an aggregate type.</typeparam>
    public abstract class Event<TAggregate> : IDomainEvent
    {
        /// <summary>
        /// Gets the aggregate id.
        /// </summary>
        public string AggregateId { get; private set; }

        /// <summary>
        /// Gets the event time stamp.
        /// </summary>
        public DateTimeOffset TimeStamp { get; private set; }

        internal void SetAggregateId(string id)
        {
            AggregateId = id;
        }

        internal void SetTimeStamp(DateTimeOffset dateTimeOffset)
        {
            TimeStamp = dateTimeOffset;
        }
    }
}
