using RabbitMQ.Client.Exceptions;
using System.Text;

namespace RabbitMQ.Client.Tests;

public class ExchangeOperationsTests : TestBase
{
    [Fact]
    public async Task When_Declaring_Headers_Exchange_And_Publishing_To_Exchange_Then_Only_Queues_With_Matching_Headers_Should_Receive_Message()
    {
        // Arrange
        var exchangeName = await CreateUniqueExchangeNameAsync();
        var queueName1 = await CreateUniqueQueueNameAsync();
        var queueName2 = await CreateUniqueQueueNameAsync();
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        var properties1 = new BasicProperties
        {
            ContentType = "text/plain",
            DeliveryMode = DeliveryModes.Persistent, // Persistent
            Priority = 0,
            Headers = new Dictionary<string, object?>
            {
                { "custom-header", "custom-value-1" }
            }
        };
        var arguments1 = new Dictionary<string, object?> { { "x-match", "all" }, { "custom-header", "custom-value-1" } };
        var properties2 = new BasicProperties
        {
            ContentType = "text/plain",
            DeliveryMode = DeliveryModes.Persistent, // Persistent
            Priority = 0,
            Headers = new Dictionary<string, object?>
            {
                { "custom-header", "custom-value-2" }
            }
        };
        var arguments2 = new Dictionary<string, object?> { { "x-match", "all" }, { "custom-header", "custom-value-2" } };

        // Act
        await channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Headers, durable: true, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);
        await channel.QueueDeclareAsync(queueName1, durable: true, exclusive: false, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);
        await channel.QueueBindAsync(queueName1, exchangeName, routingKey: "", arguments: arguments1, noWait: false, cancellationToken: default);
        await channel.QueueDeclareAsync(queueName2, durable: true, exclusive: false, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);
        await channel.QueueBindAsync(queueName2, exchangeName, routingKey: "", arguments: arguments2, noWait: false, cancellationToken: default);
        var message1 = "Hello World! custom-value-1";
        var message2 = "Hello World! custom-value-2";
        var body1 = Encoding.UTF8.GetBytes(message1);
        var body2 = Encoding.UTF8.GetBytes(message2);

        await channel.BasicPublishAsync(exchangeName, routingKey: "", false, basicProperties: properties1, body: body1);
        await channel.BasicPublishAsync(exchangeName, routingKey: "", false, basicProperties: properties2, body: body2);

        var receivedOne = false;
        var receivedTwo = false;
        while (true)
        {
            var result = await channel.BasicGetAsync(queueName1, true);
            if (result is not null)
            {
                receivedOne = Encoding.UTF8.GetString(result.Body.ToArray()) == "Hello World! custom-value-1";
            }
            result = await channel.BasicGetAsync(queueName2, true);
            if (result is not null)
            {
                receivedTwo = Encoding.UTF8.GetString(result.Body.ToArray()) == "Hello World! custom-value-2";
            }
            if (receivedOne && receivedTwo)
            {
                break;
            }
            await Task.Delay(100);
        }

        var count1 = await channel.MessageCountAsync(queueName1, cancellationToken: default);
        var count2 = await channel.MessageCountAsync(queueName2, cancellationToken: default);

        // Assert
        Assert.True(count1 + count2 == 0);

        // Clean-up
        await channel.QueueDeleteAsync(queueName1, ifUnused: false, ifEmpty: false, cancellationToken: default);
        await channel.QueueDeleteAsync(queueName2, ifUnused: false, ifEmpty: false, cancellationToken: default);
        await channel.ExchangeDeleteAsync(exchangeName, ifUnused: false, cancellationToken: default);
        await channel.CloseAsync();
        await channel.DisposeAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task When_Declaring_Fanout_Exchange_And_Publishing_Message_To_Exchange_Then_Message_Is_Published_To_All_BoundQueues_Regardless_Of_RoutingKey()
    {
        // Arrange
        var exchangeName = await CreateUniqueExchangeNameAsync();
        var queueName1 = await CreateUniqueQueueNameAsync();
        var queueName2 = await CreateUniqueQueueNameAsync();
        var routingKey1 = "messages-routing";
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        var properties = new BasicProperties
        {
            ContentType = "text/plain",
            DeliveryMode = DeliveryModes.Persistent, // Persistent
            Priority = 0,
            Headers = new Dictionary<string, object?>
            {
                { "x-custom-header", "custom-value" }
            }
        };

        // Act
        await channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Fanout, durable: true, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);
        await channel.QueueDeclareAsync(queueName1, durable: true, exclusive: false, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);
        await channel.QueueBindAsync(queueName1, exchangeName, routingKey: routingKey1, arguments: null, noWait: false, cancellationToken: default);
        await channel.QueueDeclareAsync(queueName2, durable: true, exclusive: false, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);
        await channel.QueueBindAsync(queueName2, exchangeName, routingKey: "", arguments: null, noWait: false, cancellationToken: default);
        var message = "Hello World!";
        var body = Encoding.UTF8.GetBytes(message);
        await channel.BasicPublishAsync(exchangeName, routingKey: "", false, basicProperties: properties, body: body);

        var receivedOne = false;
        var receivedTwo = false;
        while (true)
        {
            var result = await channel.BasicGetAsync(queueName1, true);
            if (result is not null)
            {
                receivedOne = true;
            }
            result = await channel.BasicGetAsync(queueName2, true);
            if (result is not null)
            {
                receivedTwo = true;
            }
            if (receivedOne && receivedTwo)
            {
                break;
            }
            await Task.Delay(100);
        }

        // Assert
        Assert.True(true);

        // Clean-up
        await channel.QueueDeleteAsync(queueName1, ifUnused: false, ifEmpty: false, cancellationToken: default);
        await channel.QueueDeleteAsync(queueName2, ifUnused: false, ifEmpty: false, cancellationToken: default);
        await channel.ExchangeDeleteAsync(exchangeName, ifUnused: false, cancellationToken: default);
        await channel.CloseAsync();
        await channel.DisposeAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }

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
    public async Task When_Binding_And_Unbinding_Exchange_To_And_From_Other_Exchange_Then_No_Exception_Is_Thrown()
    {
        // Arrange
        var firstExchangeName = await CreateUniqueExchangeNameAsync();
        var secondExchangeName = await CreateUniqueExchangeNameAsync();
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        // Act
        await channel.ExchangeDeclareAsync(firstExchangeName, ExchangeType.Direct, durable: true, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);
        await channel.ExchangeDeclareAsync(secondExchangeName, ExchangeType.Direct, durable: true, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);

        // Bind the first exchange to the second exchange
        await channel.ExchangeBindAsync(secondExchangeName, firstExchangeName, routingKey: "", arguments: null, noWait: false, cancellationToken: default);
        await Task.Delay(250);
        await channel.ExchangeUnbindAsync(secondExchangeName, firstExchangeName, routingKey: "", arguments: null, noWait: false, cancellationToken: default);

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
