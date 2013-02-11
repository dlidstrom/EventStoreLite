using System.Collections.Generic;

namespace EventStoreLite
{
    public interface IAggregate
    {
        string Id { get; }
        IEnumerable<IDomainEvent> GetUncommittedChanges();
        void MarkChangesAsCommitted();
        IEnumerable<IDomainEvent> GetHistory();
    }
}
