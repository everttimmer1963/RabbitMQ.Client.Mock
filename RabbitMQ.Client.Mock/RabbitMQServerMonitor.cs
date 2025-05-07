namespace RabbitMQ.Client.Mock;

public class RabbitMQServerMonitor
{
    private RabbitMQServer? _server;

    private RabbitMQServer Server => _server ??= RabbitMQServer.GetInstance();

    /// <summary>
    /// Indicates whether the queue specified with <paramref name="queueName"/> exists.
    /// </summary>
    /// <param name="queueName">The name of the queue to check for.</param>
    /// <returns><see langword="True"/> if the queue exists; otherwise <see langword="False"/>.</returns>
    public async ValueTask<bool> QueueExistsAsync(string queueName)
    {
        var queue = await Server.GetQueueAsync(queueName);
        return (queue is not null);
    }

    /// <summary>
    /// Indicates whether the exchange specified with <paramref name="exchangeName"/> exists.
    /// </summary>
    /// <param name="exchangeName">The name of the exchange to check.</param>
    /// <returns><see langword="True"/> if the exchange exists; otherwise <see langword="False"/>.</returns>
    public async ValueTask<bool> ExchangeExistsAsync(string exchangeName)
    {
        var exchange = await Server.GetExchangeAsync(exchangeName);
        return (exchange is not null);
    }

    /// <summary>
    /// Returns the number of messages in the queue specified with <paramref name="queueName"/>.
    /// </summary>
    /// <param name="queueName">The name of the queue to return the count for.</param>
    /// <returns>The number of messages in the specified queue.</returns>
    /// <exception cref="ArgumentException">Thrown when the queue cannot be found.</exception>
    public async ValueTask<uint> GetMessageCountAsync(string queueName)
    {
        var queue = await Server.GetQueueAsync(queueName);
        if (queue is null)
        {
            throw new ArgumentException($"Queue {queueName} does not exist.");
        }
        return await queue.CountAsync();
    }

    /// <summary>
    /// Gets the number of messages in the specified queue that are pending confirmation or rejection.
    /// </summary>
    /// <param name="queueName">The name of the queue to check.</param>
    /// <returns>The number of messages awaiting confirmation or rejection.</returns>
    /// <exception cref="ArgumentException">Thrown when the queue cannot be found.</exception>
    public async ValueTask<uint> GetPendingMessageCountAsync(string queueName)
    {
        var queue = await Server.GetQueueAsync(queueName);
        if (queue is null)
        {
            throw new ArgumentException($"Queue {queueName} does not exist.");
        }
        return await queue.PendingMessageCountAsync();
    }
}
