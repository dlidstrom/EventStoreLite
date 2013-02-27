namespace EventStoreLite
{
    internal class EventStreamAndAggregateRoot
    {
        public EventStreamAndAggregateRoot(EventStream eventStream, AggregateRoot aggregateRoot)
        {
            this.EventStream = eventStream;
            this.AggregateRoot = aggregateRoot;
        }

        public EventStream EventStream { get; private set; }

        public AggregateRoot AggregateRoot { get; private set; }
    }
}