namespace AccountManager.Models
{
    public interface IAccountImpl
    {
        bool ValidatePassword(string password);

        void Activate(string password);
    }
}