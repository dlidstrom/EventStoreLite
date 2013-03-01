using System.Web.Mvc;
using EventStoreLite;
using Raven.Client;

namespace AccountManager.Controllers
{
    public abstract class BaseController : Controller
    {
        protected IDocumentSession DocumentSession { get; private set; }

        protected IEventStoreSession EventStoreSession { get; private set; }

        protected EventStore EventStore { get; private set; }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            this.DocumentSession = MvcApplication.Container.Resolve<IDocumentSession>();
            this.EventStoreSession = MvcApplication.Container.Resolve<IEventStoreSession>();
            this.EventStore = MvcApplication.Container.Resolve<EventStore>();
        }

        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (filterContext.Exception == null)
                this.EventStoreSession.SaveChanges();
        }
    }
}