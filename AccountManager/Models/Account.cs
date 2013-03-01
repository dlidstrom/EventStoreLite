using System;
using EventStoreLite;

namespace AccountManager.Models
{
    public class Account : AggregateRoot
    {
        private IAccountImpl impl;

        public Account(string email)
        {
            if (email == null) throw new ArgumentNullException("email");
            this.ApplyChange(new AccountCreated(email));
        }

        public bool ValidatePassword(string password)
        {
            if (password == null) throw new ArgumentNullException("password");
            return impl.ValidatePassword(password);
        }

        public void Activate(string password)
        {
            if (password == null) throw new ArgumentNullException("password");
            impl.Activate(password);
        }

        private void Apply(AccountCreated e)
        {
            impl = new NotActivatedAccount(this.ApplyChange);
        }

        private void Apply(AccountActivated e)
        {
            impl = new ActivatedAccount(e);
        }
    }
}