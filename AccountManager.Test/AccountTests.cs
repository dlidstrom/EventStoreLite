using System;
using AccountManager.Models;
using NUnit.Framework;

namespace AccountManager.Test
{
    [TestFixture]
    public class AccountTests
    {
        [Test]
        public void CanCreateNewAccount()
        {
            // Act
            var account = new Account("someone@domain.com");

            // Assert
            var events = account.GetUncommittedChanges();
            Assert.That(events.Length, Is.EqualTo(1));
            Assert.That(events[0], Is.InstanceOf<AccountCreated>());
        }

        [Test]
        public void InactiveAccountDoesNotValidatePasswords()
        {
            // Arrange
            var account = new Account("someone@domain.com");

            // Act
            try
            {
                account.ValidatePassword("some password");

                // Assert
                Assert.Fail("Should throw");
            }
            catch (InvalidOperationException)
            {
            }
        }

        [Test]
        public void CanActivateAccount()
        {
            // Arrange
            var account = new Account("someone@domain.com");

            // Act
            account.Activate("some password");

            // Assert
            var events = account.GetUncommittedChanges();
            Assert.That(events.Length, Is.EqualTo(2));
            Assert.That(events[1], Is.InstanceOf<AccountActivated>());
        }

        [Test]
        public void ActivatedAccountCanValidatePasswords()
        {
            // Arrange
            var account = new Account("someone@domain.com");

            // Act
            account.Activate("some password");

            // Assert
            account.ValidatePassword("");
        }

        [Test]
        public void InvalidatesFalsePassword()
        {
            // Arrange
            var account = new Account("someone@domain.com");

            // Act
            account.Activate("some password");

            // Assert
            Assert.That(account.ValidatePassword("invalid password"), Is.False);
        }

        [Test]
        public void ValidatesTruePassword()
        {
            // Arrange
            var account = new Account("someone@domain.com");

            // Act
            account.Activate("some password");

            // Assert
            Assert.That(account.ValidatePassword("some password"), Is.True);
        }
    }
}
