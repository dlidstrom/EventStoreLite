using System.Diagnostics;
using EventStoreLite.Infrastructure;

namespace EventStoreLite
{
    using System;
    using System.Collections.Generic;

    public abstract class AggregateRoot<TAggregate> : IAggregate where TAggregate : class
    {
        private readonly List<IDomainEvent> uncommittedChanges = new List<IDomainEvent>();

        protected AggregateRoot()
        {
            this.Changes = new List<IDomainEvent>();
        }

        public string Id { get; set; }
        private List<IDomainEvent> Changes { get; set; }

        public void LoadFromHistory()
        {
            this.Changes.ForEach(x => this.ApplyChange(x, false));
        }

        public IEnumerable<IDomainEvent> GetUncommittedChanges()
        {
            return this.uncommittedChanges.ToArray();
        }

        public IEnumerable<IDomainEvent> GetHistory()
        {
            return this.Changes.ToArray();
        }

        public void MarkChangesAsCommitted()
        {
            this.Changes.AddRange(this.uncommittedChanges);
            this.uncommittedChanges.Clear();
        }

        protected void ApplyChange(Event<TAggregate> @event)
        {
            if (@event == null) throw new ArgumentNullException("event");
            this.ApplyChange(@event, true);
        }

        [DebuggerStepThrough]
        private void ApplyChange(IDomainEvent @event, bool isNew)
        {
            this.AsDynamic().Apply(@event);
            if (isNew) this.uncommittedChanges.Add(@event);
        }
    }
}
