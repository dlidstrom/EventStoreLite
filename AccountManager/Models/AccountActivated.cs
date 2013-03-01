using System.Linq;
using System;
using EventStoreLite;

namespace AccountManager.Models
{
    public class AccountActivated : Event
    {
        public AccountActivated(Guid salt, string passwordHash)
        {
            this.Salt = salt;
            this.PasswordHash = passwordHash;
        }

        public Guid Salt { get; set; }

        public string PasswordHash { get; set; }
    }
}