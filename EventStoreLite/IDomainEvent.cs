using System;

namespace EventStoreLite
{
    public interface IDomainEvent
    {
        string AggregateId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
