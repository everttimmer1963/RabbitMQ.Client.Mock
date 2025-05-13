using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace RabbitMQ.Client.Mock.Tests;

public class ConcurrencyTests : TestBase
{
    [Fact]
    public async Task When_Using_Multiple_Consumers_And_Producers_Simultanuously_Then_No_Interference_Occurs()
    {
        // Locals
        var TEST_COUNT = 1000;
        var publisherCount = 0;
        var consumerCount = 0;
        var testData = new ConcurrentQueue<string>();
        var received = new ConcurrentQueue<string>();
        for (int i = 0; i < TEST_COUNT; i++)
        {
            testData.Enqueue($"This is a test message with number {i}");
        }
        var queueName = await CreateUniqueQueueName();
        var exchange = "Hello-Exchange";

        // Arrange
        var connection = await factory.CreateConnectionAsync("RabbitMQ.Client.Mock");
        var firstProducer = await connection.CreateChannelAsync();
        var secondProducer = await connection.CreateChannelAsync();
        var firstConsumer = await connection.CreateChannelAsync();
        var firstConsumerTag = string.Empty;
        var secondConsumer = await connection.CreateChannelAsync();
        var secondConsumerTag = string.Empty;
        var thirdConsumer = await connection.CreateChannelAsync();
        var thirdConsumerTag = string.Empty;
        var fourthConsumer = await connection.CreateChannelAsync();
        var fourthConsumerTag = string.Empty;

        // declare the exchange and queue
        await firstProducer.ExchangeDeclareAsync(exchange, ExchangeType.Direct, durable: true, autoDelete: false);
        await firstProducer.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);
        await firstProducer.QueueBindAsync(queueName, exchange, queueName);
        await secondProducer.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);
        await secondProducer.QueueBindAsync(queueName, exchange, queueName);
        await firstConsumer.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);
        await secondConsumer.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);
        await thirdConsumer.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);
        await fourthConsumer.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);

        // Act
        var consumeTasks = new List<Task>()
        {
            Task.Run(async () =>
            {
                var consumer = new AsyncEventingBasicConsumer(firstConsumer);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    Interlocked.Increment(ref consumerCount);
                    received.Enqueue(message);
                    await firstConsumer.BasicAckAsync(ea.DeliveryTag, false);
                };
                firstConsumerTag = await firstConsumer.BasicConsumeAsync(queueName, false, consumer);
            }),
            Task.Run(async () =>
            {
                var consumer = new AsyncEventingBasicConsumer(secondConsumer);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    Interlocked.Increment(ref consumerCount);
                    received.Enqueue(message);
                    await secondConsumer.BasicAckAsync(ea.DeliveryTag, false);
                };
                secondConsumerTag = await secondConsumer.BasicConsumeAsync(queueName, false, consumer);
            }),
            Task.Run(async () =>
            {
                var consumer = new AsyncEventingBasicConsumer(thirdConsumer);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    Interlocked.Increment(ref consumerCount);
                    received.Enqueue(message);
                    await thirdConsumer.BasicAckAsync(ea.DeliveryTag, false);
                };
                thirdConsumerTag = await thirdConsumer.BasicConsumeAsync(queueName, false, consumer);
            }),
            Task.Run(async () =>
            {
                var consumer = new AsyncEventingBasicConsumer(fourthConsumer);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    Interlocked.Increment(ref consumerCount);
                    received.Enqueue(message);
                    await fourthConsumer.BasicAckAsync(ea.DeliveryTag, false);
                };
                fourthConsumerTag = await fourthConsumer.BasicConsumeAsync(queueName, false, consumer);
            })
        };
        var publishTasks = new List<Task>()
        {
            Task.Run(async () =>
            {
                while(testData.TryDequeue(out var message))
                {
                    Interlocked.Increment(ref publisherCount);
                    await firstProducer.BasicPublishAsync(exchange, queueName, body: Encoding.UTF8.GetBytes(message));
                }
            }),
            Task.Run(async () =>
            {
                while(testData.TryDequeue(out var message))
                {
                    Interlocked.Increment(ref publisherCount);
                    await secondProducer.BasicPublishAsync(exchange, queueName, body: Encoding.UTF8.GetBytes(message));
                }
            })
        };

        await WaitUntilAsync(() => consumerCount == TEST_COUNT);

        // Wait for all tasks to complete
        await Task.WhenAll(consumeTasks);
        await Task.WhenAll(publishTasks);

        // Cleanup
        // cancel the consumers
        await firstConsumer.BasicCancelAsync(firstConsumerTag);
        await secondConsumer.BasicCancelAsync(secondConsumerTag);
        await thirdConsumer.BasicCancelAsync(thirdConsumerTag);
        await fourthConsumer.BasicCancelAsync(fourthConsumerTag);

        // delete exchange and queue
        await firstProducer.QueueDeleteAsync(queueName);
        await firstProducer.ExchangeDeleteAsync(exchange);

        // and dispose the rest
        await firstConsumer.DisposeAsync();
        await secondConsumer.DisposeAsync();
        await thirdConsumer.DisposeAsync();
        await fourthConsumer.DisposeAsync();
        await firstProducer.DisposeAsync();
        await secondProducer.DisposeAsync();
        await connection.DisposeAsync();

        // Assert
        Assert.True(publisherCount == TEST_COUNT, $"Expected {TEST_COUNT} published messages, but published = {publisherCount}.");
        Assert.True(consumerCount == TEST_COUNT, $"Expected {TEST_COUNT} consumed messages, but actual consumed = {consumerCount}.");
    }
}
