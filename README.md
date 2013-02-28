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
* Easily rebuild your read model store, for example after adding an event handler for a new read model

What you don't get:
* CQRS and the complexity it entails ([do you really need it?](http://www.cqrsinfo.com/is-cqrs-a-viable-solution))
* A service bus
* Eventual consistency for your read models (we perform updates to write model and read model in the same transaction)

Still interested? Read on!

## Installation

The library can be installed from NuGet: http://nuget.org/packages/EventStoreLite. It requires you to use RavenDB
and there's also an implicit dependency on Castle Windsor, the ioc container.

## Documentation

Have a look in the [wiki](https://github.com/dlidstrom/EventStoreLite/wiki).

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
