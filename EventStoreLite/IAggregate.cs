namespace EventStoreLite
{
    public interface IAggregate
    {
        string Id { get; }
        IDomainEvent[] GetUncommittedChanges();
        IDomainEvent[] GetHistory();
    }
}
