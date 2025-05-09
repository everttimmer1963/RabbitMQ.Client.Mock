using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Mock.Domain;
using System.Collections.Concurrent;

namespace RabbitMQ.Client.Mock;

internal class RabbitMQServer
{
    private static RabbitMQServer? _instance;
    private ulong _lastPublishSequenceNumber = 1;
    private SemaphoreSlim _deliveryTagsSemaphore = new(1, 1);

    private IDictionary<string, Exchange> Exchanges { get; } = new ConcurrentDictionary<string, Exchange>();

    private IDictionary<string, RabbitQueue> Queues { get; } = new ConcurrentDictionary<string, RabbitQueue>();

    private IDictionary<int, IChannel> Channels { get; } = new ConcurrentDictionary<int, IChannel>();

    private IDictionary<string, IAsyncBasicConsumer> Consumers { get; } = new ConcurrentDictionary<string, IAsyncBasicConsumer>();

    private IDictionary<ulong, IList<string>> PendingMessageBindings { get; } = new ConcurrentDictionary<ulong, IList<string>>();

    private IDictionary<string, IList<string>> ConsumerBindings { get; } = new ConcurrentDictionary<string, IList<string>>();

    private IDictionary<int, ulong> ChannelDeliveryTags { get; } = new ConcurrentDictionary<int, ulong>();

    public static RabbitMQServer GetInstance()
    {
        if (_instance == null)
        {
            _instance = new RabbitMQServer();
        }
        return _instance;
    }

    internal async ValueTask<ulong> GetNextDeliveryTagAsync(int channelNumber)
    {
        await _deliveryTagsSemaphore.WaitAsync();
        try
        {
            if (ChannelDeliveryTags.TryGetValue(channelNumber, out var deliveryTag))
            {
                deliveryTag++;
                ChannelDeliveryTags[channelNumber] = deliveryTag;
                return deliveryTag;
            }
            ChannelDeliveryTags.Add(channelNumber, 1);
            return 1;
        }
        finally
        {
            _deliveryTagsSemaphore.Release();
        }
    }

    internal ValueTask<ulong> GetNextPublishSequenceNumber()
    {
        return ValueTask.FromResult(Interlocked.Increment(ref _lastPublishSequenceNumber));
    }

    internal ValueTask<RabbitQueue?> GetQueueAsync(string queue)
    {
        return ValueTask.FromResult<RabbitQueue?>(Queues.TryGetValue(queue, out var result) ? result : null);
    }

    internal async ValueTask<uint> QueueDeleteAsync(string queue, bool ifUnUsed, bool ifEmpty)
    {
        var queueInstance = Queues.TryGetValue(queue, out var result) ? result : null;
        if (queueInstance is null)
        {
            throw new ArgumentException($"Queue {queue} not found.");
        }
        if (ifUnUsed && await queueInstance.ConsumerCountAsync() > 0)
        {
            throw new ArgumentException($"Queue {queue} is in use.");
        }
        if (ifEmpty && await queueInstance.CountAsync() > 0)
        {
            throw new ArgumentException($"Queue {queue} is not empty.");
        }

        // remove the queus from the server
        Queues.Remove(queue);

        // let all exchanges know that the queue has been deleted,
        // so they can remove bindings to the queue, if any.
        Exchanges.Values
            .Where(xchg => xchg.HasBindings)
            .ToList()
            .ForEach(xchg => xchg.HandleQueueDeleted(queue));

        // get the current count of messages in the queue for reporting purposes.
        var msgCount = await queueInstance.CountAsync();

        // dispose the queue.
        await queueInstance.DisposeAsync();

        return msgCount;
    }

    internal async ValueTask<QueueDeclareOk> QueueDeclarePassiveAsync(string queue)
    { 
        var queueInstance = Queues.TryGetValue(queue, out var result) ? result : null;
        if (queueInstance is null)
        {
            throw new ArgumentException($"Queue {queue} not found.");
        }
        var consumerCount = await queueInstance.ConsumerCountAsync();
        var messageCount = await queueInstance.CountAsync();
        return new QueueDeclareOk(queue, messageCount, (uint)consumerCount);
    }

    internal async ValueTask<QueueDeclareOk> QueueDeclareAsync(int connectionNumber, string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object?>? arguments = null, bool passive = false)
    {
        var serverGeneratedName = false;

        // if an empty string is passed in for queue name, the server creates a name.
        // this is only valid for classic queues
        if (queue == string.Empty)
        {
            // check if this ia a classic queue. if not, throw an exception.
            var isClassicQueue = await ConfirmQueueType("classic", arguments);
            if (!isClassicQueue)
            {
                var reason = new ShutdownEventArgs(ShutdownInitiator.Peer, 406, "Queue name cannot be null or empty for non-classic queues.");
                throw new OperationInterruptedException(reason);
            }

            // assign a server generated name to the queue.
            queue = $"sgq.{Guid.NewGuid().ToString("N")}";
            serverGeneratedName = true;
        }

        // chekc if the queue exists. if it does, report the message and consuemr count.
        if (Queues.ContainsKey(queue))
        {
            var queueInstance = Queues.TryGetValue(queue, out var result) ? result : null;
            if (queueInstance is null)
            {
                throw new ArgumentException($"Queue {queue} not found.");
            }
            var consumerCount = await queueInstance.ConsumerCountAsync();
            var messageCount = await queueInstance.CountAsync();
            return new QueueDeclareOk(queue, messageCount, (uint)consumerCount);
        }

        // create a new queue and report back.
        var newQueue = new RabbitQueue(queue, serverGeneratedName)
        {
            IsDurable = durable,
            IsExclusive = exclusive,
            AutoDelete = autoDelete,
            Connection = connectionNumber
        };
        if (arguments is { Count: > 0 })
        {
            arguments.ToList().ForEach(newQueue.Arguments.Add);
        }
        Queues.Add(queue, newQueue);
        return new QueueDeclareOk(queue, 0, 0);
    }

