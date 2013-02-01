using System;
using System.IO;
using System.Linq;

namespace RavenEventStore
{
    public class Customer : AggregateRoot<Customer>
    {
        private string name;
        private bool hasChangedName;
        private string previousName;

        public Customer(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            this.ApplyChange(new CustomerInitializedEvent(name));
        }

        // necessary for loading from event store
        private Customer()
        {
        }

        public void ChangeName(string newName)
        {
            if (newName == null) throw new ArgumentNullException("newName");
            this.ApplyChange(new ChangeCustomerNameEvent(newName));
        }

        public void PrintName(TextWriter writer)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            var message = this.name;
            if (this.hasChangedName)
                message += string.Format(" (changed from {0})", previousName);
            writer.WriteLine(message);
        }

        private void Apply(CustomerInitializedEvent e)
        {
            this.name = e.Name;
        }

        private void Apply(ChangeCustomerNameEvent e)
        {
            this.previousName = this.name;
            this.name = e.NewName;
            this.hasChangedName = true;
        }
    }
}
