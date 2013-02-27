using System;
using SampleDomain.Domain;

namespace EventStoreLite.Test
{
    public class Handler : IEventHandler<CustomerInitialized>
    {
        public Handler()
        {
            Callback = () => { };
        }

        public Action Callback { get; set; }

        public void Handle(CustomerInitialized e, string aggregateId)
        {
            this.Callback.Invoke();
        }
    }
}