    internal ValueTask<Exchange?> GetExchangeAsync(string exchange)
    {
        return ValueTask.FromResult<Exchange?>(Exchanges.TryGetValue(exchange, out var result) ? result : null);
    }

    internal async ValueTask<bool> ExchangeDeleteAsync(string exchange, bool ifUnused = false)
    {
        var exchangeInstance = Exchanges.TryGetValue(exchange, out var result) ? result : null;
        if (exchangeInstance is null)
        {
            throw new ArgumentException($"Exchange {exchange} not found.");
        }

        // if the exchange has bound quues, and ifUnused is true, we cannot delete it.
        if (ifUnused && exchangeInstance.HasBindings)
        {
            return false;
        }

        // okay, now we delete the exchange.
        await ExchangeDeleteAsync(exchangeInstance);

        // the exchange has been deleted. now we need to remove the exchange from
        // all exchange bindings that have the deleted exchange as destination.
        Exchanges.Values
            .ToList()
            .ForEach(xchg => xchg.HandleExchangeDeleted(exchange));

        // finally we need to remove all exchanges and queues that are bound to this exchange.
        await exchangeInstance.RemoveAllBindings();

        return true;
    }

    internal ValueTask<bool> ExchangeDeleteAsync(Exchange exchange)
    {
        if (Exchanges.ContainsKey(exchange.Name))
        {
            return ValueTask.FromResult(Exchanges.Remove(exchange.Name));
        }
        return ValueTask.FromResult(false);
    }

    internal ValueTask ExchangeDeclareAsync(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object?>? arguments = null, bool passive = false)
    {
        if (Exchanges.ContainsKey(exchange))
        {
            if (passive)
            {
                return ValueTask.CompletedTask;
            }
            throw new ArgumentException($"Exchange {exchange} already exists.");
        }
        var newExchange = type switch
        {
            "direct" => new DirectExchange(exchange),
            _ => throw new ArgumentException($"Exchange type {type} is not supported.")
        };
        newExchange.IsDurable = durable;
        newExchange.AutoDelete = autoDelete;
        if (arguments is { Count: > 0 })
        {
            arguments.ToList().ForEach(newExchange.Arguments.Add);
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

    internal async ValueTask<bool> RejectMessageAsync(ulong deliveryTag, bool multiple, bool requeue)
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
            await queueInstance.RejectMessageAsync(deliveryTag, multiple, requeue);
        }
        return true;
    }

    internal async ValueTask<bool> SendToDeadLetterQueueIfExists(RabbitQueue origin, RabbitMessage message)
    {
        if (origin.Arguments is not { Count: > 0 })
        {
            return false;
        }
        var dlExchange = origin.Arguments.TryGetValue("x-dead-letter-exchange", out var xchg) ? (string)xchg!: string.Empty;
        var dlRoutingKey = origin.Arguments.TryGetValue("x-dead-letter-routing-key", out var rkey) ? (string)rkey! : message.RoutingKey;
        
        var exchangeInstance = Exchanges.TryGetValue(dlExchange, out var exchange) ? exchange : null;
        if (exchangeInstance is null)
        {
            throw new ArgumentException($"Dead letter exchange {dlExchange} not found.");
        }
        var dlQueues = await exchangeInstance.GetBoundQueuesAsync(dlRoutingKey);
        if (dlQueues is not { Count: > 0 })
        {
            throw new ArgumentException($"No queues or bindings found for bindingkey {dlRoutingKey}.");
        }

        foreach (var  queue in dlQueues)
        {
            await queue.PublishMessageAsync(message);
        }
        return true;
    }

    private ValueTask<bool> ConfirmQueueType(string expectedType, IDictionary<string, object?>? arguments)
    {
        var type = (arguments is null)
            ? "classic"
            : arguments.TryGetValue("x-queue-type", out var result) 
                ? result?.ToString()?.ToLowerInvariant() ?? "classic"
                : "classic";

        return type.Equals(expectedType, StringComparison.OrdinalIgnoreCase)
            ? ValueTask.FromResult(true)
            : ValueTask.FromResult(false);
    }

    internal async ValueTask HandleDisconnectAsync(int connectionNumber)
    {
        // queues that are exclusive are deleted when the connection that created them is closed.
        var exclusiveQueues = Queues.Values
            .Where(queue => queue.IsExclusive && queue.Connection == connectionNumber)
            .ToList();

        foreach (var queue in exclusiveQueues)
        {
            await QueueDeleteAsync(queue.Name, false, false);
        }
    }
}
