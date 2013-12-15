using System;
using System.IO;
using EventStoreLite;

namespace SampleDomain.Domain
{
    public class Customer : AggregateRoot
    {
        private string name;
        private bool hasChangedName;
        private string previousName;

        public Customer(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            ApplyChange(new CustomerInitialized(name));
        }

        public void ChangeName(string newName)
        {
            if (newName == null) throw new ArgumentNullException("newName");
            ApplyChange(new CustomerNameChanged(name, newName));
        }

        public void PrintName(TextWriter writer)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            var message = name;
            if (hasChangedName)
                message += string.Format(" (changed from {0})", previousName);
            writer.WriteLine(message);
        }

        private void Apply(CustomerInitialized e)
        {
            name = e.Name;
        }

        private void Apply(CustomerNameChanged e)
        {
            previousName = e.OldName;
            name = e.NewName;
            hasChangedName = true;
        }
    }
}