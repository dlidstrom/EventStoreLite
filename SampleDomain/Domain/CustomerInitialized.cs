using EventStoreLite;

namespace SampleDomain.Domain
{
    public class CustomerInitialized : Event
    {
        public CustomerInitialized(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}