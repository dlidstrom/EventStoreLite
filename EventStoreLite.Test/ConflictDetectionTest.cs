using NUnit.Framework;
using Raven.Client;

namespace EventStoreLite.Test
{
    [TestFixture]
    public class ConflictDetectionTest : TestBase
    {
        [Test, Ignore]
        public void MergesEvents()
        {
            // Arrange
            var container = CreateContainer();
            var eventStore = container.Resolve<EventStore>();
            var documentStore = container.Resolve<IDocumentStore>();

            // store base aggregate
            var aggregate = new Aggregate();
            using (var documentSession = documentStore.OpenSession())
            {
                var eventStoreSession = eventStore.OpenSession(documentStore, documentSession);
                eventStoreSession.Store(aggregate);
                eventStoreSession.SaveChanges();
            }

            using (var firstSession = documentStore.OpenSession())
            using (var secondSession = documentStore.OpenSession())
            {
                // use optimistic concurrency
                // this will enable merging of events
                secondSession.Advanced.UseOptimisticConcurrency = true;

                var firstEventStoreSession = eventStore.OpenSession(documentStore, firstSession);

                // Act

                // make change to left side
                firstEventStoreSession.Load<Aggregate>(aggregate.Id).FirstAction();

                var secondEventStoreSession = eventStore.OpenSession(documentStore, secondSession);

                // make change to right side
                secondEventStoreSession.Load<Aggregate>(aggregate.Id).SecondAction();

                firstEventStoreSession.SaveChanges();
                secondEventStoreSession.SaveChanges();
            }

            // Assert

            // expect merged result
            using (var documentSession = documentStore.OpenSession())
            {
                var eventStoreSession = eventStore.OpenSession(documentStore, documentSession);
                var mergedAggregate = eventStoreSession.Load<Aggregate>(aggregate.Id);
                // Assert.That(mergedAggregate.GetHistory().Length, Is.EqualTo(3));
            }
        }

        public class Initialized : Event { }

        public class FirstActionOccurred : Event { }

        public class SecondActionOccurred : Event { }

        public class Aggregate : AggregateRoot
        {
            public Aggregate()
            {
                ApplyChange(new Initialized());
            }

            public Aggregate FirstAction()
            {
                ApplyChange(new FirstActionOccurred());
                return this;
            }

            public Aggregate SecondAction()
            {
                ApplyChange(new SecondActionOccurred());
                return this;
            }
        }
    }
}