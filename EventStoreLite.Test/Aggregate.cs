namespace EventStoreLite.Test
{
    public class Aggregate : AggregateRoot
    {
        public Aggregate()
        {
            this.ApplyChange(new AggregateCreated());
        }
        public void Change()
        {
            this.ApplyChange(new AggregateChanged());
        }
        private void Apply(AggregateChanged e)
        {
            this.Changed = true;
        }

        public bool Changed { get; private set; }
    }
}