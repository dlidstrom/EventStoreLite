using System.Web.Mvc;
using EventStoreLite;
using Raven.Client;

namespace AccountManager.Controllers
{
    public abstract class BaseController : Controller
    {
        protected IDocumentStore DocumentStore { get; private set; }

        protected IDocumentSession DocumentSession { get; private set; }

        protected IEventStoreSession EventStoreSession { get; private set; }

        protected EventStore EventStore { get; private set; }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            DocumentStore = MvcApplication.Container.Resolve<IDocumentStore>();
            DocumentSession = MvcApplication.Container.Resolve<IDocumentSession>();
            EventStoreSession = MvcApplication.Container.Resolve<IEventStoreSession>();
            EventStore = MvcApplication.Container.Resolve<EventStore>();
        }

        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (filterContext.Exception == null)
                EventStoreSession.SaveChanges();
        }
    }
}