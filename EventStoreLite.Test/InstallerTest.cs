using EventStoreLite.IoC;
using NUnit.Framework;

namespace EventStoreLite.Test
{
    public abstract class InstallerTest
    {
        protected static void VerifyContainer(IServiceLocator serviceLocator, int expected)
        {
            Assert.That(serviceLocator.Resolve(typeof(EventStore)), Is.Not.Null);
            var array = serviceLocator.ResolveAll(typeof(IEventHandler<AggregateChanged>));
            Assert.That(array, Is.Not.Null);
            Assert.That(array.Length, Is.EqualTo(expected));
            if (expected == 1)
                Assert.That(serviceLocator.Resolve(typeof(IEventHandler<AggregateChanged>)), Is.Not.Null);
        }
    }
}
