using System;
using EventStoreLite;

namespace SampleDomain.Domain
{
    public class CustomerNameChanged : Event<Customer>
    {
        public CustomerNameChanged(string oldName, string newName)
        {
            this.OldName = oldName;
            this.NewName = newName;
        }

        public string OldName { get; set; }

        public string NewName { get; set; }
    }
}
