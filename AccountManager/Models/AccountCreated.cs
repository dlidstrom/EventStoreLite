using System.Linq;
using System;
using EventStoreLite;

namespace AccountManager.Models
{
    public class AccountCreated : Event
    {
        public string Email { get; set; }

        public AccountCreated(string email)
        {
            this.Email = email;
        }
    }
}