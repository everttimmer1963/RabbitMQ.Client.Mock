using RabbitMQ.Client.Events;
using RabbitMQ.Client.Mock.Server;
using System.Diagnostics.CodeAnalysis;

namespace RabbitMQ.Client.Mock;
internal class FakeChannel : IChannel, IDisposable, IAsyncDisposable
{
    private bool _disposed;
    private readonly CreateChannelOptions? _options;
    private readonly IRabbitServer _server;

    public FakeChannel(IRabbitServer server, CreateChannelOptions? options, int connectionNumber)
    {
        _server = server;
        _options = options;
        ChannelNumber = Server.RegisterChannel(this);
        ConnectionNumber = connectionNumber;
        IsOpen = true;
    }

    private IRabbitServer Server => _server;

    public int ConnectionNumber { get; private set; }

    public int ChannelNumber { get; private set; }

    public ShutdownEventArgs? CloseReason { get; private set; }

    public IAsyncBasicConsumer? DefaultConsumer { get; set; }

    public bool IsClosed { get; private set; }

    public bool IsOpen { get; private set; }

    public string? CurrentQueue { get; private set; }

    public TimeSpan ContinuationTimeout { get; set; }

    #region Events
    private AsyncEventingWrapper<BasicAckEventArgs> _basicAcksAsyncWrapper;
    private AsyncEventingWrapper<BasicNackEventArgs> _basicNacksAsyncWrapper;
    private AsyncEventingWrapper<BasicReturnEventArgs> _basicReturnAsyncWrapper;
    private AsyncEventingWrapper<CallbackExceptionEventArgs> _callbackExceptionAsyncWrapper;
    private AsyncEventingWrapper<FlowControlEventArgs> _flowControlAsyncWrapper;
    private AsyncEventingWrapper<ShutdownEventArgs> _channelShutdownAsyncWrapper;

    public event AsyncEventHandler<BasicAckEventArgs> BasicAcksAsync = null!;
    public event AsyncEventHandler<BasicNackEventArgs> BasicNacksAsync = null!;
    public event AsyncEventHandler<ShutdownEventArgs> ChannelShutdownAsync = null!;
    public event AsyncEventHandler<BasicReturnEventArgs> BasicReturnAsync = null!;
    public event AsyncEventHandler<CallbackExceptionEventArgs> CallbackExceptionAsync = null!;
    public event AsyncEventHandler<FlowControlEventArgs> FlowControlAsync = null!;

    public async Task HandleBasicReturnAsync(BasicReturnEventArgs args)
    {
        await _basicReturnAsyncWrapper.InvokeAsync(this, args).ConfigureAwait(false);
    }

    public async Task HandleBasicAckAsync(BasicAckEventArgs args)
    {
        await _basicAcksAsyncWrapper.InvokeAsync(this, args).ConfigureAwait(false);
    }

    public async Task HandleBasicNackAsync(BasicNackEventArgs args)
    {
        await _basicNacksAsyncWrapper.InvokeAsync(this, args).ConfigureAwait(false);
    }

    public async Task HandleChannelShutdownAsync(ShutdownEventArgs args)
    {
        await _channelShutdownAsyncWrapper.InvokeAsync(this, args).ConfigureAwait(false);
    }

    public async Task HandleCallbackExceptionAsync(CallbackExceptionEventArgs args)
    {
        await _callbackExceptionAsyncWrapper.InvokeAsync(this, args).ConfigureAwait(false);
    }

    public async Task HandleFlowControlAsync(FlowControlEventArgs args)
    {
        await _flowControlAsyncWrapper.InvokeAsync(this, args).ConfigureAwait(false);
    }
    #endregion

    public async ValueTask BasicAckAsync(ulong deliveryTag, bool multiple, CancellationToken cancellationToken = default)
    {
        await Server.BasicAckAsync(this, deliveryTag, multiple, cancellationToken).ConfigureAwait(false);
    }

