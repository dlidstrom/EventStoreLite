using System;
using System.Collections.Generic;
using System.Diagnostics;
using EventStoreLite.Infrastructure;

namespace EventStoreLite
{
    public abstract class AggregateRoot<TAggregate> : IAggregate where TAggregate : class
    {
        private readonly List<IDomainEvent> uncommittedChanges = new List<IDomainEvent>();

        protected AggregateRoot()
        {
            this.Changes = new List<IDomainEvent>();
        }

        public string Id { get; set; }
        private List<IDomainEvent> Changes { get; set; }

        internal void LoadFromHistory()
        {
            this.Changes.ForEach(x => this.ApplyChange(x, false));
        }

        public IDomainEvent[] GetUncommittedChanges()
        {
            return this.uncommittedChanges.ToArray();
        }

        public IDomainEvent[] GetHistory()
        {
            return this.Changes.ToArray();
        }

        /// <summary>
        /// Called implicitly from the event store when saving changes.
        /// </summary>
        internal void MarkChangesAsCommitted()
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
