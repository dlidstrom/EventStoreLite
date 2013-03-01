using AccountManager.Models;
using AccountManager.ReadModels;
using EventStoreLite;
using Raven.Client;

namespace AccountManager.Handlers
{
    public class AccountHandler : IEventHandler<AccountCreated>,
                                  IEventHandler<AccountActivated>
    {
        private readonly IDocumentSession session;

        public AccountHandler(IDocumentSession session)
        {
            this.session = session;
        }

        public void Handle(AccountCreated e, string aggregateId)
        {
            var id = GetId(aggregateId);
            session.Store(new AccountReadModel(aggregateId) { Id = id, Email = e.Email, Activated = false });
        }

        public void Handle(AccountActivated e, string aggregateId)
        {
            var id = GetId(aggregateId);
            var rm = session.Load<AccountReadModel>(id);
            rm.Activated = true;
        }

        private static string GetId(string aggregateId)
        {
            return "ReadModels" + aggregateId.Replace("EventStreams", string.Empty);
        }
    }

    public class AccountStatsHandler : IEventHandler<AccountCreated>
    {
        private readonly IDocumentSession session;

        public AccountStatsHandler(IDocumentSession session)
        {
            this.session = session;
        }

        public void Handle(AccountCreated e, string aggregateId)
        {
            var stats = session.Load<AccountStatsReadModel>(AccountStatsReadModel.DbIdentifier);
            if (stats == null)
            {
                stats = new AccountStatsReadModel();
                session.Store(stats);
            }

            stats.Total ++;
        }
    }
}