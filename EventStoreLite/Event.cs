using System;

namespace EventStoreLite
{
    /// <summary>
    /// Event base class type.
    /// </summary>
    /// <typeparam name="TAggregate">Used to tie the event to an aggregate type.</typeparam>
    public abstract class Event<TAggregate> : IDomainEvent
    {
        public string AggregateId { get; private set; }

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
