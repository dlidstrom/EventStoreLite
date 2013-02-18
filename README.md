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
            Apply(new AccountCreated(email));
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

That's it for now!

## Adding behaviour to `Account`

Coming later.

## Persisting to RavenDB

Coming later.

## Creating read models by subscribing to events

Coming later.

## Rebuilding read models

Coming later

# Contribute

There are a few things that you can do if you want to contribute.

* Clone the repository first.
* Report any issues you find.
* Contact me with any questions you might have.
* The library currently requires an ioc container. I'd like to be able to use it without one.
* Implement support for more ioc containers. Currently there's only support for Castle Windsor. Contribute with AutoFac, StructureMap, Unity, etc.

# Licence

This library is licensed under the MIT license. See License.txt.

# Author & contact details

Daniel Lidstrom (dlidstrom@gmail.com)
