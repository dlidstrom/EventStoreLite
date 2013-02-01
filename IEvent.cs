using System;

namespace RavenEventStore
{
    public interface IEvent
    {
        string AggregateId { get; set; }
        DateTimeOffset TimeStamp { get; set; }
    }
}
