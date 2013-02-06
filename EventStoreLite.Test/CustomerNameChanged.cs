namespace EventStoreLite.Test
{
    using System;

    public class CustomerNameChanged : Event<Customer>
    {
        public CustomerNameChanged(string name)
        {
            this.NewName = name;
        }

        public string NewName { get; set; }

        public override string AggregateId { get; set; }

        public override DateTimeOffset TimeStamp { get; set; }
    }
}
