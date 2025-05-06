using RabbitMQ.Client.Mock.Domain;

namespace RabbitMQ.Client.Mock;

internal class RabbitMQServer
{
    private static readonly object _lock = new();
    private static RabbitMQServer? _instance;

    private IDictionary<string, Exchange> Exchanges { get; } = new Dictionary<string, Exchange>();

    private IDictionary<string, RabbitQueue> Queues { get; } = new Dictionary<string, RabbitQueue>();

    public static RabbitMQServer GetInstance()
    {
        lock (_lock)
        {
            if (_instance == null)
            {
                _instance = new RabbitMQServer();
            }
        }
        return _instance;
    }

    public ValueTask<RabbitQueue?> GetQueueAsync(string queue)
    {
        return ValueTask.FromResult<RabbitQueue?>(Queues.TryGetValue(queue, out var result) ? result : null);
    }

    public ValueTask QueueDeclareAsync(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object?> arguments)
    {
        if (Queues.ContainsKey(queue))
        {
            throw new ArgumentException($"Queue {queue} already exists.");
        }
        var newQueue = new RabbitQueue(queue)
        {
            IsDurable = durable,
            IsExclusive = exclusive,
            AutoDelete = autoDelete
        };
        foreach (var argument in arguments)
        {
            newQueue.Arguments.Add(argument);
        }
        Queues.Add(queue, newQueue);
        return ValueTask.CompletedTask;
    }

    public ValueTask<Exchange?> GetExchangeAsync(string exchange)
    {
        return ValueTask.FromResult<Exchange?>(Exchanges.TryGetValue(exchange, out var result) ? result: null);
    }

    public ValueTask ExchangeDeleteAsync(Exchange exchange)
    {
        if (Exchanges.ContainsKey(exchange.Name))
        {
            Exchanges.Remove(exchange.Name);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask ExchangeDeclareAsync(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object?> arguments)
    {
        if (Exchanges.ContainsKey(exchange))
        {
            throw new ArgumentException($"Exchange {exchange} already exists.");
        }
        var newExchange = type switch
        {
            "direct" => new DirectExchange(exchange),
            _ => throw new ArgumentException($"Exchange type {type} is not supported.")
        };
        newExchange.IsDurable = durable;
        newExchange.AutoDelete = autoDelete;
        foreach (var argument in arguments)
        {
            newExchange.Arguments.Add(argument);
        }
        Exchanges.Add(exchange, newExchange);
        return ValueTask.CompletedTask;
    }
}
