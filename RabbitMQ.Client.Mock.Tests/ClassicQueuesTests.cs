using RabbitMQ.Client.Exceptions;
using System.Text;

namespace RabbitMQ.Client.Mock.Tests;

public class ClassicQueuesTests : TestBase
{
    [Fact]
    public async Task When_Initializing_Connection_And_Channel_Then_Correct_Instances_Are_Returned()
    {
        // Arrange
        var connection = await factory.CreateConnectionAsync("RabbitMQ.Client.Mock");
        var channel = await connection.CreateChannelAsync();

        // Act
        var connectionValid = (connection is IConnection);
        var channelValie = (channel is IChannel);

        // Assert
        Assert.True(connectionValid, "Connection is not a valid IConnection instance.");
        Assert.True(channelValie, "Channel is not a valid IChannel instance.");

        // Cleanup
        await channel.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task When_Declaring_ClassicQueue_Without_QueueName_Then_Queue_With_ServerAssigned_QueueName_Is_Created()
    {
        // Locals
        var queueName = string.Empty;

        // Arrange
        var connection = await factory.CreateConnectionAsync("RabbitMQ.Client.Mock");
        var channel = await connection.CreateChannelAsync();

        // Act
        var result = await QueueDeclareAndBindAsync(channel, queueName, durable: false, exclusive: true, autoDelete: true);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.QueueName), $"Expected server assigned queue name, but a zero length string is returned.");
    }

    [Fact]
    public async Task When_Declaring_Queue_And_Queueing_A_Message_Then_Queue_ShouldHave_One_Item()
    {
        // Locals
        var queueName = await CreateUniqueQueueName();

        // Arrange
        var connection = await factory.CreateConnectionAsync("RabbitMQ.Client.Mock");
        var channel = await connection.CreateChannelAsync();

        // Act
        var result = await channel.QueueDeclareAsync(queueName, durable: false, exclusive: true, autoDelete: true);
        await channel.BasicPublishAsync(exchange: string.Empty, routingKey: result.QueueName, body: Encoding.UTF8.GetBytes("This is a test message."));
        var item = await channel.BasicGetAsync(result.QueueName, true);

        // Assert
        Assert.NotNull(item);
        Assert.Equal("This is a test message.", Encoding.UTF8.GetString(item.Body.ToArray()));

        // Cleanup
        await channel.DisposeAsync();
        await connection.DisposeAsync();
    }
}
