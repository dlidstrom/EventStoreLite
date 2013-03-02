namespace EventStoreLite.Test
{
    public class AggregateHandler : IEventHandler<AggregateChanged>
    {
        public int Changes { get; private set; }

        public void Handle(AggregateChanged e, string aggregateId)
        {
            this.Changes++;
        }
    }
}