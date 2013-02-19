namespace EventStoreLite
{
    internal interface IAggregate
    {
        string Id { get; }
        IDomainEvent[] GetUncommittedChanges();
        IDomainEvent[] GetHistory();
    }
}
