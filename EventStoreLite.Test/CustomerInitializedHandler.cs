using System;
using Raven.Client;
using SampleDomain.Domain;

namespace EventStoreLite.Test
{
    public class CustomerInitializedHandler : IEventHandler<CustomerInitialized>,
                                              IEventHandler<CustomerNameChanged>
    {
        private readonly IDocumentSession session;

        public CustomerInitializedHandler(IDocumentSession session)
        {
            if (session == null) throw new ArgumentNullException("session");
            this.session = session;
        }

        public string AggregateId { get; set; }

        public void Handle(CustomerInitialized e)
        {
            this.AggregateId = e.AggregateId;
            var viewModel = new CustomerViewModel
                            {
                                AggregateId = e.AggregateId,
                                Name = e.Name
                            };
            this.session.Store(viewModel);
        }

        public void Handle(CustomerNameChanged e)
        {
        }
    }
}