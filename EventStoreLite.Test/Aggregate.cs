namespace EventStoreLite.Test
{
    public class Aggregate : AggregateRoot
    {
        public Aggregate()
        {
            ApplyChange(new AggregateCreated());
        }

        public bool Changed { get; private set; }

        public void Change()
        {
            ApplyChange(new AggregateChanged());
        }

        private void Apply(AggregateChanged e)
        {
            Changed = true;
        }
    }
}