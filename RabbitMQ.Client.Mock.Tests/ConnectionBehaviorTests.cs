using RabbitMQ.Client.Events;

namespace RabbitMQ.Client.Mock.Tests;

public class ConnectionBehaviorTests : TestBase
{
    [Fact]
    public async Task When_Subscribing_To_Connection_Events_Then_No_Exception_Is_Thrown()
    {
        // Arrange
        var connection = await factory.CreateConnectionAsync();
        AsyncEventHandler<CallbackExceptionEventArgs> callbackHandler = (_, _) => Task.CompletedTask;
        AsyncEventHandler<ShutdownEventArgs> shutdownHandler = (_, _) => Task.CompletedTask;

        // Act
        var ex = Record.Exception(() =>
        {
            connection.CallbackExceptionAsync += callbackHandler;
            connection.ConnectionShutdownAsync += shutdownHandler;
            connection.CallbackExceptionAsync -= callbackHandler;
            connection.ConnectionShutdownAsync -= shutdownHandler;
        });

        // Assert
        Assert.Null(ex);

        // Clean up
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task When_Closing_Connection_Then_All_Channels_Are_Closed()
    {
        // Arrange
        var connection = await factory.CreateConnectionAsync();
        var channel1 = await connection.CreateChannelAsync();
        var channel2 = await connection.CreateChannelAsync();

        // Act
        await connection.CloseAsync();

        // Assert
        Assert.False(connection.IsOpen);
        Assert.False(channel1.IsOpen);
        Assert.False(channel2.IsOpen);

        // Clean up
        await channel1.DisposeAsync();
        await channel2.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task When_Closing_Channel_Then_ChannelShutdownAsync_Handler_Is_Invoked()
    {
        // Arrange
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        var handlerInvocationCount = 0;

        AsyncEventHandler<ShutdownEventArgs> shutdownHandler = (_, _) =>
        {
            handlerInvocationCount++;
            return Task.CompletedTask;
        };

        channel.ChannelShutdownAsync += shutdownHandler;

        // Act
        await channel.CloseAsync();

        // Assert
        Assert.Equal(1, handlerInvocationCount);

        // Clean up
        channel.ChannelShutdownAsync -= shutdownHandler;
        await channel.DisposeAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }
}
