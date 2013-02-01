using System;
using System.Linq;

namespace RavenEventStore
{
    public class ChangeCustomerNameEvent : Event<Customer>
    {
        public ChangeCustomerNameEvent(string name)
        {
            this.NewName = name;
        }

        public string NewName { get; set; }

        public override string AggregateId { get; set; }

        public override DateTimeOffset TimeStamp { get; set; }
    }
}
