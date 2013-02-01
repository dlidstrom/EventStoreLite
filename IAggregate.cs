using System.Collections.Generic;

namespace RavenEventStore
{
    public interface IAggregate
    {
        string Id { get; set; }
        IEnumerable<IEvent> GetUncommittedChanges();
        void MarkChangesAsCommitted();
    }
}
