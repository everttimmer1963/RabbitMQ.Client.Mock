using System.Text;

namespace RabbitMQ.Client.Mock.Tests;

public class RoutingTests : TestBase
{
    [Fact]
    public async Task When_Publishing_To_Exchange_With_Multiple_Queues_Bound_Then_All_Queues_Should_Have_A_Copy()
    {
        // Locals
        var queueName1 = await CreateUniqueQueueName();
        var queueName2 = await CreateUniqueQueueName();
        var bindingKey = "messages";
        var exchangeName = "xchg-hello";

        // Arrange
        var connection = await factory.CreateConnectionAsync("RabbitMQ.Client.Mock");
        var channel = await connection.CreateChannelAsync();

        // Act
        // create and bind the queues
        await channel.ExchangeDeclareAsync(exchange: exchangeName, "direct", false, true);
        var result1 = await QueueDeclareAndBindAsync(channel, queueName1, exchangeName, bindingKey, durable: false, exclusive: true, autoDelete: true);
        var result2 = await QueueDeclareAndBindAsync(channel, queueName2, exchangeName, bindingKey, durable: false, exclusive: true, autoDelete: true);

        // publish message and allow some time for the message to be delivered to the queues
        await channel.BasicPublishAsync(exchange: exchangeName, routingKey: bindingKey, body: Encoding.UTF8.GetBytes("This is a test message."));

        // get counts
        var item1 = await channel.BasicGetAsync(result1.QueueName, true);
        var item2 = await channel.BasicGetAsync(result2.QueueName, true);

        // Assert
        Assert.NotNull(item1);
        Assert.NotNull(item2);
        Assert.Equal("This is a test message.", Encoding.UTF8.GetString(item1.Body.ToArray()));
        Assert.Equal("This is a test message.", Encoding.UTF8.GetString(item2.Body.ToArray()));

        // Cleanup (this will also delete the queues since they are none durable, and only exists during connection lifetime.)
        await channel.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task When_Publishing_To_Exchange_Then_Message_Should_Be_Published_To_All_Bound_Exchanges_And_Queues()
    {
        // Locals
        var queueName1 = await CreateUniqueQueueName();
        var queueName2 = await CreateUniqueQueueName();
        var bindingKey = "messages";
        var exchangeName1 = "xchg-hello-1";
        var exchangeName2 = "xchg-hello-2";

        // Arrange
        var connection = await factory.CreateConnectionAsync("RabbitMQ.Client.Mock");
        var channel = await connection.CreateChannelAsync();

        // Act
        // create and bind the queues
        await channel.ExchangeDeclareAsync(exchange: exchangeName1, "direct", false, true);
        await channel.ExchangeDeclareAsync(exchange: exchangeName2, "direct", false, true);
        await channel.ExchangeBindAsync(source: exchangeName1, destination: exchangeName2, routingKey: bindingKey);
        var result1 = await QueueDeclareAndBindAsync(channel, queueName1, exchangeName1, bindingKey, durable: false, exclusive: true, autoDelete: true);
        var result2 = await QueueDeclareAndBindAsync(channel, queueName2, exchangeName2, bindingKey, durable: false, exclusive: true, autoDelete: true);

        // publish message and allow some time for the message to be delivered to the queues
        await channel.BasicPublishAsync(exchange: exchangeName1, routingKey: bindingKey, body: Encoding.UTF8.GetBytes("This is a test message."));

        // get counts
        var item1 = await channel.BasicGetAsync(result1.QueueName, true);
        var item2 = await channel.BasicGetAsync(result2.QueueName, true);

        // Assert
        Assert.NotNull(item1);
        Assert.NotNull(item2);
        Assert.Equal("This is a test message.", Encoding.UTF8.GetString(item1.Body.ToArray()));
        Assert.Equal("This is a test message.", Encoding.UTF8.GetString(item2.Body.ToArray()));

        // Cleanup (this will also delete the queues since they are none durable, and only exists during connection lifetime.)
        await channel.DisposeAsync();
        await connection.DisposeAsync();
    }
}
