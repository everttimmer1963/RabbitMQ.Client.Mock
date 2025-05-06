using RabbitMQ.Client.Mock.Domain;
using System.Collections.Concurrent;

namespace RabbitMQ.Client.Mock;

internal class RabbitMQServer
{
    private readonly DirectExchange DefaultExchange = new DirectExchange(string.Empty)
    {
        IsDurable = false,
        AutoDelete = false
    };

    private static readonly object _instanceLock = new();
    private static RabbitMQServer? _instance;
    private int _lastDeliverTag = 0;
    
    private IDictionary<string, Exchange> Exchanges { get; } = new ConcurrentDictionary<string, Exchange>();

    private IDictionary<string, RabbitQueue> Queues { get; } = new ConcurrentDictionary<string, RabbitQueue>();

    private IDictionary<int, IChannel> Channels { get; } = new ConcurrentDictionary<int, IChannel>();

    private IDictionary<string, IAsyncBasicConsumer> Consumers { get; } = new ConcurrentDictionary<string, IAsyncBasicConsumer>();

    private IDictionary<ulong, IList<string>> PendingMessageBindings { get; } = new ConcurrentDictionary<ulong, IList<string>>();

    private IDictionary<string, IList<string>> ConsumerBindings { get; } = new ConcurrentDictionary<string, IList<string>>();

    public static RabbitMQServer GetInstance()
    {
        if (_instance == null)
        {
            _instance = new RabbitMQServer();
        }
        return _instance;
    }

    public RabbitMQServer()
    {
        Exchanges.Add(DefaultExchange.Name, DefaultExchange);
    }

    internal ValueTask<int> GetNextDeliveryTagAsync()
    {
        return ValueTask.FromResult(Interlocked.Increment(ref _lastDeliverTag));
    }

    internal ValueTask<RabbitQueue?> GetQueueAsync(string queue)
    {
        return ValueTask.FromResult<RabbitQueue?>(Queues.TryGetValue(queue, out var result) ? result : null);
    }

    internal ValueTask QueueDeclareAsync(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object?> arguments)
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

    internal ValueTask<Exchange?> GetExchangeAsync(string exchange)
    {
        return ValueTask.FromResult<Exchange?>(Exchanges.TryGetValue(exchange, out var result) ? result : null);
    }

    internal ValueTask<bool> ExchangeDeleteAsync(Exchange exchange)
    {
        if (Exchanges.ContainsKey(exchange.Name))
        {
            return ValueTask.FromResult(Exchanges.Remove(exchange.Name));
        }
        return ValueTask.FromResult(false);
    }

    internal ValueTask ExchangeDeclareAsync(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object?> arguments)
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

    internal ValueTask<bool> AddConsumerBindingAsync(string consumerTag, string queue)
    {
        var bindings = ConsumerBindings.TryGetValue(consumerTag, out var result) ? result : null;
        if (bindings is not null)
        {
            bindings.Add(queue);
            return ValueTask.FromResult(true);
        }
        bindings = new List<string> { queue };
        return ValueTask.FromResult(ConsumerBindings.TryAdd(consumerTag, bindings));
    }

    internal ValueTask<bool> RemoveConsumerBindingAsync(string consumerTag, string queue)
    {
        var bindings = ConsumerBindings.TryGetValue(consumerTag, out var result) ? result : null;
        if (bindings is not null)
        {
            return ValueTask.FromResult(bindings.Remove(queue));
        }
        return ValueTask.FromResult(false);
    }

    internal async ValueTask<bool> RegisterConsumerAsync(string consumerTag, string queue, bool autoAck, IDictionary<string, object?>? arguments, IAsyncBasicConsumer consumer)
    {
        var queueInstance = await GetQueueAsync(queue);
        if (queueInstance is null)
        {
            throw new ArgumentException($"Queue {queue} does not exist.");
        }
        Consumers.TryAdd(consumerTag, consumer);
        return await queueInstance.AddConsumerAsync(consumerTag, autoAck, arguments);
    }
    internal async ValueTask<IAsyncBasicConsumer?> UnregisterConsumerAsync(string consumerTag)
    {
        var bindings = ConsumerBindings.TryGetValue(consumerTag, out var result) 
            ? result.ToArray() 
            : null;

        if (bindings is null)
        {
            throw new ArgumentException($"Consumer {consumerTag} not found.");
        }

        foreach (var binding in bindings)
        { 
            var queueInstance = Queues.TryGetValue(binding, out var queue) ? queue : null;
            if (queueInstance is null)
            {
                continue;
            }
            await queueInstance.RemoveConsumerAsync(consumerTag);
        }
        Consumers.Remove(consumerTag, out var consumer);
        return consumer;
    }

    internal ValueTask<IAsyncBasicConsumer> GetConsumerAsync(string consumerTag)
    {
        return ValueTask.FromResult<IAsyncBasicConsumer>(Consumers.TryGetValue(consumerTag, out var result) ? result : throw new ArgumentException($"Consumer {consumerTag} was not found."));
    }

    internal ValueTask<bool> RegisterChannelAsync(IChannel channel)
    {
        Channels.Add(channel.ChannelNumber, channel);
        return ValueTask.FromResult(true);
    }

    internal ValueTask<bool> UnregisterChannelAsync(int channelNumber)
    {
        var channel = Channels.TryGetValue(channelNumber, out var result) ? result : null;
        if (channel is null)
        {
            throw new ArgumentException($"Channel {channelNumber} not found.");
        }
        return ValueTask.FromResult(Channels.Remove(channelNumber));
    }

    internal ValueTask<bool> AddPendingMessageBindingAsync(ulong deliveryTag, string queue)
    {
        var bindings = PendingMessageBindings.TryGetValue(deliveryTag, out var result) ? result : null;
        if (bindings is not null)
        {
            bindings.Add(queue);
            return ValueTask.FromResult(true);
        }

        bindings = new List<string> { queue };
        PendingMessageBindings.Add(deliveryTag, bindings);
        return ValueTask.FromResult(true);
    }

    internal ValueTask<bool> RemovePendingMessageBindingAsync(ulong deliveryTag, string queue)
    {
        var queues = PendingMessageBindings.TryGetValue(deliveryTag, out var result) ? result : null;
        if (queues is not null)
        {
            return ValueTask.FromResult(queues.Remove(queue));
        }
        return ValueTask.FromResult(false);
    }

    internal async ValueTask<bool> ConfirmMessageAsync(ulong deliveryTag)
    {
        var queues = PendingMessageBindings.TryGetValue(deliveryTag, out var result) ? result : null;
        if (queues is null)
        {
            return false;
        }

        var confirmed = false;
        foreach (var queue in queues)
        {
            var queueInstance = await GetQueueAsync(queue);
            if (queueInstance is null)
            {
                continue;
            }

            if (await queueInstance.ConfirmMessageAsync(deliveryTag))
            {
                confirmed = true;
            }
        }
        return confirmed;
    }

    internal async ValueTask<bool> RejectMessageAsync(ulong deliveryTag, bool requeue)
    {
        var queues = PendingMessageBindings.TryGetValue(deliveryTag, out var result) ? result : null;
        if (queues is null)
        {
            throw new ArgumentException($"Message with delivery tag {deliveryTag} could not be found.");
        }
        foreach (var queue in queues)
        {
            var queueInstance = await GetQueueAsync(queue);
            if (queueInstance is null)
            {
                continue;
            }
            await queueInstance.RejectMessageAsync(deliveryTag, requeue);
        }
        return true;
    }
}
