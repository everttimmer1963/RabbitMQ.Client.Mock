using RabbitMQ.Client.Exceptions;

namespace RabbitMQ.Client.Tests;
public class QueueOperationsTests : TestBase
{
    [Fact]
    public async Task When_Declaring_Queue_Then_Queue_Should_Exist()
    {
        // Arrange
        var queueName = await CreateUniqueQueueNameAsync();
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        // Act
        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);
        var result = await channel.QueueDeclarePassiveAsync(queueName, cancellationToken: default);

        // Assert
        Assert.True(true);
        Assert.Equal(queueName, result.QueueName);
        Assert.Equal(0u, result.MessageCount);
        Assert.Equal(0u, result.ConsumerCount);

        // Clean-up
        await channel.QueueDeleteAsync(queueName, ifUnused: false, ifEmpty: false, cancellationToken: default);
        await channel.CloseAsync();
        await channel.DisposeAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task When_Declaring_Queue_That_Already_Exists_Then_No_Exception_Occurs()
    {
        // Arrange
        var queueName = await CreateUniqueQueueNameAsync();
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        // Act
        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: true, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);
        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: true, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);

        // Assert
        Assert.True(true);

        // Clean-up
        await channel.QueueDeleteAsync(queueName, ifUnused: false, ifEmpty: false, cancellationToken: default);
        await channel.CloseAsync();
        await channel.DisposeAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task When_Declaring_And_Binding_Queue_To_Default_Exchange_With_Default_RoutingKey_Throws_OperationInterruptedException()
    {
        // Arrange
        var queueName = await CreateUniqueQueueNameAsync();
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        // Act
        await channel.QueueDeclareAsync(queueName, durable: false, exclusive: false, autoDelete: true, arguments: null, noWait: false, cancellationToken: default);
        Func<Task> act = async () => await channel.QueueBindAsync(queueName, "", routingKey: "", arguments: null, noWait: false, cancellationToken: default);

        // Assert
        await Assert.ThrowsAsync<OperationInterruptedException>(act);

        // Clean-up
        connection = await factory.CreateConnectionAsync();
        channel = await connection.CreateChannelAsync();
        await channel.QueueDeleteAsync(queueName, ifUnused: false, ifEmpty: false, cancellationToken: default);
        await channel.CloseAsync();
        await channel.DisposeAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task When_Declaring_And_Binding_Queue_To_Named_Exchange_With_Default_RoutingKey_Throws_No_Exception()
    {
        // Arrange
        var exchangeName = await CreateUniqueExchangeNameAsync();
        var queueName = await CreateUniqueQueueNameAsync();
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Direct, durable: false, autoDelete: true, arguments: null, noWait: false, cancellationToken: default);

        // Act
        await channel.QueueDeclareAsync(queueName, durable: false, exclusive: false, autoDelete: true, arguments: null, noWait: false, cancellationToken: default);
        await channel.QueueBindAsync(queueName, exchangeName, routingKey: "", arguments: null, noWait: false, cancellationToken: default);

        // Assert
        Assert.True(true);

        // Clean-up
        await channel.QueueDeleteAsync(queueName, ifUnused: false, ifEmpty: false, cancellationToken: default);
        await channel.ExchangeDeleteAsync(exchangeName, ifUnused: false, cancellationToken: default);
        await channel.CloseAsync();
        await channel.DisposeAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task When_Binding_Queue_That_Does_Not_Exist_Then_Throws_OperationInterruptedException()
    {
        // Arrange
        var exchangeName = await CreateUniqueExchangeNameAsync();
        var queueName = await CreateUniqueQueueNameAsync();
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Direct, durable: false, autoDelete: true, arguments: null, noWait: false, cancellationToken: default);

        // Act
        Func<Task> act = async () => await channel.QueueBindAsync(queueName, exchangeName, routingKey: "");

        // Assert
        await Assert.ThrowsAsync<OperationInterruptedException>(act);

        // Clean-up
        connection = await factory.CreateConnectionAsync();
        channel = await connection.CreateChannelAsync();
        await channel.QueueDeleteAsync(queueName, ifUnused: false, ifEmpty: false, cancellationToken: default);
        await channel.ExchangeDeleteAsync(exchangeName, ifUnused: false, cancellationToken: default);
        await channel.CloseAsync();
        await channel.DisposeAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }
}
