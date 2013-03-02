using System.Reflection;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using AccountManager.App_Start;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using EventStoreLite;
using EventStoreLite.IoC.Castle;
using Raven.Client;
using Raven.Client.Document;

namespace AccountManager
{
    public class MvcApplication : HttpApplication
    {
        public static IWindsorContainer Container { get; private set; }
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            // registers the document store, singleton lifestyle
            var storeComponent =
                Component.For<IDocumentStore>()
                         .UsingFactoryMethod(
                             k =>
                             new DocumentStore
                             {
                                 Url = "http://localhost:8082",
                                 DefaultDatabase = "EventStoreLite"
                             }.Initialize())
                         .LifestyleSingleton();

            // registers the document session, per web request lifestyle
            var sessionComponent = Component.For<IDocumentSession>().UsingFactoryMethod(
                k =>
                    {
                        var session = k.Resolve<IDocumentStore>().OpenSession();
                        session.Advanced.UseOptimisticConcurrency = true;
                        return session;
                    }).LifestylePerWebRequest();

            // registers the event store session, per web request
            var esSessionComponent =
                Component.For<IEventStoreSession>()
                         .UsingFactoryMethod(
                             k =>
                             k.Resolve<EventStore>()
                              .OpenSession(
                                  k.Resolve<IDocumentStore>(), k.Resolve<IDocumentSession>()))
                         .LifestylePerWebRequest();

            // registers event handlers from the current assembly
            var eventStoreInstaller = EventStoreInstaller.FromAssembly(Assembly.GetExecutingAssembly());
            Container =
                new WindsorContainer().Register(storeComponent, sessionComponent, esSessionComponent)
                                      .Install(eventStoreInstaller);
        }
    }
}