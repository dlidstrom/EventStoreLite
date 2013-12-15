using EventStoreLite;

namespace SampleDomain.Domain
{
    public class CustomerNameChanged : Event
    {
        public CustomerNameChanged(string oldName, string newName)
        {
            OldName = oldName;
            NewName = newName;
        }

        public string OldName { get; set; }

        public string NewName { get; set; }
    }
}