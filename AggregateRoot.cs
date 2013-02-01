using System;
using System.Collections.Generic;
using System.Linq;
using ReflectionMagic;

namespace RavenEventStore
{
    public abstract class AggregateRoot<TAggregate> : IAggregate where TAggregate : class
    {
        private readonly List<IEvent> changes = new List<IEvent>();

        protected AggregateRoot()
        {
            this.Changes = new List<IEvent>();
        }

        public string Id { get; set; }
        private List<IEvent> Changes { get; set; }

        public void LoadFromHistory()
        {
            this.Changes.ForEach(x => this.ApplyChange(x, false));
        }

        public IEnumerable<IEvent> GetUncommittedChanges()
        {
            return this.changes.ToArray();
        }

        public void MarkChangesAsCommitted()
        {
            this.Changes.AddRange(this.changes);
            this.changes.Clear();
        }

        protected void ApplyChange(Event<TAggregate> @event)
        {
            if (@event == null) throw new ArgumentNullException("event");
            this.ApplyChange(@event, true);
        }

        private void ApplyChange(IEvent @event, bool isNew)
        {
            this.AsDynamic().Apply(@event);
            if (isNew) this.changes.Add(@event);
        }
    }
}
