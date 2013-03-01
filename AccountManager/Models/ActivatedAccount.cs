using System.Globalization;
using System;

namespace AccountManager.Models
{
    internal class ActivatedAccount : IAccountImpl
    {
        private readonly Guid salt;
        private readonly string passwordHash;

        public ActivatedAccount(AccountActivated e)
        {
            this.salt = e.Salt;
            this.passwordHash = e.PasswordHash;
        }

        public bool ValidatePassword(string password)
        {
            return string.Format("{0}/{1}", this.salt, password).GetHashCode().ToString(CultureInfo.InvariantCulture) == this.passwordHash;
        }

        public void Activate(string password)
        {
            throw new InvalidOperationException("Cannot activate an activated accout");
        }
    }
}