using AccountManager.Models;
using AccountManager.ReadModels;
using EventStoreLite;
using Raven.Client;

namespace AccountManager.Handlers
{
    public class AuditLogHandler : IEventHandler<AccountCreated>,
        IEventHandler<AccountActivated>
    {
        private readonly IDocumentSession session;

        public AuditLogHandler(IDocumentSession session)
        {
            this.session = session;
        }

        public void Handle(AccountActivated e, string aggregateId)
        {
            var change = string.Format("{0} - {1}: Activated", e.TimeStamp, aggregateId.Replace("EventStreams/", string.Empty));
            session.Store(new AuditLogReadModel{ Change = change });
        }

        public void Handle(AccountCreated e, string aggregateId)
        {
            var change = string.Format("{0} - {1}: Created {2}", e.TimeStamp, aggregateId.Replace("EventStreams/", string.Empty), e.Email);
            session.Store(new AuditLogReadModel { Change = change });
        }
    }
}