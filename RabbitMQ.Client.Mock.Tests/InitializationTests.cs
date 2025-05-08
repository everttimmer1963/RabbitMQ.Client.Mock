using RabbitMQ.Client;
using System.Text;

namespace RabbitMQ.Client.Mock.Tests;

public class InitializationTests : TestBase
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
}
