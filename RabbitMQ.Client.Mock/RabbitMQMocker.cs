namespace RabbitMQ.Client.Mock;

public static class RabbitMQMocker
{
    private static FakeConnectionFactory? _connectionFactory;
    private static RabbitMQServerMonitor? _serverMonitor;
    /// <summary>
    /// Creates the main entry point for the mocking client. From this factory, we can create connections,
    /// and connections can create the channels we work with.
    /// </summary>
    /// <returns>An <see cref="IConnectionFactory"/> interface, providing access to the mocked RabbitMQ eco-system.</returns>
    public static Task<IConnectionFactory> CreateConnectionFactoryAsync()
    {
        if (_connectionFactory is null)
        {
            _connectionFactory = new FakeConnectionFactory();
        }
        return Task.FromResult<IConnectionFactory>(_connectionFactory);
    }

    /// <summary>
    /// This does not exist in the original RabbitMQ.Client library. It is a helper to get information
    /// about what happens in the server.
    /// </summary>
    public static RabbitMQServerMonitor ServerMonitor => _serverMonitor ??= new RabbitMQServerMonitor();
}
