using System;

namespace EventStoreLite
{
    public interface IDomainEvent
    {
        string AggregateId { get; set; }
        DateTimeOffset TimeStamp { get; set; }
    }
}
