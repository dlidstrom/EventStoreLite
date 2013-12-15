using System.Collections.Generic;
using EventStoreLite.IoC.Castle;
using NUnit.Framework;
using Raven.Client;

namespace EventStoreLite.Test
{
    [TestFixture]
    public class EventStore_RebuildReadModels_Ordering :
        TestBase,
        IEventHandler<EventStore_RebuildReadModels_Ordering.AuthorCreated>,
        IEventHandler<EventStore_RebuildReadModels_Ordering.BookCreated>,
        IEventHandler<EventStore_RebuildReadModels_Ordering.AuthorAdded>,
        IEventHandler<EventStore_RebuildReadModels_Ordering.BookAdded>
    {
        private List<string> changes = new List<string>();

        [Test]
        public void RebuildingAppliesEventsAfterCommitSequence()
        {
            // Arrange
            var container = CreateContainer(new[] { this });
            var documentStore = container.Resolve<IDocumentStore>();
            var eventStore = container.Resolve<EventStore>();

            using (var documentSession = documentStore.OpenSession())
            {
                var session = eventStore.OpenSession(documentStore, documentSession);
                var book = new Book("Book-1");
                session.Store(book);
                var author = new Author("Author-A");
                session.Store(author);
                author.AddBook(book);
                book.AddAuthor(author);
                session.SaveChanges();
            }

            // Act
            var currentChanges = changes;
            changes = new List<string>();
            EventStore.ReplayEvents(new WindsorServiceLocator(container));

            // Assert
            Assert.That(changes.Count, Is.EqualTo(4));
            Assert.That(changes[0], Is.EqualTo("BookCreated"));
            Assert.That(changes[1], Is.EqualTo("AuthorCreated"));
            Assert.That(changes[2], Is.EqualTo("BookAdded"));
            Assert.That(changes[3], Is.EqualTo("AuthorAdded"));
            Assert.That(changes, Is.EqualTo(currentChanges));
        }

        public void Handle(AuthorCreated e, string aggregateId)
        {
            changes.Add("AuthorCreated");
        }

        public void Handle(BookCreated e, string aggregateId)
        {
            changes.Add("BookCreated");
        }

        public void Handle(AuthorAdded e, string aggregateId)
        {
            changes.Add("AuthorAdded");
        }

        public void Handle(BookAdded e, string aggregateId)
        {
            changes.Add("BookAdded");
        }

        public class AuthorCreated : Event { public string Name { get; set; } }

        public class BookAdded : Event { public string BookId { get; set; } }

        public class BookCreated : Event { public string Title { get; set; } }

        public class AuthorAdded : Event { public string AuthorId { get; set; } }

        private class Author : AggregateRoot
        {
            public Author(string name)
            {
                ApplyChange(new AuthorCreated { Name = name });
            }

            public void AddBook(Book book)
            {
                ApplyChange(new BookAdded { BookId = book.Id });
            }
        }

        private class Book : AggregateRoot
        {
            public Book(string title)
            {
                ApplyChange(new BookCreated { Title = title });
            }

            public void AddAuthor(Author author)
            {
                ApplyChange(new AuthorAdded { AuthorId = author.Id });
            }
        }
    }
}