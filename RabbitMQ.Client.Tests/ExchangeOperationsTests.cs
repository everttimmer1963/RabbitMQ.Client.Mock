using RabbitMQ.Client.Exceptions;

namespace RabbitMQ.Client.Tests;

public class ExchangeOperationsTests : TestBase
{
    [Fact]
    public async Task When_Declaring_Exchange_Then_Exchange_Should_Exist()
    {
        // Arrange
        var exchangeName = await CreateUniqueExchangeNameAsync();
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        // Act
        await channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Direct, durable: true, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);
        await channel.ExchangeDeclarePassiveAsync(exchangeName, cancellationToken: default);

        // Assert
        Assert.True(true);

        // Clean-up
        await channel.ExchangeDeleteAsync(exchangeName, ifUnused: false, cancellationToken: default);
        await channel.CloseAsync();
        await channel.DisposeAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task When_Declaring_Exchange_That_Already_Exists_Then_No_Exception_Occurs()
    {
        // Arrange
        var exchangeName = await CreateUniqueExchangeNameAsync();
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        // Act
        await channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Direct, durable: true, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);
        await channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Direct, durable: true, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);

        // Assert
        Assert.True(true);

        // Clean-up
        await channel.ExchangeDeleteAsync(exchangeName, ifUnused: false, cancellationToken: default);
        await channel.CloseAsync();
        await channel.DisposeAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task When_Binding_Exchange_To_Exchange_That_Does_Not_Exist_Then_Throws_OperationInterruptedException()
    {
        // Arrange
        var firstExchangeName = await CreateUniqueExchangeNameAsync();
        var secondExchangeName = await CreateUniqueExchangeNameAsync();
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        // Act
        await channel.ExchangeDeclareAsync(firstExchangeName, ExchangeType.Direct, durable: true, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);
        Func<Task> act = async () => await channel.ExchangeBindAsync(secondExchangeName, firstExchangeName, routingKey: "", arguments: null, noWait: false, cancellationToken: default);

        // Assert
        await Assert.ThrowsAsync<OperationInterruptedException>(act);

        // Clean-up (we need to reconnect after an exception)
        connection = await factory.CreateConnectionAsync();
        channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeleteAsync(firstExchangeName, ifUnused: false, cancellationToken: default);
        await channel.CloseAsync();
        await channel.DisposeAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }


    [Fact]
    public async Task When_Binding_Exchange_That_Not_Exists_To_Other_Exchange_Then_Throws_OperationInterruptedException()
    {
        // Arrange
        var firstExchangeName = await CreateUniqueExchangeNameAsync();
        var secondExchangeName = await CreateUniqueExchangeNameAsync();
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        // Act
        await channel.ExchangeDeclareAsync(secondExchangeName, ExchangeType.Direct, durable: true, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);
        Func<Task> act = async () => await channel.ExchangeBindAsync(secondExchangeName, firstExchangeName, routingKey: "", arguments: null, noWait: false, cancellationToken: default);

        // Assert
        await Assert.ThrowsAsync<OperationInterruptedException>(act);

        // Clean-up (we need to reconnect after an exception)
        connection = await factory.CreateConnectionAsync();
        channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeleteAsync(secondExchangeName, ifUnused: false, cancellationToken: default);
        await channel.CloseAsync();
        await channel.DisposeAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task When_Binding_Exchange_To_Other_Exchange_Then_No_Exception_Is_Thrown()
    {
        // Arrange
        var firstExchangeName = await CreateUniqueExchangeNameAsync();
        var secondExchangeName = await CreateUniqueExchangeNameAsync();
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        // Act
        await channel.ExchangeDeclareAsync(firstExchangeName, ExchangeType.Direct, durable: true, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);
        await channel.ExchangeDeclareAsync(secondExchangeName, ExchangeType.Direct, durable: true, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);
        Func<Task> act = async () => await channel.ExchangeBindAsync(secondExchangeName, firstExchangeName, routingKey: "", arguments: null, noWait: false, cancellationToken: default);

        // Assert
        Assert.True(true);

        // Clean-up (we need to reconnect after an exception)
        await channel.ExchangeDeleteAsync(firstExchangeName, ifUnused: false, cancellationToken: default);
        await channel.ExchangeDeleteAsync(secondExchangeName, ifUnused: false, cancellationToken: default);
        await channel.CloseAsync();
        await channel.DisposeAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task When_Binding_Exchange_To_Default_Exchange_Then_No_Exception_Is_Thrown()
    {
        // Arrange
        var destinationExchange = await CreateUniqueExchangeNameAsync();
        var sourceExchange = string.Empty; 
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        // Act
        await channel.ExchangeDeclareAsync(destinationExchange, ExchangeType.Direct, durable: true, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);
        Func<Task> act = async () => await channel.ExchangeBindAsync(destinationExchange, sourceExchange, routingKey: "", arguments: null, noWait: false, cancellationToken: default);

        // Assert
        Assert.True(true);

        // Clean-up (we need to reconnect after an exception)
        await channel.ExchangeDeleteAsync(destinationExchange, ifUnused: false, cancellationToken: default);
        await channel.CloseAsync();
        await channel.DisposeAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task When_Binding_Default_Exchange_To_Other_Exchange_Then_OperationInterruptedException_Is_Thrown()
    {
        // Arrange
        var destinationExchange = string.Empty;
        var sourceExchange = await CreateUniqueExchangeNameAsync();
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        // Act
        await channel.ExchangeDeclareAsync(sourceExchange, ExchangeType.Direct, durable: true, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);
        Func<Task> act = async () => await channel.ExchangeBindAsync(destinationExchange, sourceExchange, routingKey: "", arguments: null, noWait: false, cancellationToken: default);

        // Assert
        await Assert.ThrowsAsync<OperationInterruptedException>(act);

        // Clean-up (we need to reconnect after an exception)
        connection = await factory.CreateConnectionAsync();
        channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeleteAsync(sourceExchange, ifUnused: false, cancellationToken: default);
        await channel.CloseAsync();
        await channel.DisposeAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }
}