    public async Task BasicCancelAsync(string consumerTag, bool noWait = false, CancellationToken cancellationToken = default)
    {
        await Server.BasicCancelAsync(consumerTag, noWait, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> BasicConsumeAsync(string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object?>? arguments, IAsyncBasicConsumer consumer, CancellationToken cancellationToken = default)
    {
        return await Server.BasicConsumeAsync(this, queue, autoAck, consumerTag, noLocal, exclusive, arguments, consumer, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BasicGetResult?> BasicGetAsync(string queue, bool autoAck, CancellationToken cancellationToken = default)
    {
        return await Server.BasicGetAsync(ChannelNumber, queue, autoAck, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask BasicNackAsync(ulong deliveryTag, bool multiple, bool requeue, CancellationToken cancellationToken = default)
    {
        await Server.BasicNackAsync(this, deliveryTag, multiple, requeue, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask BasicPublishAsync<TProperties>(string exchange, string routingKey, bool mandatory, TProperties basicProperties, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default) where TProperties : IReadOnlyBasicProperties, IAmqpHeader
    {
        await Server.BasicPublishAsync(this, exchange, routingKey, mandatory, basicProperties, body, cancellationToken).ConfigureAwait(false);
    }

    [ExcludeFromCodeCoverage]
    public async ValueTask BasicPublishAsync<TProperties>(CachedString exchange, CachedString routingKey, bool mandatory, TProperties basicProperties, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default) where TProperties : IReadOnlyBasicProperties, IAmqpHeader
    {
        await BasicPublishAsync(exchange.Value, routingKey.Value, mandatory, basicProperties, body, cancellationToken).ConfigureAwait(false);
    }

    public Task BasicQosAsync(uint prefetchSize, ushort prefetchCount, bool global, CancellationToken cancellationToken = default)
    {
        // no implenentation needed for this test. we cannot throw not implemented because client may call.
        return Task.CompletedTask;
    }

    public async ValueTask BasicRejectAsync(ulong deliveryTag, bool requeue, CancellationToken cancellationToken = default)
    {
        await Server.BasicRejectAsync(this, deliveryTag, requeue, cancellationToken).ConfigureAwait(false);
    }

    public async Task CloseAsync(ushort replyCode, string replyText, bool abort, CancellationToken cancellationToken = default)
    {
        await Server.CloseAsync(replyCode, replyText, abort, cancellationToken).ConfigureAwait(false);
        CloseReason = new ShutdownEventArgs(ShutdownInitiator.Application, replyCode, replyText, cancellationToken: cancellationToken);
        await _channelShutdownAsyncWrapper.InvokeAsync(this, CloseReason);
        IsClosed = true;
        IsOpen = false;
    }

    public async Task CloseAsync(ShutdownEventArgs reason, bool abort, CancellationToken cancellationToken = default)
    {
        await Server.CloseAsync(reason, abort, cancellationToken).ConfigureAwait(false);
        CloseReason = reason;
        await _channelShutdownAsyncWrapper.InvokeAsync(this, reason);
        IsClosed = true;
        IsOpen = false;
    }

    public async Task<uint> ConsumerCountAsync(string queue, CancellationToken cancellationToken = default)
    {
        return await Server.ConsumerCountAsync(queue, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExchangeBindAsync(string destination, string source, string routingKey, IDictionary<string, object?>? arguments = null, bool noWait = false, CancellationToken cancellationToken = default)
    {
        await Server.ExchangeBindAsync(destination, source, routingKey, arguments, noWait, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExchangeDeclareAsync(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object?>? arguments = null, bool passive = false, bool noWait = false, CancellationToken cancellationToken = default)
    {
        await Server.ExchangeDeclareAsync(exchange, type, durable, autoDelete, arguments, passive).ConfigureAwait(false);
    }

    public async Task ExchangeDeclarePassiveAsync(string exchange, CancellationToken cancellationToken = default)
    {
        await Server.ExchangeDeclarePassiveAsync(exchange, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExchangeDeleteAsync(string exchange, bool ifUnused = false, bool noWait = false, CancellationToken cancellationToken = default)
    {
        await Server.ExchangeDeleteAsync(exchange, ifUnused, noWait, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExchangeUnbindAsync(string destination, string source, string routingKey, IDictionary<string, object?>? arguments = null, bool noWait = false, CancellationToken cancellationToken = default)
    {
        await Server.ExchangeUnbindAsync(destination, source, routingKey, arguments, noWait, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<ulong> GetNextPublishSequenceNumberAsync(CancellationToken cancellationToken = default)
    {
        return await Server.GetNextPublishSequenceNumberAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<uint> MessageCountAsync(string queue, CancellationToken cancellationToken = default)
    {
        return await Server.MessageCountAsync(queue, cancellationToken).ConfigureAwait(false);
    }

    public async Task QueueBindAsync(string queue, string exchange, string routingKey, IDictionary<string, object?>? arguments = null, bool noWait = false, CancellationToken cancellationToken = default)
    {
        await Server.QueueBindAsync(queue, exchange, routingKey, arguments, noWait, cancellationToken).ConfigureAwait(false);
    }

    public async Task<QueueDeclareOk> QueueDeclareAsync(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object?>? arguments = null, bool passive = false, bool noWait = false, CancellationToken cancellationToken = default)
    {
        return await Server.QueueDeclareAsync(queue, durable, exclusive, autoDelete, arguments, passive).ConfigureAwait(false);
    }

    public async Task<QueueDeclareOk> QueueDeclarePassiveAsync(string queue, CancellationToken cancellationToken = default)
    {
        return await Server.QueueDeclarePassiveAsync(queue).ConfigureAwait(false);
    }

    public async Task<uint> QueueDeleteAsync(string queue, bool ifUnused, bool ifEmpty, bool noWait = false, CancellationToken cancellationToken = default)
    {
        return await Server.QueueDeleteAsync(queue, ifUnused, ifEmpty).ConfigureAwait(false);
    }

    public async Task<uint> QueuePurgeAsync(string queue, CancellationToken cancellationToken = default)
    {
        return await Server.QueuePurgeAsync(queue, cancellationToken).ConfigureAwait(false);
    }

    public async Task QueueUnbindAsync(string queue, string exchange, string routingKey, IDictionary<string, object?>? arguments = null, CancellationToken cancellationToken = default)
    {
        await Server.QueueUnbindAsync(queue, exchange, routingKey, arguments, cancellationToken).ConfigureAwait(false);
    }

    public Task TxCommitAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task TxRollbackAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task TxSelectAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    private async ValueTask<ulong> GetNextDeliveryTagAsync()
    {
        return await Server.GetNextDeliveryTagForChannel(ChannelNumber).ConfigureAwait(false);
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
