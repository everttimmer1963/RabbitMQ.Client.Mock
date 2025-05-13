using RabbitMQ.Client.Exceptions;

namespace RabbitMQ.Client.Tests;
public class QuorumQueuesTests : TestBase
{
    [Fact]
    public async Task When_Declaring_QuorumQueue_Without_QueueName_Then_Exception_Is_Thrown()
    {
        // Locals
        var queueName = string.Empty;
        var arguments = new Dictionary<string, object?>() { { "x-queue-type", "quorum" } };

        // Arrange
        var connection = await factory.CreateConnectionAsync("RabbitMQ.Client.Mock");
        var channel = await connection.CreateChannelAsync();

        // Act
        var act = async () =>
        {
            var result = await QueueDeclareAndBindAsync(channel, queueName, durable: false, exclusive: true, autoDelete: true, arguments: arguments);
        };

        // Assert
        await Assert.ThrowsAsync<OperationInterruptedException>(act);
    }

    [Fact]
    public async Task When_Declaring_QuorumQueue_Without_QueueName_Then_Queue_With_ServerAssigned_QueueName_Is_Created()
    {
        // Locals
        var queueName = await CreateUniqueQueueName();

        // Arrange
        var connection = await factory.CreateConnectionAsync("RabbitMQ.Client.Mock");
        var channel = await connection.CreateChannelAsync();

        // Act
        var result = await QueueDeclareAndBindAsync(channel, queueName, durable: false, exclusive: true, autoDelete: true);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.QueueName), $"Expected server assigned queue name, but a zero length string is returned.");
    }
}
