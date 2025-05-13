using RabbitMQ.Client.Events;
using RabbitMQ.Client.Mock.Domain;
using System.Diagnostics;

namespace RabbitMQ.Client.Mock;
internal class FakeChannel : IChannel, IDisposable, IAsyncDisposable
{
    private bool _disposed;
    private readonly CreateChannelOptions? _options;
    
    private static int _lastChannelNumber = 0;
    private static int GetNextChannelNumber()
    { 
        return Interlocked.Increment(ref _lastChannelNumber);
    }

    public FakeChannel(CreateChannelOptions? options, int connectionNumber)
    {
        _options = options;
        ChannelNumber = GetNextChannelNumber();
        ConnectionNumber = connectionNumber;
    }

    private RabbitMQServer Server => RabbitMQServer.GetInstance(ConnectionNumber);

    public int ConnectionNumber { get; private set; }

    public int ChannelNumber { get; private set; }

    public ShutdownEventArgs? CloseReason { get; private set; }

    public IAsyncBasicConsumer? DefaultConsumer { get; set; }

    public bool IsClosed { get; private set; }

    public bool IsOpen { get; private set; }

    public string? CurrentQueue { get; private set; }

    public TimeSpan ContinuationTimeout { get; set; }

    private AsyncEventingWrapper<BasicAckEventArgs> _basicAcksAsyncWrapper;
    private AsyncEventingWrapper<BasicNackEventArgs> _basicNacksAsyncWrapper;
    private AsyncEventingWrapper<BasicReturnEventArgs> _basicReturnAsyncWrapper;
    private AsyncEventingWrapper<CallbackExceptionEventArgs> _callbackExceptionAsyncWrapper;
    private AsyncEventingWrapper<FlowControlEventArgs> _flowControlAsyncWrapper;
    private AsyncEventingWrapper<ShutdownEventArgs> _channelShutdownAsyncWrapper;

