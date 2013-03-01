using EventStoreLite;

namespace AccountManager.ReadModels
{
    public class AccountReadModel : IReadModel
    {
        public AccountReadModel(string aggregateId)
        {
            AggregateId = aggregateId;
        }

        public string Id { get; set; }

        public string AggregateId { get; private set; }

        public string Email { get; set; }

        public bool Activated { get; set; }
    }

    public class AccountStatsReadModel : IReadModel
    {
        public const string DbIdentifier = "account-stats";

        public AccountStatsReadModel()
        {
            Id = DbIdentifier;
        }

        public string Id { get; private set; }

        public int Total { get; set; }
    }
}