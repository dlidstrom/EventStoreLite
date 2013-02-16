namespace EventStoreLite
{
    public interface IEventStoreSession
    {
        TAggregate Load<TAggregate>(string id) where TAggregate : AggregateRoot<TAggregate>;
        void Store<TAggregate>(AggregateRoot<TAggregate> aggregate) where TAggregate : class;
        void SaveChanges();
    }
}