using EventStoreLite;

namespace AccountManager.Models
{
    public class AccountCreated : Event
    {
        public AccountCreated(string email)
        {
            Email = email;
        }

        public string Email { get; set; }
    }
}