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
