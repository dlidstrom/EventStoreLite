namespace EventStoreLite.Test
{
    using System;
    using System.IO;

    public class Customer : AggregateRoot<Customer>
    {
        private string name;
        private bool hasChangedName;
        private string previousName;

        public Customer(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            this.ApplyChange(new CustomerInitialized(name));
        }

        // necessary for loading from event store
        private Customer()
        {
        }

        public void ChangeName(string newName)
        {
            if (newName == null) throw new ArgumentNullException("newName");
            this.ApplyChange(new CustomerNameChanged(newName));
        }

        public void PrintName(TextWriter writer)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            var message = this.name;
            if (this.hasChangedName)
                message += string.Format(" (changed from {0})", this.previousName);
            writer.WriteLine(message);
        }

        private void Apply(CustomerInitialized e)
        {
            this.name = e.Name;
        }

        private void Apply(CustomerNameChanged e)
        {
            this.previousName = this.name;
            this.name = e.NewName;
            this.hasChangedName = true;
        }
    }
}
