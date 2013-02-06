namespace EventStoreLite
{
    using System;

    public interface IDomainEvent
    {
        string AggregateId { get; set; }
        DateTimeOffset TimeStamp { get; set; }
    }
}
