namespace EventStoreLite.Test
{
    using System;

    public class CustomerInitialized : Event<Customer>
    {
        public CustomerInitialized(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }

        public override string AggregateId { get; set; }

        public override DateTimeOffset TimeStamp { get; set; }
    }
}
