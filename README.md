# EventStoreLite

This is an event store implementation that uses RavenDB for storing both read models and write models. I imagine this
library being used when you are using RavenDB to start with and you want to model some complex domain. Since this
library is storing aggregate roots as documents it would not be suitable for domains with an extreme amount of events
for a single aggregate root. There are other, more suitable (and complex) libraries for that purpose
([Oliver's Event Store](https://github.com/joliver/EventStore) or [Greg Young's Event Store](http://geteventstore.com/)).

Sweet spots for this library:
* Possibility to model very complex domains
* You get to use only RavenDB (i.e. no relational store to manage)
* There's no service bus necessary (i.e. eventual consistency is not required for updating your read models)
* Write models and read models are updated in the same transaction (so they're not likely to get out-of-sync)
* You can use RavenDB's indexing features for your read models
* Easily rebuild your read model store, for example when adding a new read model

What you don't get:
* CQRS and the complexity it entails ([do you really need it?](http://www.cqrsinfo.com/is-cqrs-a-viable-solution))
* A service bus
* Eventual consistency for your read models (we perform updates to write model and read model in the same transaction)

Still interested? Read on!

## Installation

The library can be installed from NuGet: http://nuget.org/packages/EventStoreLite. It requires you to use RavenDB
and there's also an implicit dependency on Castle Windsor, the ioc container.

## Domain modeling using event sourcing

Let's implement a domain class representing an account. The account should start off being inactive. Activating
an account requires a password. Once activated, the account can validate passwords. Imagine this class being used
for registering and logon scenarios.

Start by adding a test for creation scenario:

```csharp
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
```

Here we expect to be able to create new accounts. We also expect an event of type `AccountCreated` to be raised
by the domain model.

Let's implement the `Account` class:

```csharp
public class Account : AggregateRoot<Account>
{
    public Account(string email)
    {
        if (email == null) throw new ArgumentNullException("email");
    }
}
```

Note the generic base class `AggregateRoot`, parameterized with the `Account` class itself. This turns `Account` into
a domain model class. To make the test pass we need to raise an `AccountCreated` event. This is done by calling
the base class method `ApplyChange`:

```csharp
public class Account : AggregateRoot<Account>
{
    public Account(string email)
    {
        this.ApplyChange(new AccountCreated(email));
    }
}
```

We now need to define the event class `AccountCreated`:

```csharp
public class AccountCreated : Event<Account>
{
    public string Email { get; set; }

    public AccountCreated(string email)
    {
        Email = email;
    }
}
```

Note here that the event class derives from the generic base class `Event`, parameterized with the domain model class.
The reason for this is to tie the event class to the domain model class. It will become clear why once we start
subscribing to events published from our domain model.

## Adding behaviour to `Account`

Let's add a scenario to the `Account` class. We want the account to be inactive until it has been activated.
Once activated we want to be able to validate passwords. This means we need to supply a password when activating the
account. An inactive account should not be able to validate passwords.

First test:

```csharp
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
```

This test currently fails to build. Let's fix it by adding the required method `CheckPassword`
with the expected behaviour:

```csharp
public class Account : AggregateRoot<Account>
{
    private bool activated;

    public Account(string email)
    {
        this.ApplyChange(new AccountCreated(email));
    }

    public bool ValidatePassword(string password)
    {
        if (!activated) throw new InvalidOperationException("Cannot use inactive accounts to validate passwords");
        return false;
    }
}
```

That should pass.

### Activating the account

Now we need a way to activate an account. Let's specify the behaviour and
add a password parameter while we're at it:

```csharp
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
```

To make it pass we need the following in our `Account` class:

```csharp
public void Activate(string password)
{
    var @event = new AccountActivated();
    this.ApplyChange(@event);
}
```

An event is raised by calling the `ApplyChange` base class method. This will later be used to dispatch the event
to event handlers when the domain class is persisted to RavenDB.

Here's the `AccountActivated` class:

```csharp
public class AccountActivated : Event<Account>
{
}
```

It is empty for now but will soon have some necessary data for password validations.

An activated account should be able to validate passwords. Let's specify this behaviour:

```csharp
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
```

The assertion here is that no exception should be thrown. To implement it we have to add an event handler
to our domain model for the `AccountActivated` event:

```csharp
private void Apply(AccountActivated e)
{
    activated = true;
}
```

That's it, the test passes. Here's the complete domain model as of now:

```csharp
public class Account : AggregateRoot<Account>
{
    private bool activated;

    public Account(string email)
    {
        this.ApplyChange(new AccountCreated(email));
    }

    public bool ValidatePassword(string password)
    {
        if (!activated) throw new InvalidOperationException("Cannot use inactive accounts to validate passwords");
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
```

### Account password validation

Now for the final piece of the puzzle: actually validating the password. Let's add the tests:

```csharp
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
```

To implement this, I'm going to go out on a limb and introduce salted password hashing (a controversial subject,
tricky to do "right").
Let's add this information to the `AccountActivated` event:

```csharp
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
```

Now update the `Account` class. Add the following method for password hashing:

```csharp
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
```

Now change the `Activate` method:

```csharp
public void Activate(string password)
{
    var salt = Guid.NewGuid();
    var @event = new AccountActivated(salt, ComputeHashedPassword(salt, password));
    this.ApplyChange(@event);
}
```

This should make `InvalidatesFalsePassword` pass. Now we'll fix the last test. Change the event handler
for `AccountActivated` to this:

```csharp
private void Apply(AccountActivated e)
{
    activated = true;
    this.salt = e.Salt;
    this.passwordHash = e.PasswordHash;
}
```

Add the two member variables required:

```csharp
private Guid salt;
private string passwordHash;
```

Finally, update the `ValidatePassword` method:

```csharp
public bool ValidatePassword(string password)
{
    if (!activated) throw new InvalidOperationException("Cannot use inactive accounts to verify passwords");
    return ComputeHashedPassword(this.salt, password) == passwordHash;
}
```

With that all of our tests should pass and the domain model is complete. Or are there other behaviours you can think of?
Let me know!

### Complete `Account` class with the events

```csharp
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
        if (!activated) throw new InvalidOperationException("Cannot use inactive accounts to validate passwords");
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
```

And the tests:

```csharp
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
```

## Persisting to RavenDB

### Configuring Castle Windsor

You need to configure Castle Windsor with components for RavenDB document store and document session, and finally
the event store. Here's an example for an ASP.NET MVC application:

```csharp
public class MvcApplication : HttpApplication
{
    public static IWindsorContainer Container { get; private set; }
    protected void Application_Start()
    {
        AreaRegistration.RegisterAllAreas();

        WebApiConfig.Register(GlobalConfiguration.Configuration);
        FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
        RouteConfig.RegisterRoutes(RouteTable.Routes);
        BundleConfig.RegisterBundles(BundleTable.Bundles);

        var storeComponent =
            Component.For<IDocumentStore>()
                     .UsingFactoryMethod(
                         k => new DocumentStore { Url = "http://localhost:8082" }.Initialize())
                         .LifestyleSingleton();
        var sessionComponent =
            Component.For<IDocumentSession>()
                     .UsingFactoryMethod(k => k.Resolve<IDocumentStore>().OpenSession())
                     .LifestylePerWebRequest();
        var esSessionComponent =
            Component.For<IEventStoreSession>()
                     .UsingFactoryMethod(
                         k => k.Resolve<EventStore>().OpenSession(k.Resolve<IDocumentSession>()))
                     .LifestylePerWebRequest();
        Container =
            new WindsorContainer().Register(storeComponent, sessionComponent, esSessionComponent)
                                  .Install(
                                      new EventStoreInstaller(
                                          Assembly.GetExecutingAssembly().GetTypes()));
    }
}
```

Observe the `new EventStoreIntaller(Assembly.GetExecutingAssembly().GetTypes())`. It will install event handlers
found in the executing assembly into the container. This is used by the event store to resolve event handlers and
dispatching events raised from domain models.

### Storing an instance of `Account`

```csharp
[HttpPost]
public ActionResult CreateAccount(string email)
{
    var account = new Account(email);
    var session = MvcApplication.Container.Resolve<IEventStoreSession>();
    session.Store(account);
    session.SaveChanges();
    return RedirectToAction("Index");
}
```

This will store an instance of `Account` into the event store. It will also dispatch the events raised by the domain
object, and we will see later how to handle these events using event handlers.

### Loading and activating an instance of `Account`

```csharp
public ActionResult ActivateAccount(string id, string password)
{
    var session = MvcApplication.Container.Resolve<IEventStoreSession>();
    var account = session.Load<Account>(id);
    if (account == null) throw new HttpException(404, "Account not found");
    account.Activate(password);
    session.SaveChanges();
    return RedirectToAction("Index");
}
```

This will load an instance of `Account`, verify that it existed and then proceed to activate it. The event store
will dispatch the events raised when activating the account to any event handlers registered at startup.

## Creating read models by subscribing to events

Let's define an event handler that creates read models for each account. Here's a sample:

```csharp
public class AccountReadModel : IReadModel
{
    public string Id { get; set; }

    public string Email { get; set; }

    public bool Activated { get; set; }
}

public class AccountHandler : IEventHandler<AccountCreated>,
                              IEventHandler<AccountActivated>
{
    private readonly IDocumentSession session;

    public AccountHandler(IDocumentSession session)
    {
        this.session = session;
    }

    public void Handle(AccountCreated e)
    {
        var id = GetId(e);
        session.Store(new AccountReadModel { Id = id, Email = e.Email, Activated = false });
    }

    public void Handle(AccountActivated e)
    {
        var id = GetId(e);
        var rm = session.Load<AccountReadModel>(id);
        rm.Activated = true;
    }

    private static string GetId(IDomainEvent e)
    {
        return "account-read-models/" + int.Parse(e.AggregateId.Substring(e.AggregateId.LastIndexOf('/') + 1));
    }
}
```

Note the interface `IEventHandler` which is derived from twice, once for each type of event. Also note that we can
use dependency injection here. `IDocumentSession` will be injected by Castle Windsor. Finally, the read model should
derive from `IReadModel`. This is used when rebuilding read models from scratch, as we'll see shortly.

We can now query RavenDB for `AccountReadModel`:

```csharp
public ActionResult Index()
{
    var session = MvcApplication.Container.Resolve<IDocumentSession>();
    return this.View(session.Query<AccountReadModel>().ToList());
}
```

Simple, eh?

## Rebuilding read models

Let's say we've stored a whole bunch of accounts. Now, we add the above event handler. We want it to handle all accounts,
back to the beginning of time. The event store allows us to do that, simply by:

```csharp
[HttpPost]
public ActionResult RebuildReadModels()
{
    MvcApplication.Container.Resolve<EventStore>().RebuildReadModels();
    return RedirectToAction("Index");
}
```

The call to `EventStore.RebuildReadModels` will clear any stored read models (that's why we derive from `IReadModel`,
to be able to find them using an index in RavenDB). Next, it will load all aggregate roots, one by one, and dispatch
the events. It might take a while but once done we will have up-to-date read models.

## Contribute

There are a few things that you can do if you want to contribute.

* Clone the repository and check out the code.
* Report any issues you find.
* Contact me with any questions you might have.
* The library currently requires an ioc container. I'd like to be able to use it without one.
* Implement support for more ioc containers. Currently there's only support for Castle Windsor. Contribute with AutoFac, StructureMap, Unity, etc.

## License

This library is licensed under the MIT license. See License.txt.

## Author & contact details

Daniel Lidstrom (dlidstrom@gmail.com)
