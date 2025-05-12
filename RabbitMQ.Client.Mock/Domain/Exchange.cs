using System.Collections.Concurrent;

namespace RabbitMQ.Client.Mock.Domain;

internal abstract class Exchange(string name, string type, int connectionNumber)
{
    private static object _lock = new();

    #region Properties
    protected RabbitMQServer Server => RabbitMQServer.GetInstance(connectionNumber);
    public string Name { get; set; } = name;
    public string Type { get; set; } = type;
    public bool IsDurable { get; set; }
    public bool AutoDelete { get; set; }
    public IDictionary<string, object?> Arguments { get; } = new Dictionary<string, object?>();
    public bool HasBindings => QueueBindings.Count > 0 || ExchangeBindings.Count > 0;
    protected ConcurrentDictionary<string, QueueBinding> QueueBindings { get; } = new ConcurrentDictionary<string, QueueBinding>();
    protected ConcurrentDictionary<string, ExchangeBinding> ExchangeBindings { get; } = new ConcurrentDictionary<string, ExchangeBinding>();
    #endregion

    #region Exchange Bindings
    public virtual async ValueTask BindExchangeAsync(string exchange, string routingKey, IDictionary<string, object?>? arguments = null)
    {
        var exchangeInstance = await Server.GetExchangeAsync(exchange);
        if (exchangeInstance == null)
        {
            throw new ArgumentException($"Exchange {exchange} does not exist.");
        }
        var bindings = ExchangeBindings.GetOrAdd(routingKey, new ExchangeBinding { Arguments = arguments });
        bindings.BoundExchanges.Add(exchangeInstance);
    }

    public virtual async ValueTask UnbindExchangeAsync(string exchange, string routingKey)
    {
        var binding = ExchangeBindings.TryGetValue(routingKey, out var eb) ? eb : null;
        if (binding is null)
        {
            throw new ArgumentException($"No bindings found for routingkey {routingKey}.");
        }

        var boundExchange = binding.BoundExchanges.FirstOrDefault(b => b.Name == exchange);
        if (boundExchange is null)
        {
            throw new ArgumentException($"Exchange {exchange} not bound to exchange {Name}");
        }

        binding.BoundExchanges.Remove(boundExchange);

        if (binding.BoundExchanges.Count == 0 && AutoDelete)
        {
            ExchangeBindings.TryRemove(routingKey, out _);
            await CheckForSelfDestructAsync();
        }
    }

    public ValueTask HandleExchangeDeleted(string exchange)
    {
        foreach (var binding in ExchangeBindings)
        {
            var boundExchange = binding.Value.BoundExchanges.FirstOrDefault(b => b.Name == exchange);
            if (boundExchange is not null)
            {
                binding.Value.BoundExchanges.Remove(boundExchange);
            }
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask HandleQueueDeleted(string queue)
    {
        lock (_lock)
        {
            foreach (var binding in QueueBindings)
            {
                var boundQueue = binding.Value.BoundQueues.FirstOrDefault(bq => bq.Name == queue);
                if (boundQueue is not null)
                {
                    binding.Value.BoundQueues.Remove(boundQueue);
                }
            }
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveAllBindings()
    {
        ExchangeBindings.Clear();
        QueueBindings.Clear();
        return ValueTask.CompletedTask;
    }
    #endregion

    #region Queue Bindings
    public virtual ValueTask BindQueueAsync(string bindingKey, RabbitQueue queue, IDictionary<string,object?>? arguments = null)
    {
        var binding = QueueBindings.TryGetValue(bindingKey, out var result) ? result : null;
        if (binding is null)
        {
            binding = new QueueBinding { Arguments = arguments };
            binding.BoundQueues.Add(queue);
            QueueBindings.TryAdd(bindingKey, binding);
            return ValueTask.CompletedTask;
        }

        var boundQueue = binding.BoundQueues.FirstOrDefault(q => q.Name.Equals(queue.Name));
        if (boundQueue is not null)
        {
            return ValueTask.CompletedTask;
        }

        binding.BoundQueues.Add(queue);
        return ValueTask.CompletedTask;
    }

    public virtual async ValueTask UnbindQueueAsync(string bindingKey, RabbitQueue queue)
    {
        var binding = QueueBindings.TryGetValue(bindingKey, out var result) ? result : null;
        if (binding is null)
        {
            throw new ArgumentException($"No binding found for bindingkey {bindingKey}.");
        }

        var boundQueue = binding.BoundQueues.FirstOrDefault(q => q.Name.Equals(queue.Name));
        if (boundQueue is not null)
        {
            binding.BoundQueues.Remove(boundQueue);
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
            .FirstOrDefault(qc => qc.BoundQueues.FirstOrDefault(q => q.Name.Equals(bindingKey)) != null)?
            .BoundQueues.First(queue => queue.Name.Equals(bindingKey));
        if (queue is not null)
        {
            return new List<RabbitQueue>() { queue };
        }

        // that didn't work, we try to get all bound queues for the bindingkey
        var binding = QueueBindings.TryGetValue(bindingKey, out var result) ? result : null;
        if (binding is not { BoundQueues.Count: > 0 })
        {
            throw new ArgumentException($"No queues or bindings found for bindingkey {bindingKey}.");
        }
        return await Task.FromResult(binding.BoundQueues);
    }

    public virtual IEnumerable<Exchange> EnumerateExchanges(RabbitMessage message)
    {
        var binding = ExchangeBindings.TryGetValue(message.RoutingKey, out var result) ? result : null;
        if (binding is null)
        {
            yield break;
        }
        foreach (var boundExchange in binding.BoundExchanges)
        { 
            yield return boundExchange;
        }
    }

    public virtual IEnumerable<RabbitQueue> EnumerateQueues(RabbitMessage message)
    {
        var binding = QueueBindings.TryGetValue(message.RoutingKey, out var result) ? result : null;
        if (binding is null)
        {
            yield break;
        }
        foreach (var boundQueue in binding.BoundQueues)
        {
            yield return boundQueue;
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
        if (published)
        {
            await Server.GetNextPublishSequenceNumber();
        }
        return published;
    }

    #endregion

    protected async ValueTask CheckForSelfDestructAsync()
    {
        if (QueueBindings.Count == 0 && ExchangeBindings.Count == 0 && AutoDelete)
        {
            await Server.ExchangeDeleteAsync(this);
        }
    }
}
