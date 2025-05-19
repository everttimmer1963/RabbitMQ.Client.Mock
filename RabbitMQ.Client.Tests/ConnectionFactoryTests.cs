namespace RabbitMQ.Client.Tests;

public class ConnectionFactoryTests : TestBase
{
    [Fact]
    public async Task When_Creating_Connection_From_Factory_Then_IConnection_Is_Returned()
    {
        // Arrange
        // Act
        var connection = await  factory.CreateConnectionAsync();

        // Assert
        Assert.IsAssignableFrom<IConnection>(connection);
        Assert.True(connection.IsOpen);
        Assert.Equal(2047, connection.ChannelMax);
        Assert.Equal(7, connection.ClientProperties.Count);
        Assert.Equal(5672, connection.RemotePort);

        // Clean up
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }
}
