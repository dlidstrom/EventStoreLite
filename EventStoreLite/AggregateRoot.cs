using System;
using System.Collections.Generic;
using System.Diagnostics;
using EventStoreLite.Infrastructure;

namespace EventStoreLite
{
    /// <summary>
    /// Used to define aggregate roots.
    /// </summary>
    /// <typeparam name="TAggregate">Type of aggregate class.</typeparam>
    public abstract class AggregateRoot<TAggregate> : IAggregate where TAggregate : class
    {
        private readonly List<IDomainEvent> uncommittedChanges = new List<IDomainEvent>();

        /// <summary>
        /// Initializes a new instance of the AggregateRoot class.
        /// </summary>
        protected AggregateRoot()
        {
            this.Changes = new List<IDomainEvent>();
        }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public string Id { get; set; }

        private List<IDomainEvent> Changes { get; set; }

        internal void LoadFromHistory()
        {
            this.Changes.ForEach(x => this.ApplyChange(x, false));
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
        /// Gets the history of this aggregate root. This is the
        /// complete list of events that have been raised by this
        /// aggregate root.
        /// </summary>
        /// <returns>Event history.</returns>
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

        /// <summary>
        /// Applies the event to this aggregate root instance.
        /// </summary>
        /// <param name="event">Event instance.</param>
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
