using System.Globalization;
using System;
using EventStoreLite;

namespace AccountManager.Models
{
    internal class NotActivatedAccount : IAccountImpl
    {
        private readonly Action<Event> change;

        public NotActivatedAccount(Action<Event> change)
        {
            this.change = change;
        }

        public bool ValidatePassword(string password)
        {
            throw new InvalidOperationException("Cannot use inactive accounts to verify passwords");
        }

        public void Activate(string password)
        {
            var salt = Guid.NewGuid();
            var @event = new AccountActivated(salt, string.Format("{0}/{1}", salt, password).GetHashCode().ToString(CultureInfo.InvariantCulture));
            change(@event);
        }
    }
}