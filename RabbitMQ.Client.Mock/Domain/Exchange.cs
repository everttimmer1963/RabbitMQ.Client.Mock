using System.Collections.Concurrent;

namespace RabbitMQ.Client.Mock.Domain;

internal abstract class Exchange(string name, string type)
{
    protected readonly RabbitMQServer _server = RabbitMQServer.GetInstance();
    public string Name { get; set; } = name;
    public string Type { get; set; } = type;
    public bool IsDurable { get; set; }
    public bool AutoDelete { get; set; }
    public IDictionary<string, object?> Arguments { get; } = new Dictionary<string, object?>();
    protected ConcurrentDictionary<string, IList<RabbitQueue>> QueueBindings { get; } = new ConcurrentDictionary<string, IList<RabbitQueue>>();
    protected ConcurrentDictionary<string, IList<Exchange>> ExchangeBindings { get; } = new ConcurrentDictionary<string, IList<Exchange>>();

    public virtual async ValueTask BindExchange(string exchange, string routingKey)
    {
        var exchangeInstance = await _server.GetExchangeAsync(exchange);
        if (exchangeInstance == null)
        {
            throw new ArgumentException($"Exchange {exchange} does not exist.");
        }
        var bindings = ExchangeBindings.GetOrAdd(routingKey, new List<Exchange>());
        bindings.Add(exchangeInstance);
    }
    public virtual ValueTask UnbindExchange(string exchange, string routingKey)
    {
        var bindings = ExchangeBindings.TryGetValue(routingKey, out var exchanges) ? exchanges : null;
        if (bindings is null)
        {
            throw new ArgumentException($"No bindings found for routingkey {routingKey}.");
        }

        var binding = bindings.FirstOrDefault(b => b.Name == exchange);
        if (binding is null)
        {
            throw new ArgumentException($"Exchange {exchange} not bound to exchange {Name}");
        }

        bindings.Remove(binding);
        return ValueTask.CompletedTask;
    }
    public virtual ValueTask BindQueueAsync(string bindingKey, RabbitQueue queue)
    {
        var bindings = QueueBindings.TryGetValue(bindingKey, out var queues) ? queues : null;
        if (bindings is null)
        {
            throw new ArgumentException($"No bindings found for bindingkey {bindingKey}.");
        }

        var binding = bindings.FirstOrDefault(q => q.Name.Equals(queue.Name));
        if (binding is not null)
        {
            return ValueTask.CompletedTask;
        }
        bindings.Add(queue);
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask UnbindQueue(string bindingKey, RabbitQueue queue)
    {
        var bindings = QueueBindings.TryGetValue(bindingKey, out var queues) ? queues : null;
        if (bindings is null)
        {
            throw new ArgumentException($"No bindings found for bindingkey {bindingKey}.");
        }

        var binding = bindings.FirstOrDefault(q => q.Name.Equals(queue.Name));
        if (binding is not null)
        {
            bindings.Remove(binding);
        }
        return ValueTask.CompletedTask;
    }

    public virtual IEnumerable<Exchange> EnumerateExchanges(RabbitMessage message)
    {
        var bindings = ExchangeBindings.TryGetValue(message.RoutingKey, out var result) ? result : null;
        if (bindings is null)
        {
            yield break;
        }
        foreach (var binding in bindings)
        { 
            yield return binding;
        }
    }

    public virtual IEnumerable<RabbitQueue> EnumerateQueues(RabbitMessage message)
    {
        var bindings = QueueBindings.TryGetValue(message.RoutingKey, out var result) ? result : null;
        if (bindings is null)
        {
            yield break;
        }
        foreach (var binding in bindings)
        {
            yield return binding;
        }
    }

    public async ValueTask<bool> PublishMessageAsync(RabbitMessage message)
    {
        var published = false;
        var exchanges = EnumerateExchanges(message).ToArray();
        if (exchanges.Length > 0)
        {
            foreach (var exchange in exchanges)
            { 
                published |= await exchange.PublishMessageAsync(message);
            }
        }
        var queues = EnumerateQueues(message).ToArray();
        if (queues.Length > 0)
        {
            foreach (var queue in queues)
            {
                published |= await queue.PublishMessageAsync(message);
            }
        }
        return published;
    }
}
