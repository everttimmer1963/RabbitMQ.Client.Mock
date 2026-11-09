# RabbitMQ.Client.Mock
Provides a way to mock the RabbitMQ.Client library for unit testing.

## Installation
Just add the RabbitMQ.Client.Mock NuGet package to your project.

## Usage
```csharp
// Creates the connection factory which is the entry point for the mock.
IConnectionFactory factory = RabbitMQMocker.CreateConnectionFactory();

// Create connections and channels as you would normally do.
IConnection connection = await factory.CreateConnectionAsync();
IChannel channel = await connection.CreateChannelAsync();
```

## Recent changes
To keep the mock aligned with the latest RabbitMQ client updates, the following changes were made:

- Updated the mock to be compatible with the latest `RabbitMQ.Client` API.
- Added support for the new channel close overload: `CloseAsync(ShutdownEventArgs reason, bool abort)`.
- Improved close behavior so closing a connection now closes child channels reliably.
- Improved event behavior for connection/channel event subscription and invocation.
- Added regression tests for these updated behaviors.

## Version numbering
The package versioning strategy was changed so the main `RabbitMQ.Client.Mock` package version matches the `RabbitMQ.Client` version it is intended to support.

Example:
- `RabbitMQ.Client.Mock` **7.2.2** is intended to be used with `RabbitMQ.Client` **7.2.2**.

Reason:
- Makes compatibility obvious at a glance.
- Reduces guesswork when selecting a mock package version.
- Simplifies dependency management during upgrades.

## Features
RabbitMQ.Client.Mock supports, or at least tries to support as much as scenarios as possible.
Some of the features include:

- Usage of multiple channels so you can mock having multiple consumers & producers that use their own channels.
- Mimics behaviour when deleting queues and exchanges.
- Mimics behaviour regarding client named queues, or server named queues.
- Correct routing of messages, including usage of exchange-to-exchange bindings.
- Supports Direct, Fanout, Headers & Topic exchanges.
- Usage of DeadLetter exchanges and queues.

## Note
The solutions contains 3 projects:

- RabbitMQ.Client.Mock: The actual mock implementation.
- RabbitMQ.Client.Mock.Tests: The unit tests for the mock implementation.
- RabbitMQ.Client.Tests: The same unit tests but pointing to an actual localhost based RabbitMQ service.
