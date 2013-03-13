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
        /// <summary>
        /// This container is used to create components during the regular
        /// ASP.NET MVC request pipeline. It is configured to create a document
        /// session per web request, a popular lifestyle for database connections.
        /// </summary>
        public static IWindsorContainer Container { get; private set; }

        /// <summary>
        /// This container is used when replaying events. For this to work,
        /// the document session needs to be created as a transient component.
        /// RavenDB imposes some (safe-by-default) restrictions to limit the
        /// amount of requests per session. When replaying events a potentially
        /// large number of requests will be made to RavenDB. So we create a new
        /// document session for every x requests.
        /// </summary>
        public static IWindsorContainer ChildContainer { get; private set; }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            Container = CreateContainer();
            ChildContainer = CreateChildContainer(Container);
        }

        private static IWindsorContainer CreateContainer()
        {
            // registers the document store, singleton lifestyle
            var storeComponent = Component.For<IDocumentStore>().UsingFactoryMethod(
                k =>
                    {
                        var store =
                            new DocumentStore { Url = "http://localhost:8082", DefaultDatabase = "EventStoreLite" }
                                .Initialize();
                        store.Conventions.IdentityPartsSeparator = "-";
                        return store;
                    }).LifestyleSingleton();

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
                              .OpenSession(k.Resolve<IDocumentStore>(), k.Resolve<IDocumentSession>()))
                         .LifestylePerWebRequest();

            // registers event handlers from the current assembly
            var eventStoreInstaller = EventStoreInstaller.FromAssembly(Assembly.GetExecutingAssembly());
            var windsorContainer =
                new WindsorContainer().Register(storeComponent, sessionComponent, esSessionComponent)
                                      .Install(eventStoreInstaller);
            return windsorContainer;
        }

        private static IWindsorContainer CreateChildContainer(IWindsorContainer container)
        {
            var childContainer = new WindsorContainer();
            childContainer.Register(
                Component.For<IDocumentSession>()
                         .UsingFactoryMethod(kernel =>
                         {
                             var documentSession = kernel.Resolve<IDocumentStore>().OpenSession();
                             documentSession.Advanced.UseOptimisticConcurrency = true;
                             return documentSession;
                         })
                         .LifestyleTransient());
            container.AddChildContainer(childContainer);
            return childContainer;
        }
    }
}