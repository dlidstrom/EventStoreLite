using System;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using AccountManager.Models;
using AccountManager.ReadModels;
using EventStoreLite;
using EventStoreLite.IoC.Castle;
using Raven.Client.Indexes;

namespace AccountManager.Controllers
{
    public class HomeController : BaseController
    {
        public ActionResult Index()
        {
            return this.View(DocumentSession.Query<AccountReadModel>().ToList());
        }

        public ActionResult AuditLog()
        {
            return this.View(DocumentSession.Query<AuditLogReadModel>().ToList());
        }

        [ChildActionOnly]
        public PartialViewResult Accounts()
        {
            return this.PartialView(DocumentSession.Load<AccountStatsReadModel>(AccountStatsReadModel.DbIdentifier));
        }

        public ActionResult CreateAccount()
        {
            return this.View();
        }

        [HttpPost]
        public ActionResult CreateAccount(string email)
        {
            var account = new Account(email);
            EventStoreSession.Store(account);
            return RedirectToAction("Index");
        }

        public ActionResult ActivateAccount(string id)
        {
            if (id == null) throw new ArgumentNullException("id");
            var accountReadModel = DocumentSession.Load<AccountReadModel>(id);
            if (accountReadModel == null) throw new HttpException(404, "Account not found");
            return this.View(accountReadModel);
        }

        [HttpPost]
        public ActionResult ActivateAccount(string aggregateId, string password)
        {
            if (aggregateId == null) throw new ArgumentNullException("aggregateId");
            if (password == null) throw new ArgumentNullException("password");
            var account = EventStoreSession.Load<Account>(aggregateId);
            if (account == null) throw new HttpException(404, "Account not found");
            account.Activate(password);
            return RedirectToAction("Index");
        }

        public ActionResult ReplayEvents()
        {
            return this.View();
        }

        [HttpPost, ActionName("ReplayEvents")]
        public ActionResult ReplayEventsConfirmed()
        {
            var windsorServiceLocator = new WindsorServiceLocator(MvcApplication.ChildContainer);
            EventStore.ReplayEvents(windsorServiceLocator);
            return this.View();
        }

        public ActionResult ResetIndexes()
        {
            return this.View();
        }

        [HttpPost, ActionName("ResetIndexes")]
        public ActionResult ResetIndexesConfirmed()
        {
            while (true)
            {
                var indexNames = DocumentStore.DatabaseCommands.GetIndexNames(0, 20);
                if (indexNames.Length == 0) break;
                foreach (var indexName in indexNames)
                {
                    DocumentStore.DatabaseCommands.DeleteIndex(indexName);
                }
            }

            // create indexes
            IndexCreation.CreateIndexes(Assembly.GetExecutingAssembly(), DocumentStore);
            EventStore.Initialize(DocumentStore);

            return this.RedirectToAction("Index");
        }
    }
}
