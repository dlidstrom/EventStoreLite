using System.Reflection;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using AccountManager.App_Start;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using EventStoreLite;
using EventStoreLite.IoC;
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
            var sessionComponent =
                Component.For<IDocumentSession>()
                         .UsingFactoryMethod(k => k.Resolve<IDocumentStore>().OpenSession())
                         .LifestylePerWebRequest();
            var esSessionComponent =
                Component.For<IEventStoreSession>()
                         .UsingFactoryMethod(
                             k =>
                             k.Resolve<EventStore>()
                              .OpenSession(
                                  k.Resolve<IDocumentStore>(), k.Resolve<IDocumentSession>()))
                         .LifestylePerWebRequest();
            Container =
                new WindsorContainer().Register(storeComponent, sessionComponent, esSessionComponent)
                                      .Install(
                                          EventStoreInstaller.FromAssembly(Assembly.GetExecutingAssembly()));
        }
    }
}