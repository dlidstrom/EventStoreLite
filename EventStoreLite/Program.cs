namespace EventStoreLite
{
    using System;
    using Raven.Client.Document;

    public static class Program
    {
        public static void Main()
        {
            try
            {
                Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void Run()
        {
            /*using (var documentStore = new DocumentStore { Url = "http://localhost:8082" }.Initialize())
            using (var session = new EventStore(documentStore.OpenSession()))
            {
                var existingCustomer = session.Load<Customer>(1);
                if (existingCustomer != null)
                    existingCustomer.PrintName(Console.Out);
                else
                {
                    var customer = new Customer("Daniel Lidström");
                    customer.ChangeName("Per Daniel Lidström");
                    session.Store(customer);
                }

                session.SaveChanges();
            }*/
        }
    }
}
