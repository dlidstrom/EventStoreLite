using System;
using System.Collections.Generic;

namespace EventStoreLite
{
    /// <summary>
    /// Defines an event migrator.
    /// </summary>
    public interface IEventMigrator
    {
        /// <summary>
        /// Migrate event.
        /// </summary>
        /// <param name="event">Event to migrate.</param>
        /// <param name="aggregateId">Aggregate id.</param>
        /// <returns>Events to replace with.</returns>
        IEnumerable<IDomainEvent> Migrate(IDomainEvent @event, string aggregateId);

        /// <summary>
        /// Used to order event migrators.
        /// </summary>
        /// <returns>When this event migrator was defined.</returns>
        DateTime DefinedOn();
    }
}