    public event AsyncEventHandler<BasicAckEventArgs> BasicAcksAsync = null!;
    public event AsyncEventHandler<BasicNackEventArgs> BasicNacksAsync = null!;
    public event AsyncEventHandler<ShutdownEventArgs> ChannelShutdownAsync = null!;
    public event AsyncEventHandler<BasicReturnEventArgs> BasicReturnAsync
    { 
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }
    public event AsyncEventHandler<CallbackExceptionEventArgs> CallbackExceptionAsync
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }
    public event AsyncEventHandler<FlowControlEventArgs> FlowControlAsync
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    public async ValueTask BasicAckAsync(ulong deliveryTag, bool multiple, CancellationToken cancellationToken = default)
    {
        if (multiple)
        {
            while (await Server.ConfirmMessageAsync(deliveryTag))
            {
                deliveryTag--;
            }
            return;
        }
        await Server.ConfirmMessageAsync(deliveryTag);
    }

    public async Task BasicCancelAsync(string consumerTag, bool noWait = false, CancellationToken cancellationToken = default)
    {
        var consumer = await Server.UnregisterConsumerAsync(consumerTag);
        if (consumer is null)
        {
            throw new ArgumentException($"Consumer {consumerTag} not found.");
        }
        await consumer.HandleBasicCancelOkAsync(consumerTag);
    }

    public async Task<string> BasicConsumeAsync(string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object?>? arguments, IAsyncBasicConsumer consumer, CancellationToken cancellationToken = default)
    {
        return await Server.RegisterConsumerAsync(consumerTag, queue, autoAck, arguments, consumer);
    }

    public async Task<BasicGetResult?> BasicGetAsync(string queue, bool autoAck, CancellationToken cancellationToken = default)
    {
        var queueInstance = await Server.GetQueueAsync(queue);
        if (queueInstance is null)
        {
            throw new ArgumentException($"Queue {queue} not found.");
        }
        var message = await queueInstance.ConsumeMessageAsync(autoAck);
        if ( message is null)
        {
            return null;
        }
        return new BasicGetResult(
            deliveryTag: message.DeliveryTag,
            redelivered: false,
            exchange: message.Exchange,
            routingKey: message.RoutingKey,
            messageCount: await queueInstance.CountAsync(),
            basicProperties: message.BasicProperties,
            body: message.Body);
    }

    public async ValueTask BasicNackAsync(ulong deliveryTag, bool multiple, bool requeue, CancellationToken cancellationToken = default)
    {
        await Server.RejectMessageAsync(deliveryTag, multiple, requeue);
    }

    public async ValueTask BasicPublishAsync<TProperties>(string exchange, string routingKey, bool mandatory, TProperties basicProperties, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default) where TProperties : IReadOnlyBasicProperties, IAmqpHeader
    {
        var message = new RabbitMessage
        { 
            Exchange = exchange,
            RoutingKey = routingKey,
            Mandatory = mandatory,
            Immediate = true,
            BasicProperties = basicProperties,
            Body = body.ToArray(),
            DeliveryTag = await GetNextDeliveryTagAsync(),
        };

        // check the exchange, if the exchange is an empty string, we need to publish to a queue with the name
        // specified in routingkey
        if (exchange == string.Empty)
        { 
            var queueInstance = await Server.GetQueueAsync(routingKey);
            if (queueInstance is null)
            {
                throw new ArgumentException($"Queue {routingKey} not found.");
            }
            await queueInstance.PublishMessageAsync(message);
            return;
        }

        // we need to publish to an exchange. the routingkey will have the bindingkey for the bound queues.
        var exchangeInstance = await Server.GetExchangeAsync(exchange);
        if (exchangeInstance is null)
        {
            throw new ArgumentException($"Exchange {exchange} not found.");
        }
        await exchangeInstance.PublishMessageAsync(message);
    }

    public async ValueTask BasicPublishAsync<TProperties>(CachedString exchange, CachedString routingKey, bool mandatory, TProperties basicProperties, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default) where TProperties : IReadOnlyBasicProperties, IAmqpHeader
    {
        await BasicPublishAsync(exchange.Value, routingKey.Value, mandatory, basicProperties, body, cancellationToken);
    }

    public Task BasicQosAsync(uint prefetchSize, ushort prefetchCount, bool global, CancellationToken cancellationToken = default)
    {
        // no implenentation needed for this test
        return Task.CompletedTask;
    }

    public async ValueTask BasicRejectAsync(ulong deliveryTag, bool requeue, CancellationToken cancellationToken = default)
    {
        await Server.RejectMessageAsync(deliveryTag, false, requeue);
    }

    public async Task CloseAsync(ushort replyCode, string replyText, bool abort, CancellationToken cancellationToken = default)
    {
        CloseReason = new ShutdownEventArgs(ShutdownInitiator.Application, replyCode, replyText, cancellationToken: cancellationToken);
        await _channelShutdownAsyncWrapper.InvokeAsync(this, CloseReason);
        IsClosed = true;
        IsOpen = false;
    }

    public async Task CloseAsync(ShutdownEventArgs reason, bool abort, CancellationToken cancellationToken = default)
    {
        CloseReason = reason;
        await _channelShutdownAsyncWrapper.InvokeAsync(this, reason);
        IsClosed = true;
        IsOpen = false;
    }

    public async Task<uint> ConsumerCountAsync(string queue, CancellationToken cancellationToken = default)
    {
        var queueInstance = await Server.GetQueueAsync(queue);
        if(queueInstance is null)
        {
            throw new ArgumentException($"Queue {queue} does not exist.");
        }
        return (uint)await queueInstance.ConsumerCountAsync();
    }

    public async Task ExchangeBindAsync(string destination, string source, string routingKey, IDictionary<string, object?>? arguments = null, bool noWait = false, CancellationToken cancellationToken = default)
    {
        var sourceInstance = await Server.GetExchangeAsync(source);
        if (sourceInstance is null)
        {
            throw new ArgumentException($"Exchange {source} does not exist.");
        }
        await sourceInstance.BindExchangeAsync(destination, routingKey, arguments);
    }

    public async Task ExchangeDeclareAsync(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object?>? arguments = null, bool passive = false, bool noWait = false, CancellationToken cancellationToken = default)
    {
        await Server.ExchangeDeclareAsync(ConnectionNumber, exchange, type, durable, autoDelete, arguments, passive);
    }

    public async Task ExchangeDeclarePassiveAsync(string exchange, CancellationToken cancellationToken = default)
    {
        var exchangeInstance = await Server.GetExchangeAsync(exchange);
        if (exchangeInstance is null)
        {
            throw new ArgumentException($"Exchange {exchange} does not exist.");
        }
    }

    public async Task ExchangeDeleteAsync(string exchange, bool ifUnused = false, bool noWait = false, CancellationToken cancellationToken = default)
    {
        await Server.ExchangeDeleteAsync(exchange, ifUnused);
    }

    public async Task ExchangeUnbindAsync(string destination, string source, string routingKey, IDictionary<string, object?>? arguments = null, bool noWait = false, CancellationToken cancellationToken = default)
    {
        var sourceInstance = await Server.GetExchangeAsync(source);
        if(sourceInstance is null)
        {
            throw new ArgumentException($"Exchange {source} does not exist.");
        }
        await sourceInstance.UnbindExchangeAsync(destination, routingKey);
    }

    public ValueTask<ulong> GetNextPublishSequenceNumberAsync(CancellationToken cancellationToken = default)
    {
        return Server.GetNextPublishSequenceNumber();
    }

    public async Task<uint> MessageCountAsync(string queue, CancellationToken cancellationToken = default)
    {
        var queueInstance = await Server.GetQueueAsync(queue);
        if (queueInstance is null)
        {
            throw new ArgumentException($"Queue {queue} does not exist.");
        }
        return await queueInstance.CountAsync();
    }

    public async Task QueueBindAsync(string queue, string exchange, string routingKey, IDictionary<string, object?>? arguments = null, bool noWait = false, CancellationToken cancellationToken = default)
    {
        var exchangeInstance = Server.GetExchangeAsync(exchange).Result;
        if (exchangeInstance is null)
        {
            throw new ArgumentException($"Exchange {exchange} does not exist.");
        }
        var queueInstance = Server.GetQueueAsync(queue).Result;
        if (queueInstance is null)
        {
            throw new ArgumentException($"Queue {queue} does not exist.");
        }
        await exchangeInstance.BindQueueAsync(routingKey, queueInstance, arguments);
    }

    public async Task<QueueDeclareOk> QueueDeclareAsync(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object?>? arguments = null, bool passive = false, bool noWait = false, CancellationToken cancellationToken = default)
    {
        return await Server.QueueDeclareAsync(ConnectionNumber, queue, durable, exclusive, autoDelete, arguments, passive);
    }

    public async Task<QueueDeclareOk> QueueDeclarePassiveAsync(string queue, CancellationToken cancellationToken = default)
    {
        return await Server.QueueDeclarePassiveAsync(queue);
    }

    public async Task<uint> QueueDeleteAsync(string queue, bool ifUnused, bool ifEmpty, bool noWait = false, CancellationToken cancellationToken = default)
    {
        return await Server.QueueDeleteAsync(queue, ifUnused, ifEmpty);
    }

    public async Task<uint> QueuePurgeAsync(string queue, CancellationToken cancellationToken = default)
    {
        var queueInstance = await Server.GetQueueAsync(queue);
        if(queueInstance is null)
        {
            throw new ArgumentException($"Queue {queue} does not exist.");
        }
        return await queueInstance.PurgeMessagesAsync();
    }

    public async Task QueueUnbindAsync(string queue, string exchange, string routingKey, IDictionary<string, object?>? arguments = null, CancellationToken cancellationToken = default)
    {
        var queueInstance = await Server.GetQueueAsync(queue);
        if (queueInstance is null)
        {
            throw new ArgumentException($"Queue {queue} does not exist.");
        }
        var exchangeInstancee = await Server.GetExchangeAsync(exchange);
        if (exchangeInstancee is null)
        {
            throw new ArgumentException($"Exchange {exchange} does not exist.");
        }

        await exchangeInstancee.UnbindQueueAsync(routingKey, queueInstance);
    }

    public Task TxCommitAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task TxRollbackAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task TxSelectAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private async ValueTask<ulong> GetNextDeliveryTagAsync()
    {
        return await Server.GetNextDeliveryTagAsync(channelNumber: ChannelNumber);
    }

    public void Dispose()
    {
        _disposed = true;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
