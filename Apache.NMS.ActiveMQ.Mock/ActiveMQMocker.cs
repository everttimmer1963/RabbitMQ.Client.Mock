namespace Apache.NMS.ActiveMQ.Mock;

public static class ActiveMQMocker
{
    private static INM? _connectionFactory;

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
    /// Creates the main entry point for the mocking client. From this factory, we can create connections,
    /// and connections can create the channels we work with.
    /// </summary>
    /// <returns>An <see cref="IConnectionFactory"/> interface, providing access to the mocked RabbitMQ eco-system.</returns>
    public static IConnectionFactory CreateConnectionFactory()
    {
        if (_connectionFactory is null)
        {
            _connectionFactory = new FakeConnectionFactory();
        }
        return _connectionFactory;
    }
}
