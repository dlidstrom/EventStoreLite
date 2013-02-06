namespace EventStoreLite
{
    using System;

    public abstract class Event<TAggregate> : IDomainEvent
    {
        public abstract string AggregateId { get; set; }

        public abstract DateTimeOffset TimeStamp { get; set; }
    }
}
