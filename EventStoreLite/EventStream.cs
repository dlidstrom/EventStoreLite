using System.Collections.Generic;

namespace EventStoreLite
{
    internal class EventStream
    {
        public string Id { get; set; }

        public EventStream()
        {
            this.History = new List<IDomainEvent>();
        }

        public List<IDomainEvent> History { get; private set; }
    }
}