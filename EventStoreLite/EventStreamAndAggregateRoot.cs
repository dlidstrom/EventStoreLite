namespace EventStoreLite
{
    internal class EventStreamAndAggregateRoot
    {
        public EventStreamAndAggregateRoot(EventStream eventStream, AggregateRoot aggregateRoot)
        {
            EventStream = eventStream;
            AggregateRoot = aggregateRoot;
        }

        public EventStream EventStream { get; private set; }

        public AggregateRoot AggregateRoot { get; private set; }
    }
}