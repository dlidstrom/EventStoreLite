using System;
using System.Linq;

namespace RavenEventStore
{
    public class CustomerInitializedEvent : Event<Customer>
    {
        public CustomerInitializedEvent(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }

        public override string AggregateId { get; set; }

        public override DateTimeOffset TimeStamp { get; set; }
    }
}
