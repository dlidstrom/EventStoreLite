using System;
using EventStoreLite;

namespace SampleDomain.Domain
{
    public class CustomerInitialized : Event<Customer>
    {
        public CustomerInitialized(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }
    }
}
