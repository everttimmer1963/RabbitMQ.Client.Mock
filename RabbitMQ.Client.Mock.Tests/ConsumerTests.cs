using RabbitMQ.Client.Events;
using System.Text;

namespace RabbitMQ.Client.Mock.Tests;

public class ConsumerTests : TestBase
{
    [Fact]
    public async Task When_Retrieving_Message_From_Queue_And_Nack_It_With_ReQueue_Option_Set_Then_Queue_Should_Contain_Message()
    {
        // Arrange
        var queueName = await CreateUniqueQueueNameAsync();
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);
        var message = "Hello world! This is a test message.";
        await channel.BasicPublishAsync(string.Empty, routingKey: queueName, Encoding.UTF8.GetBytes(message));

        // Act
        var result = await channel.BasicGetAsync(queueName, autoAck: false, cancellationToken: default);
        await channel.BasicNackAsync(result.DeliveryTag, false, true);
        var requeuedMessage = await channel.BasicGetAsync(queueName, autoAck: true, cancellationToken: default);

        // Assert
        Assert.NotNull(requeuedMessage);

        // Clean-up
        await channel.QueueDeleteAsync(queueName, ifUnused: false, ifEmpty: false, cancellationToken: default);
        await channel.CloseAsync();
        await channel.DisposeAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task When_Retrieving_Message_From_Empty_Queue_Then_Returns_Null()
    {
        // Arrange
        var queueName = await CreateUniqueQueueNameAsync();
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null, noWait: false, cancellationToken: default);

        // Act
        var result = await channel.BasicGetAsync(queueName, autoAck: true, cancellationToken: default);

        // Assert
        Assert.Null(result);

        // Clean-up
        await channel.QueueDeleteAsync(queueName, ifUnused: false, ifEmpty: false, cancellationToken: default);
        await channel.CloseAsync();
        await channel.DisposeAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task When_Publishing_X_Number_Of_Messages_To_Unbound_Queue_And_Consuming_Messages_Then_X_Number_Of_Messages_Are_Read()
    {
        // Arrange
        var testData = await CreateTestMessagesAsync(50);
        var queueName = await CreateUniqueQueueNameAsync();
        var connection = await factory.CreateConnectionAsync();
        var publisher = await connection.CreateChannelAsync();
        var consumer = await connection.CreateChannelAsync();
        await publisher.QueueDeclareAsync(queueName);
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
            messagesReceived++;
        };
        var consumerTag = await consumer.BasicConsumeAsync(queueName, true, consumerSink);

        foreach (var message in testData)
        {
            var body = System.Text.Encoding.UTF8.GetBytes(message);
            await publisher.BasicPublishAsync(string.Empty, queueName, false, properties, body);
            messagesPublished++;
        }

        await WaitUntilAsync(() => messagesReceived == messagesPublished, 10000, 100);

        // Assert
        Assert.Equal(messagesPublished, messagesReceived);

        // Clean-up
        await consumer.BasicCancelAsync(consumerTag);
        await publisher.QueueDeleteAsync(queueName);
        await publisher.CloseAsync();
        await consumer.CloseAsync();
        await consumer.DisposeAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task When_Publishing_X_Number_Of_Messages_To_Unbound_Queue_And_Consuming_Messages_With_Multiple_Consumers_Then_X_Number_Of_Messages_Are_Read()
    {
        // Arrange
        var testData = await CreateTestMessagesAsync(50);
        var queueName = await CreateUniqueQueueNameAsync();
        var connection = await factory.CreateConnectionAsync();
        var publisher = await connection.CreateChannelAsync();
        var consumerOne = await connection.CreateChannelAsync();
        var consumerTwo = await connection.CreateChannelAsync();
        await publisher.QueueDeclareAsync(queueName);
        var consumerSinkOne = new AsyncEventingBasicConsumer(consumerOne);
        var consumerSinkTwo = new AsyncEventingBasicConsumer(consumerTwo);
        var properties = new BasicProperties
        {
            ContentType = "text/plain",
            DeliveryMode = DeliveryModes.Persistent, // persistent
            Priority = 0,
            Headers = new Dictionary<string, object?> { { "x-match", "all" } }
        };
        var messagesPublished = 0;
        var messagesReceivedOne = 0;
        var messagesReceivedTwo = 0;

        // Act
        consumerSinkOne.ReceivedAsync += async (sender, args) =>
        {
            await Task.Delay(1);
            var body = args.Body.ToArray();
            var message = System.Text.Encoding.UTF8.GetString(body);
            Console.WriteLine($"Received message: {message}");
            messagesReceivedOne++;
        };
        consumerSinkTwo.ReceivedAsync += async (sender, args) =>
        {
            await Task.Delay(1);
            var body = args.Body.ToArray();
            var message = System.Text.Encoding.UTF8.GetString(body);
            Console.WriteLine($"Received message: {message}");
            messagesReceivedTwo++;
        };

        var consumerTagOne = await consumerOne.BasicConsumeAsync(queueName, true, consumerSinkOne);
        var consumerTagTwo = await consumerTwo.BasicConsumeAsync(queueName, true, consumerSinkTwo);

        foreach (var message in testData)
        {
            var body = System.Text.Encoding.UTF8.GetBytes(message);
            await publisher.BasicPublishAsync(string.Empty, queueName, false, properties, body);
            messagesPublished++;
        }

        await WaitUntilAsync(() => (messagesReceivedOne + messagesReceivedTwo) == messagesPublished, 10000, 100);

        // Assert
        Assert.Equal(messagesPublished, (messagesReceivedOne + messagesReceivedTwo));

        // Clean-up
        await consumerOne.BasicCancelAsync(consumerTagOne);
        await consumerTwo.BasicCancelAsync(consumerTagTwo);
        await publisher.QueueDeleteAsync(queueName);
        await publisher.CloseAsync();
        await consumerOne.CloseAsync();
        await consumerOne.DisposeAsync();
        await consumerTwo.CloseAsync();
        await consumerTwo.DisposeAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }
}
