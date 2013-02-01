using System;
using System.Linq;

namespace RavenEventStore
{
    public abstract class Event<TAggregate> : IEvent
    {
        public abstract string AggregateId { get; set; }

        public abstract DateTimeOffset TimeStamp { get; set; }
    }
}
