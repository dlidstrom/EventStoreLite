namespace EventStoreLite.Test
{
    public class AnotherAggregateHandler : IEventHandler<AggregateChanged>
    {
        public void Handle(AggregateChanged e, string aggregateId)
        {
        }
    }
}