using System;
using System.Collections.Generic;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using EventStoreLite;
using Raven.Client;
using SampleDomain.Domain;

namespace App
{
    public class Benchmark
    {
        public void Run(IWindsorContainer container)
        {
            var random = new Random();

            /*WithEventStore(container, store =>
                {
                    for (var x = 0; x < 1000; x++)
                    {
                        var customer = new Customer(names[random.Next(names.Count)]);
                        for (var i = 0; i < random.Next(100); i++)
                        {
                            customer.ChangeName(names[random.Next(names.Count)]);
                        }

                        store.Store(customer);
                    }
                });*/

            // rebuild read models
            WithEventStore(container, x => x.RebuildReadModels());

            // try loading a few
            WithEventStore(container, store =>
                {
                    var customer = store.Load<Customer>("customers/1460");
                    customer.PrintName(Console.Out);
                });
        }

        private static void WithEventStore(IWindsorContainer container, Action<EventStore> action)
        {
            using (container.BeginScope())
            {
                var store = container.Resolve<IDocumentStore>();
                var session = container.Resolve<IDocumentSession>();
                var eventStore = container.Resolve<EventStore>();
                action.Invoke(eventStore);
                eventStore.SaveChanges();
                session.SaveChanges();
                container.Release(eventStore);
                container.Release(session);
                container.Release(store);
            }
        }

        private readonly List<string> names = new List<string>
                                     {
                                         "SMITH",
                                         "JOHNSON",
                                         "WILLIAMS",
                                         "JONES",
                                         "BROWN",
                                         "DAVIS",
                                         "MILLER",
                                         "WILSON",
                                         "MOORE",
                                         "TAYLOR",
                                         "ANDERSON",
                                         "THOMAS",
                                         "JACKSON",
                                         "WHITE",
                                         "HARRIS",
                                         "MARTIN",
                                         "THOMPSON",
                                         "GARCIA",
                                         "MARTINEZ",
                                         "ROBINSON",
                                         "CLARK",
                                         "RODRIGUEZ",
                                         "LEWIS",
                                         "LEE",
                                         "WALKER",
                                         "HALL",
                                         "ALLEN",
                                         "YOUNG",
                                         "HERNANDEZ",
                                         "KING",
                                         "WRIGHT",
                                         "LOPEZ",
                                         "HILL",
                                         "SCOTT",
                                         "GREEN",
                                         "ADAMS",
                                         "BAKER",
                                         "GONZALEZ",
                                         "NELSON",
                                         "CARTER",
                                         "MITCHELL",
                                         "PEREZ",
                                         "ROBERTS",
                                         "TURNER",
                                         "PHILLIPS",
                                         "CAMPBELL",
                                         "PARKER",
                                         "EVANS",
                                         "EDWARDS",
                                         "COLLINS",
                                         "STEWART",
                                         "SANCHEZ",
                                         "MORRIS",
                                         "ROGERS",
                                         "REED",
                                         "COOK",
                                         "MORGAN",
                                         "BELL",
                                         "MURPHY",
                                         "BAILEY",
                                         "RIVERA",
                                         "COOPER",
                                         "RICHARDSO",
                                         "COX",
                                         "HOWARD",
                                         "WARD",
                                         "TORRES",
                                         "PETERSON",
                                         "GRAY",
                                         "RAMIREZ",
                                         "JAMES",
                                         "WATSON",
                                         "BROOKS",
                                         "KELLY",
                                         "SANDERS",
                                         "PRICE",
                                         "BENNETT",
                                         "WOOD",
                                         "BARNES",
                                         "ROSS",
                                         "HENDERSON",
                                         "COLEMAN",
                                         "JENKINS",
                                         "PERRY",
                                         "POWELL",
                                         "LONG",
                                         "PATTERSON",
                                         "HUGHES",
                                         "FLORES",
                                         "WASHINGTO",
                                         "BUTLER",
                                         "SIMMONS",
                                         "FOSTER",
                                         "GONZALES",
                                         "BRYANT",
                                         "ALEXANDER",
                                         "RUSSELL",
                                         "GRIFFIN",
                                         "DIAZ",
                                         "HAYES",
                                     };
    }
}
