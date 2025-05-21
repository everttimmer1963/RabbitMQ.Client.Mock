using RabbitMQ.Client.Events;

namespace RabbitMQ.Client.Tests;

public class DeadLetteringTests : TestBase
{
    [Fact]
    public async Task When_Creating_Queue_And_DeadLetterQueue_And_Binding_The_DeadLetterQueue_With_Default_Exchange_And_RoutingKey_Then_Nacked_Messages_EndUp_In_DeadLetterQueue()
    { 
        // Arrange
        var testData = await CreateTestMessagesAsync(5);
        var queueName = await CreateUniqueQueueNameAsync();
        var exchangeName = await CreateUniqueExchangeNameAsync();
        var deadLetterQueueName = await CreateUniqueQueueNameAsync();
        var connection = await factory.CreateConnectionAsync();
        var consumer = await connection.CreateChannelAsync();
        var publisher = await connection.CreateChannelAsync();
        var arguments = new Dictionary<string, object?>
        {
            { "x-dead-letter-exchange", exchangeName },
            { "x-dead-letter-routing-key", deadLetterQueueName }
        };
        await publisher.QueueDeclareAsync(queueName, arguments: arguments);
        await publisher.QueueDeclareAsync(deadLetterQueueName);
        await publisher.ExchangeDeclareAsync(exchangeName, "direct", durable: true, autoDelete: false, arguments: null);
        await publisher.QueueBindAsync(deadLetterQueueName, exchangeName, deadLetterQueueName);
        var consumerSink = new AsyncEventingBasicConsumer(consumer);
        var properties = new BasicProperties
        {
            ContentType = "text/plain",
            DeliveryMode = DeliveryModes.Persistent, // persistent
            Priority = 0,
            Headers = new Dictionary<string, object?> { { "x-match", "all" } }
        };
        var messagesPublished = 0;
        var messagesReceived = 0;

        // Act
        consumerSink.ReceivedAsync += async (sender, args) =>
        {
            await Task.Delay(1);
            var body = args.Body.ToArray();
            var message = System.Text.Encoding.UTF8.GetString(body);
            Console.WriteLine($"Received message: {message}");
            await consumer.BasicNackAsync(args.DeliveryTag, false, false);
            messagesReceived++;
        };
        var consumerTag = await consumer.BasicConsumeAsync(queueName, false, consumerSink);

        foreach (var message in testData)
        {
            var body = System.Text.Encoding.UTF8.GetBytes(message);
            await publisher.BasicPublishAsync(string.Empty, queueName, false, properties, body);
            messagesPublished++;
        }

        await WaitUntilAsync(() => messagesReceived == messagesPublished, 10000, 100);
        var deadLetterCount = await consumer.MessageCountAsync(deadLetterQueueName);
        while (deadLetterCount < messagesReceived)
        {
            await Task.Delay(100);
            deadLetterCount = await consumer.MessageCountAsync(deadLetterQueueName);
        }

        // Assert
        Assert.True(true);

        // Clean-up
        await consumer.BasicCancelAsync(consumerTag);
        await consumer.QueueDeleteAsync(queueName, ifUnused: false, ifEmpty: false, cancellationToken: default);
        await consumer.QueueDeleteAsync(deadLetterQueueName, ifUnused: false, ifEmpty: false, cancellationToken: default);
        await consumer.ExchangeDeleteAsync(exchangeName, ifUnused: false, noWait: false, cancellationToken: default);
        await consumer.CloseAsync();
        await consumer.DisposeAsync();
        await publisher.CloseAsync();
        await publisher.DisposeAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task When_Creating_Queue_And_DeadLetterQueue_And_Binding_The_DeadLetterQueue_With_Default_Exchange_And_RoutingKey_Then_Acked_Messages_DoNot_EndUp_In_DeadLetterQueue()
    {
        // Arrange
        var testData = await CreateTestMessagesAsync(5);
        var queueName = await CreateUniqueQueueNameAsync();
        var exchangeName = await CreateUniqueExchangeNameAsync();
        var deadLetterQueueName = await CreateUniqueQueueNameAsync();
        var connection = await factory.CreateConnectionAsync();
        var consumer = await connection.CreateChannelAsync();
        var publisher = await connection.CreateChannelAsync();
        var arguments = new Dictionary<string, object?>
        {
            { "x-dead-letter-exchange", exchangeName },
            { "x-dead-letter-routing-key", deadLetterQueueName }
        };
        await publisher.QueueDeclareAsync(queueName, arguments: arguments);
        await publisher.QueueDeclareAsync(deadLetterQueueName);
        await publisher.ExchangeDeclareAsync(exchangeName, "direct", durable: true, autoDelete: false, arguments: null);
        await publisher.QueueBindAsync(deadLetterQueueName, exchangeName, deadLetterQueueName);
        var consumerSink = new AsyncEventingBasicConsumer(consumer);
        var properties = new BasicProperties
        {
            ContentType = "text/plain",
            DeliveryMode = DeliveryModes.Persistent, // persistent
            Priority = 0,
            Headers = new Dictionary<string, object?> { { "x-match", "all" } }
        };
        var messagesPublished = 0;
        var messagesReceived = 0;

        // Act
        consumerSink.ReceivedAsync += async (sender, args) =>
        {
            await Task.Delay(1);
            var body = args.Body.ToArray();
            var message = System.Text.Encoding.UTF8.GetString(body);
            Console.WriteLine($"Received message: {message}");
            await consumer.BasicAckAsync(args.DeliveryTag, false);
            messagesReceived++;
        };
        var consumerTag = await consumer.BasicConsumeAsync(queueName, false, consumerSink);

        foreach (var message in testData)
        {
            var body = System.Text.Encoding.UTF8.GetBytes(message);
            await publisher.BasicPublishAsync(string.Empty, queueName, false, properties, body);
            messagesPublished++;
        }

        await WaitUntilAsync(() => messagesReceived == messagesPublished, 10000, 100);
        var queueCount = await consumer.MessageCountAsync(queueName);
        var deadLetterCount = await consumer.MessageCountAsync(deadLetterQueueName);

        // Assert
        Assert.True(queueCount == 0);
        Assert.True(deadLetterCount == 0);

        // Clean-up
        await consumer.BasicCancelAsync(consumerTag);
        await consumer.QueueDeleteAsync(queueName, ifUnused: false, ifEmpty: false, cancellationToken: default);
        await consumer.QueueDeleteAsync(deadLetterQueueName, ifUnused: false, ifEmpty: false, cancellationToken: default);
        await consumer.ExchangeDeleteAsync(exchangeName, ifUnused: false, noWait: false, cancellationToken: default);
        await consumer.CloseAsync();
        await consumer.DisposeAsync();
        await publisher.CloseAsync();
        await publisher.DisposeAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }
}
