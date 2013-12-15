using System;
using System.Globalization;

namespace AccountManager.Models
{
    internal class ActivatedAccount : IAccountImpl
    {
        private readonly Guid salt;
        private readonly string passwordHash;

        public ActivatedAccount(AccountActivated e)
        {
            salt = e.Salt;
            passwordHash = e.PasswordHash;
        }

        public bool ValidatePassword(string password)
        {
            return string.Format("{0}/{1}", salt, password).GetHashCode().ToString(CultureInfo.InvariantCulture) == passwordHash;
        }

        public void Activate(string password)
        {
            throw new InvalidOperationException("Cannot activate an activated accout");
        }
    }
}