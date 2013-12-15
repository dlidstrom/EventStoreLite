using System;
using EventStoreLite;

namespace AccountManager.Models
{
    public class AccountActivated : Event
    {
        public AccountActivated(Guid salt, string passwordHash)
        {
            Salt = salt;
            PasswordHash = passwordHash;
        }

        public Guid Salt { get; set; }

        public string PasswordHash { get; set; }
    }
}