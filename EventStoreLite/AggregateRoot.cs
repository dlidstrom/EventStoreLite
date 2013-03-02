using System;
using System.Collections.Generic;
using System.Diagnostics;
using EventStoreLite.Infrastructure;

namespace EventStoreLite
{
    /// <summary>
    /// Used to define aggregate roots.
    /// </summary>
    public abstract class AggregateRoot : IAggregate
    {
        private List<IDomainEvent> uncommittedChanges = new List<IDomainEvent>();

        /// <summary>
        /// Gets the id.
        /// </summary>
        public string Id { get; private set; }

        internal void SetId(string id)
        {
            if (id == null) throw new ArgumentNullException("id");
            Id = id;
        }

        internal void LoadFromHistory(IEnumerable<IDomainEvent> history)
        {
            if (history == null) throw new ArgumentNullException("history");
            uncommittedChanges = new List<IDomainEvent>();
            foreach (var domainEvent in history) this.ApplyChange(domainEvent, false);
        }

        /// <summary>
        /// Gets the uncommitted changes. These are events that
        /// have been raised by the aggregate root but have not
        /// yet been persisted to the event store.
        /// </summary>
        /// <returns>Uncommitted changes.</returns>
        public IDomainEvent[] GetUncommittedChanges()
        {
            return this.uncommittedChanges.ToArray();
        }

        /// <summary>
        /// Clears the uncommitted changes. This is done when
        /// changes have been committed to the event store.
        /// </summary>
        internal void ClearUncommittedChanges()
        {
            this.uncommittedChanges = new List<IDomainEvent>();
        }

        /// <summary>
        /// Applies the event to this aggregate root instance.
        /// </summary>
        /// <param name="event">Event instance.</param>
        [DebuggerStepThrough]
        protected void ApplyChange(Event @event)
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
