namespace EventStoreLite.Test
{
    public class CustomerViewModel : IReadModel
    {
        public string AggregateId { get; set; }

        public string Name { get; set; }

        public string Id { get; set; }
    }
}