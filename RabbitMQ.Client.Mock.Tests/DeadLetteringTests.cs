using System.Text;

namespace RabbitMQ.Client.Mock.Tests;
public class DeadLetteringTests : TestBase
{
    [Fact]
    public async Task When_Rejecting_Consumed_Message_From_Queue_That_Has_A_DeadLetterQueue_Configured_Then_Message_Is_Routed_To_DeadletterQueue()
    {
        // Locals
        var mainQueueName = "Hello";
        var dlqQueueName = "Hello-DLQ";
        var exchange = $"{mainQueueName.ToLowerInvariant()}-xchg";
        var routingKey = $"{mainQueueName.ToLowerInvariant()}-rkey";
        var arguments = new Dictionary<string, object?>()
        {
            { "x-dead-letter-exchange", exchange },
            { "x-dead-letter-routing-key", routingKey }
        };

        // Arrange
        var connection = await factory.CreateConnectionAsync("RabbitMQ.Client.Mock");
        var channel = await connection.CreateChannelAsync();

        // declare exchange, dead-letter queue and regular queue.
        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Direct, durable: true, autoDelete: false);
        await QueueDeclareAndBindAsync(channel, dlqQueueName, exchange: exchange, bindingKey: routingKey, durable: true, exclusive: false, autoDelete: false);
        await QueueDeclareAndBindAsync(channel, mainQueueName, durable: true, exclusive: false, autoDelete: false, arguments: arguments);

        // Act
        // publish, read and nack the message
        await channel.BasicPublishAsync(string.Empty, mainQueueName, body: Encoding.UTF8.GetBytes("This is a test message."));
        var item = await channel.BasicGetAsync(mainQueueName, false);
        await channel.BasicNackAsync(item!.DeliveryTag, false, false);

        // Allow some time for the message to be delivered to the DLQ
        await Task.Delay(1000);

        var queueCount = (int)await channel.MessageCountAsync(mainQueueName);
        var dlqCount = (int)await channel.MessageCountAsync(dlqQueueName);

        // Assert
        Assert.NotNull(item);
        Assert.Equal(0, queueCount);
        Assert.Equal(1, dlqCount);

        // Cleanup
        await channel.QueueDeleteAsync(mainQueueName);
        await channel.QueueDeleteAsync(dlqQueueName);
        await channel.ExchangeDeleteAsync(exchange);
        await channel.DisposeAsync();
        await connection.DisposeAsync();
    }
}
