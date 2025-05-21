using System.Text;

namespace RabbitMQ.Client.Mock.Tests;

public class PublisherTests : TestBase
{
    [Fact]
    public async Task When_Publishing_Message_To_Unbound_Queue_Then_Queue_Should_Contain_One_Message()
    {
        // Arrange
        const string textMessage = "Hello world, this is a message.";
        var queueName = await CreateUniqueQueueNameAsync();
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        var messageBody = Encoding.UTF8.GetBytes(textMessage);
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
        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);

        // Act (publish and readback, what proves that the message was stored.)
        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: queueName,
            false,
            basicProperties: properties,
            body: messageBody,
            cancellationToken: default);

        var result = await channel.BasicGetAsync(queueName, autoAck: true, cancellationToken: default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.Exchange);
        Assert.Equal(queueName, result.RoutingKey);
        Assert.Equal(textMessage, Encoding.UTF8.GetString(result.Body.ToArray()));

        // Clean-up
        await channel.QueueDeleteAsync(queueName, ifUnused: false, ifEmpty: false, cancellationToken: default);
        await channel.CloseAsync();
        await channel.DisposeAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task When_Publishing_Message_To_Exchange_With_Bound_Queue_Then_Queue_Should_Contain_One_Message()
    {
        // Arrange
        const string textMessage = "Hello world, this is a message.";
        var exchangeName = await CreateUniqueExchangeNameAsync();
        var queueName = await CreateUniqueQueueNameAsync();
        var routingKey = "text-messages-routing-key";
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        var messageBody = Encoding.UTF8.GetBytes(textMessage);
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
        await channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Direct, durable: true, autoDelete: false, arguments: null, cancellationToken: default);
        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);
        await channel.QueueBindAsync(queueName, exchangeName, routingKey: routingKey, arguments: null, noWait: false, cancellationToken: default);

        // Act (publish and readback, what proves that the message was stored.)
        await channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: routingKey,
            false,
            basicProperties: properties,
            body: messageBody,
            cancellationToken: default);

        var result = await channel.BasicGetAsync(queueName, autoAck: true, cancellationToken: default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(exchangeName, result.Exchange);
        Assert.Equal(routingKey, result.RoutingKey);
        Assert.Equal(textMessage, Encoding.UTF8.GetString(result.Body.ToArray()));

        // Clean-up
        await channel.QueueDeleteAsync(queueName, ifUnused: false, ifEmpty: false, cancellationToken: default);
        await channel.ExchangeDeleteAsync(exchangeName, ifUnused: false, cancellationToken: default);
        await channel.CloseAsync();
        await channel.DisposeAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task When_Publishing_Message_To_Exchange_With_Two_Bound_Queues_Then_Both_Queues_Should_Contain_One_Message()
    {
        // Arrange
        const string textMessage = "Hello world, this is a message.";
        var exchangeName = await CreateUniqueExchangeNameAsync();
        var queueName1 = await CreateUniqueQueueNameAsync();
        var queueName2 = await CreateUniqueQueueNameAsync();
        var routingKey = "text-messages-routing-key";
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        var messageBody = Encoding.UTF8.GetBytes(textMessage);
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
        await channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Direct, durable: true, autoDelete: false, arguments: null, cancellationToken: default);
        await channel.QueueDeclareAsync(queueName1, durable: true, exclusive: false, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);
        await channel.QueueDeclareAsync(queueName2, durable: true, exclusive: false, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);
        await channel.QueueBindAsync(queueName1, exchangeName, routingKey: routingKey, arguments: null, noWait: false, cancellationToken: default);
        await channel.QueueBindAsync(queueName2, exchangeName, routingKey: routingKey, arguments: null, noWait: false, cancellationToken: default);


        // Act (publish and readback, what proves that the message was stored.)
        await channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: routingKey,
            false,
            basicProperties: properties,
            body: messageBody,
            cancellationToken: default);

        var result1 = await channel.BasicGetAsync(queueName1, autoAck: true, cancellationToken: default);
        var result2 = await channel.BasicGetAsync(queueName2, autoAck: true, cancellationToken: default);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(exchangeName, result1.Exchange);
        Assert.Equal(exchangeName, result2.Exchange);
        Assert.Equal(routingKey, result1.RoutingKey);
        Assert.Equal(routingKey, result2.RoutingKey);
        Assert.Equal(textMessage, Encoding.UTF8.GetString(result1.Body.ToArray()));
        Assert.Equal(textMessage, Encoding.UTF8.GetString(result2.Body.ToArray()));

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
    public async Task When_Publishing_Message_To_Two_Bound_Exchanges_With_Two_Bound_Queues_Then_Both_Queues_Should_Contain_One_Message()
    {
        // Arrange
        const string textMessage = "Hello world, this is a message.";
        var exchangeName1 = await CreateUniqueExchangeNameAsync();
        var exchangeName2 = await CreateUniqueExchangeNameAsync();
        var queueName1 = await CreateUniqueQueueNameAsync();
        var queueName2 = await CreateUniqueQueueNameAsync();
        var routingKey = "text-messages-routing-key";
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        var messageBody = Encoding.UTF8.GetBytes(textMessage);
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
        await channel.ExchangeDeclareAsync(exchangeName1, ExchangeType.Direct, durable: true, autoDelete: false, arguments: null, cancellationToken: default);
        await channel.ExchangeDeclareAsync(exchangeName2, ExchangeType.Direct, durable: true, autoDelete: false, arguments: null, cancellationToken: default);
        await channel.ExchangeBindAsync(exchangeName2, exchangeName1, routingKey);
        await channel.QueueDeclareAsync(queueName1, durable: true, exclusive: false, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);
        await channel.QueueDeclareAsync(queueName2, durable: true, exclusive: false, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);
        await channel.QueueBindAsync(queueName1, exchangeName2, routingKey: routingKey, arguments: null, noWait: false, cancellationToken: default);
        await channel.QueueBindAsync(queueName2, exchangeName2, routingKey: routingKey, arguments: null, noWait: false, cancellationToken: default);


        // Act (publish and readback, what proves that the message was stored.)
        await channel.BasicPublishAsync(
            exchange: exchangeName1,
            routingKey: routingKey,
            false,
            basicProperties: properties,
            body: messageBody,
            cancellationToken: default);

        var result1 = await channel.BasicGetAsync(queueName1, autoAck: true, cancellationToken: default);
        var result2 = await channel.BasicGetAsync(queueName2, autoAck: true, cancellationToken: default);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(exchangeName1, result1.Exchange);
        Assert.Equal(exchangeName1, result2.Exchange);
        Assert.Equal(routingKey, result1.RoutingKey);
        Assert.Equal(routingKey, result2.RoutingKey);
        Assert.Equal(textMessage, Encoding.UTF8.GetString(result1.Body.ToArray()));
        Assert.Equal(textMessage, Encoding.UTF8.GetString(result2.Body.ToArray()));

        // Clean-up
        await channel.QueueDeleteAsync(queueName1, ifUnused: false, ifEmpty: false, cancellationToken: default);
        await channel.QueueDeleteAsync(queueName2, ifUnused: false, ifEmpty: false, cancellationToken: default);
        await channel.ExchangeDeleteAsync(exchangeName1, ifUnused: false, cancellationToken: default);
        await channel.ExchangeDeleteAsync(exchangeName2, ifUnused: false, cancellationToken: default);
        await channel.CloseAsync();
        await channel.DisposeAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }
}
