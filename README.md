# EventStoreLite

Event store that uses RavenDB for storing read model and write model.

## Installation

The library can be installed from NuGet: http://nuget.org/packages/EventStoreLite. It requires you to use RavenDB
and there's also an implicit dependency on Castle Windsor, the ioc container.

## Usage

Start by defining a domain model class that you wish to store as a write model. Here's an example domain class
representing an account.

Start by adding a test for creation scenario:

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

Here we expect to be able to create new accounts. We also expect an event of type `AccountCreated` to be applied
by the domain model.

Let's implement the `Account` class:

    public class Account : AggregateRoot<Account>
    {
        public Account(string email)
        {
            if (email == null) throw new ArgumentNullException("email");
        }
    }

Note the generic base class `AggregateRoot`, parameterized with the `Account` class itself. This turns `Account` into
a domain model class. To make the test pass we need to raise an `AccountCreated` event. This is done by calling
the base class method `Apply`:

    public class Account : AggregateRoot<Account>
    {
        public Account(string email)
        {
            this.ApplyChange(new AccountCreated(email));
        }
    }

We now need to define the event class `AccountCreated`:

    public class AccountCreated : Event<Account>
    {
        public string Email { get; set; }

        public AccountCreated(string email)
        {
            Email = email;
        }
    }

Note here that the event class derives from the generic base class `Event`, parameterized with the domain model class.
The reason for this is to tie the event class to the domain model class. It will become clear once we start subscribing
to events published from our domain model.

## Adding behaviour to `Account`

Let's add a scenario to the `Account` class. We want the account to be inactive until it has been activated.
Once activated we want to be able to verify passwords. This means we need to supply a password when activating the
account. An inactive account should not verify passwords.

First test:

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

This test currently fails to build. Let's implement it by adding the required method `CheckPassword`
with the expected behaviour:

    public class Account : AggregateRoot<Account>
    {
        private bool activated;

        public Account(string email)
        {
            this.ApplyChange(new AccountCreated(email));
        }

        public bool ValidatePassword(string password)
        {
            if (!activated) throw new InvalidOperationException("Cannot use inactive accounts to verify passwords");
            return false;
        }
    }

That should pass. Now we need a way to activate an account. Let's specify the behaviour and
add a password while we're at it:

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

To make it pass we need the following in our `Account` class:

    public void Activate(string password)
    {
        var @event = new AccountActivated();
        this.ApplyChange(@event);
    }

Here's the `AccountActivated` class:

    public class AccountActivated : Event<Account>
    {
    }

It is empty for now but will soon have some necessary data for password validations.

An activated account should be able to validate passwords. Let's specify this behaviour:

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

The assertion here is that no exception should be thrown. To implement it we have to add an event handler
to our domain model for the `AccountActivated` event:

    private void Apply(AccountActivated e)
    {
        activated = true;
    }

That's it, the test passes. Here's the complete domain model as of now:

    public class Account : AggregateRoot<Account>
    {
        private bool activated;

        public Account(string email)
        {
            this.ApplyChange(new AccountCreated(email));
        }

        public bool ValidatePassword(string password)
        {
            if (!activated) throw new InvalidOperationException("Cannot use inactive accounts to verify passwords");
            return false;
        }

        public void Activate(string password)
        {
            var @event = new AccountActivated();
            this.ApplyChange(@event);
        }

        private void Apply(AccountActivated e)
        {
            activated = true;
        }
    }

Now for the final piece of the puzzle: actually validating the password. Let's add the tests:

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

To implement this, I'm going to go out on a limb and introduce password hashing with salt.
Let's add this information to the `AccountActivated` event:

    public class AccountActivated : Event<Account>
    {
        public AccountActivated(Guid salt, string passwordHash)
        {
            Salt = salt;
            PasswordHash = passwordHash;
        }

        public Guid Salt { get; set; }

        public string PasswordHash { get; set; }
    }

Now update the `Account` class. Add the following method for password hashing:

    // password hashing
    private static string ComputeHashedPassword(Guid salt, string password)
    {
        string hashedPassword;
        using (var sha = SHA256.Create())
        {
            var computedHash = sha.ComputeHash(
                salt.ToByteArray().Concat(Encoding.Unicode.GetBytes(password)).ToArray());

            hashedPassword = Convert.ToBase64String(computedHash);
        }

        return hashedPassword;
    }

Now change the `Activate` method:

    public void Activate(string password)
    {
        var salt = Guid.NewGuid();
        var @event = new AccountActivated(salt, ComputeHashedPassword(salt, password));
        this.ApplyChange(@event);
    }

This should make `InvalidatesFalsePassword` pass. Now we'll fix the last test. Change the event handler
for `AccountActivated` to this:

    private void Apply(AccountActivated e)
    {
        activated = true;
        this.salt = e.Salt;
        this.passwordHash = e.PasswordHash;
    }

Add the two member variables required:

    private Guid salt;
    private string passwordHash;

Finally, update the `ValidatePassword` method:

    public bool ValidatePassword(string password)
    {
        if (!activated) throw new InvalidOperationException("Cannot use inactive accounts to verify passwords");
        return ComputeHashedPassword(this.salt, password) == passwordHash;
    }

With that all of our tests should pass and the domain model is complete. Or are there other behaviours you can think of?
Let me know!

## Complete `Account` class with the events

    public class Account : AggregateRoot<Account>
    {
        private bool activated;
        private Guid salt;
        private string passwordHash;

        public Account(string email)
        {
            this.ApplyChange(new AccountCreated(email));
        }

        public bool ValidatePassword(string password)
        {
            if (!activated) throw new InvalidOperationException("Cannot use inactive accounts to verify passwords");
            return ComputeHashedPassword(this.salt, password) == passwordHash;
        }

        public void Activate(string password)
        {
            var salt = Guid.NewGuid();
            var @event = new AccountActivated(salt, ComputeHashedPassword(salt, password));
            this.ApplyChange(@event);
        }

        private void Apply(AccountActivated e)
        {
            activated = true;
            this.salt = e.Salt;
            this.passwordHash = e.PasswordHash;
        }

        // password hashing
        private static string ComputeHashedPassword(Guid salt, string password)
        {
            string hashedPassword;
            using (var sha = SHA256.Create())
            {
                var computedHash = sha.ComputeHash(
                    salt.ToByteArray().Concat(Encoding.Unicode.GetBytes(password)).ToArray());

                hashedPassword = Convert.ToBase64String(computedHash);
            }

            return hashedPassword;
        }
    }

And the tests:

    [TestFixture]
    public class AccountTest
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


## Persisting to RavenDB

Coming later.

## Creating read models by subscribing to events

Coming later.

## Rebuilding read models

Coming later

# Contribute

There are a few things that you can do if you want to contribute.

* Clone the repository and check out the code.
* Report any issues you find.
* Contact me with any questions you might have.
* The library currently requires an ioc container. I'd like to be able to use it without one.
* Implement support for more ioc containers. Currently there's only support for Castle Windsor. Contribute with AutoFac, StructureMap, Unity, etc.

# License

This library is licensed under the MIT license. See License.txt.

# Author & contact details

Daniel Lidstrom (dlidstrom@gmail.com)
