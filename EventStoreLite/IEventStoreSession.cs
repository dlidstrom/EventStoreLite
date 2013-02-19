namespace EventStoreLite
{
    /// <summary>
    /// Represents an event store session.
    /// </summary>
    public interface IEventStoreSession
    {
        /// <summary>
        /// Loads an aggregate root from the event store.
        /// </summary>
        /// <typeparam name="TAggregate">Type of aggregate root.</typeparam>
        /// <param name="id">Aggregate identifier.</param>
        /// <returns>Aggregate root instance.</returns>
        TAggregate Load<TAggregate>(string id) where TAggregate : AggregateRoot<TAggregate>;

        /// <summary>
        /// Persists the specified aggregate root to the event store.
        /// </summary>
        /// <typeparam name="TAggregate">Type of aggregate root.</typeparam>
        /// <param name="aggregate">Aggregate root instance.</param>
        void Store<TAggregate>(AggregateRoot<TAggregate> aggregate) where TAggregate : class;

        /// <summary>
        /// Commits to the event store. This will dispatch any raised events,
        /// mark all uncommitted changes as committed, and commit the document session.
        /// </summary>
        void SaveChanges();
    }
}