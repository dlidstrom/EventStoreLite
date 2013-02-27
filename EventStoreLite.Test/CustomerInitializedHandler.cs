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

        public void Handle(CustomerInitialized e, string aggregateId)
        {
            this.AggregateId = aggregateId;
            var viewModel = new CustomerViewModel
                            {
                                AggregateId = aggregateId,
                                Name = e.Name
                            };
            this.session.Store(viewModel);
        }

        public void Handle(CustomerNameChanged e, string aggregateId)
        {
        }
    }
}