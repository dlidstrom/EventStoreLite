namespace EventStoreLite
{
    using System.Collections.Generic;

    public interface IAggregate
    {
        string Id { get; }
        IEnumerable<IDomainEvent> GetUncommittedChanges();
        void MarkChangesAsCommitted();
    }
}
