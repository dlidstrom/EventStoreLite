using EventStoreLite;

namespace AccountManager.ReadModels
{
    public class AuditLogReadModel : IReadModel
    {
        public string Change { get; set; }

        public string Id { get; private set; }
    }
}