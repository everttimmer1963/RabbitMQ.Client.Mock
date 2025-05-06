using System.Collections.Concurrent;

namespace RabbitMQ.Client.Mock.Domain;

internal abstract class Exchange(string name, string type)
{
    protected readonly RabbitMQServer _server = RabbitMQServer.GetInstance();

    #region Properties
    public string Name { get; set; } = name;
    public string Type { get; set; } = type;
    public bool IsDurable { get; set; }
    public bool AutoDelete { get; set; }
    public IDictionary<string, object?> Arguments { get; } = new Dictionary<string, object?>();
    protected ConcurrentDictionary<string, IList<RabbitQueue>> QueueBindings { get; } = new ConcurrentDictionary<string, IList<RabbitQueue>>();
    protected ConcurrentDictionary<string, IList<Exchange>> ExchangeBindings { get; } = new ConcurrentDictionary<string, IList<Exchange>>();
    #endregion

    #region Exchange Bindings
    public virtual async ValueTask BindExchangeAsync(string exchange, string routingKey)
    {
        var exchangeInstance = await _server.GetExchangeAsync(exchange);
        if (exchangeInstance == null)
        {
            throw new ArgumentException($"Exchange {exchange} does not exist.");
        }
        var bindings = ExchangeBindings.GetOrAdd(routingKey, new List<Exchange>());
        bindings.Add(exchangeInstance);
    }

    public virtual async ValueTask UnbindExchangeAsync(string exchange, string routingKey)
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

        if ( bindings.Count == 0 && AutoDelete)
        {
            await CheckForSelfDestructAsync();
        }
    }
    #endregion

    #region Queue Bindings
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

    public virtual async ValueTask UnbindQueueAsync(string bindingKey, RabbitQueue queue)
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

        if ( AutoDelete )
        {
            await CheckForSelfDestructAsync();
        }
    }

    public virtual async ValueTask<IList<RabbitQueue>> GetBoundQueuesAsync(string bindingKey)
    {
        // first, test if the bindingkey corresponds to a single queue in this dead letter exchange.
        var queue = QueueBindings.Values
            .FirstOrDefault(qc => qc.FirstOrDefault(q => q.Name.Equals(bindingKey)) != null)?
            .First(queue => queue.Name.Equals(bindingKey));
        if (queue is not null)
        {
            return new List<RabbitQueue>() { queue };
        }

        // now, we try to get all bound queues for the bindingkey
        var bindings = QueueBindings.TryGetValue(bindingKey, out var queues) ? queues : null;
        if (bindings is not { Count: > 0 })
        {
            throw new ArgumentException($"No queues or bindings found for bindingkey {bindingKey}.");
        }
        return await Task.FromResult(bindings);
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

    public virtual async ValueTask<bool> PublishMessageAsync(RabbitMessage message)
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

    #endregion

    protected async ValueTask CheckForSelfDestructAsync()
    {
        if (QueueBindings.Count == 0 && ExchangeBindings.Count == 0 && AutoDelete)
        {
            await _server.ExchangeDeleteAsync(this);
        }
    }
}
