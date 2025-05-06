using RabbitMQ.Client.Events;
using RabbitMQ.Client.Mock.Domain;

namespace RabbitMQ.Client.Mock;
internal class FakeChannel : IChannel
{
    private readonly RabbitMQServer _server;
    private readonly CreateChannelOptions? _options;

    private static int _lastChannelNumber = 0;
    private static int GetNextChannelNumber()
    { 
        return Interlocked.Increment(ref _lastChannelNumber);
    }

    public FakeChannel(CreateChannelOptions? options)
    {
        _server = RabbitMQServer.GetInstance();
        _options = options;
        ChannelNumber = GetNextChannelNumber();
    }

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
            while (await _server.ConfirmMessageAsync(deliveryTag))
            {
                deliveryTag--;
            }
            return;
        }
        await _server.ConfirmMessageAsync(deliveryTag);
    }

    public async Task BasicCancelAsync(string consumerTag, bool noWait = false, CancellationToken cancellationToken = default)
    {
        var consumer = await _server.UnregisterConsumerAsync(consumerTag);
        if (consumer is null)
        {
            throw new ArgumentException($"Consumer {consumerTag} not found.");
        }
        await consumer.HandleBasicCancelOkAsync(consumerTag);
    }

    public async Task<string> BasicConsumeAsync(string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object?>? arguments, IAsyncBasicConsumer consumer, CancellationToken cancellationToken = default)
    {
        await _server.RegisterConsumerAsync(consumerTag, queue, autoAck, arguments, consumer);
        return consumerTag;
    }

    public async Task<BasicGetResult?> BasicGetAsync(string queue, bool autoAck, CancellationToken cancellationToken = default)
    {
        var queueInstance = await _server.GetQueueAsync(queue);
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
        await _server.RejectMessageAsync(deliveryTag, multiple, requeue);
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
            DeliveryTag = 0,
        };
        var exchangeInstance = await _server.GetExchangeAsync(exchange);
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

    public ValueTask BasicRejectAsync(ulong deliveryTag, bool requeue, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task CloseAsync(ushort replyCode, string replyText, bool abort, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task CloseAsync(ShutdownEventArgs reason, bool abort, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<uint> ConsumerCountAsync(string queue, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }

    public Task ExchangeBindAsync(string destination, string source, string routingKey, IDictionary<string, object?>? arguments = null, bool noWait = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task ExchangeDeclareAsync(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object?>? arguments = null, bool passive = false, bool noWait = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task ExchangeDeclarePassiveAsync(string exchange, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task ExchangeDeleteAsync(string exchange, bool ifUnused = false, bool noWait = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task ExchangeUnbindAsync(string destination, string source, string routingKey, IDictionary<string, object?>? arguments = null, bool noWait = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<ulong> GetNextPublishSequenceNumberAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<uint> MessageCountAsync(string queue, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task QueueBindAsync(string queue, string exchange, string routingKey, IDictionary<string, object?>? arguments = null, bool noWait = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<QueueDeclareOk> QueueDeclareAsync(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object?>? arguments = null, bool passive = false, bool noWait = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<QueueDeclareOk> QueueDeclarePassiveAsync(string queue, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<uint> QueueDeleteAsync(string queue, bool ifUnused, bool ifEmpty, bool noWait = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<uint> QueuePurgeAsync(string queue, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task QueueUnbindAsync(string queue, string exchange, string routingKey, IDictionary<string, object?>? arguments = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
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
}
