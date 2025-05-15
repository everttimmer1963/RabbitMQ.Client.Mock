using RabbitMQ.Client.Events;
using RabbitMQ.Client.Mock.Server.Bindings;
using RabbitMQ.Client.Mock.Server.Exchanges;
using RabbitMQ.Client.Mock.Server.Operations;
using RabbitMQ.Client.Mock.Server.Queues;
using System.Collections.Concurrent;

namespace RabbitMQ.Client.Mock.Server;

internal class RabbitServer : IRabbitServer
{
    public RabbitServer()
    {
        this.Processor = new OperationsProcessor();
        this.Processor.StartProcessing();
    }

    #region Properties
    public IDictionary<string, RabbitExchange> Exchanges { get; } = new ConcurrentDictionary<string, RabbitExchange>();
    public IDictionary<string, RabbitQueue> Queues { get; } = new ConcurrentDictionary<string, RabbitQueue>();
    public IDictionary<string, ExchangeBinding> ExchangeBindings { get; } = new ConcurrentDictionary<string, ExchangeBinding>();
    public IDictionary<string, QueueBinding> QueueBindings { get; } = new ConcurrentDictionary<string, QueueBinding>();

    public OperationsProcessor Processor { get; private set; }
    #endregion

    #region Channel Interface Implementation
    public ValueTask BasicAckAsync(ulong deliveryTag, bool multiple, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task BasicCancelAsync(string consumerTag, bool noWait = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<string> BasicConsumeAsync(string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object?>? arguments, IAsyncBasicConsumer consumer, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<BasicGetResult?> BasicGetAsync(string queue, bool autoAck, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask BasicNackAsync(ulong deliveryTag, bool multiple, bool requeue, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask BasicPublishAsync<TProperties>(string exchange, string routingKey, bool mandatory, TProperties basicProperties, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default) where TProperties : IReadOnlyBasicProperties, IAmqpHeader
    {
        throw new NotImplementedException();
    }

    public ValueTask BasicPublishAsync<TProperties>(CachedString exchange, CachedString routingKey, bool mandatory, TProperties basicProperties, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default) where TProperties : IReadOnlyBasicProperties, IAmqpHeader
    {
        throw new NotImplementedException();
    }

    public Task BasicQosAsync(uint prefetchSize, ushort prefetchCount, bool global, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
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

    public async Task ExchangeBindAsync(string destination, string source, string routingKey, IDictionary<string, object?>? arguments = null, bool noWait = false, CancellationToken cancellationToken = default)
    {
        // get the source exchange
        var exchange = Exchanges.TryGetValue(source, out var x) ? x : null;
        if (exchange is null)
        {
            throw new ArgumentException($"Exchange '{source}' not found.");
        }

        // now bind the destination exchange to the source exchange
        await exchange.ExchangeBindAsync(destination, routingKey, arguments, noWait, cancellationToken);
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

    public async Task ExchangeUnbindAsync(string destination, string source, string routingKey, IDictionary<string, object?>? arguments = null, bool noWait = false, CancellationToken cancellationToken = default)
    {
        // get the source exchange
        var exchange = Exchanges.TryGetValue(source, out var x) ? x : null;
        if (exchange is null)
        {
            throw new ArgumentException($"Exchange '{source}' not found.");
        }

        // now unbind the destination exchange from the source exchange
        await exchange.ExchangeUnbindAsync(destination, routingKey, noWait, cancellationToken);
    }

    public ValueTask<ulong> GetNextPublishSequenceNumberAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<uint> MessageCountAsync(string queue, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task QueueBindAsync(string queue, string exchange, string routingKey, IDictionary<string, object?>? arguments = null, bool noWait = false, CancellationToken cancellationToken = default)
    {
        // get the source exchange
        var exchangeInstance = Exchanges.TryGetValue(exchange, out var x) ? x : null;
        if (exchangeInstance is null)
        {
            throw new ArgumentException($"Exchange '{exchange}' not found.");
        }

        // now bind the queue to the exchange
        await exchangeInstance.QueueBindAsync(queue, routingKey, arguments, noWait, cancellationToken);
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

    public async Task<uint> QueuePurgeAsync(string queue, CancellationToken cancellationToken = default)
    {
        // get the queue instance
        var queueInstance = Queues.TryGetValue(queue, out var q) ? q : null;
        if (queueInstance is null)
        {
            throw new ArgumentException($"Queue '{queue}' not found.");
        }

        // now purge the queue
        return await queueInstance.PurgeAsync();
    }

    public async Task QueueUnbindAsync(string queue, string exchange, string routingKey, IDictionary<string, object?>? arguments = null, CancellationToken cancellationToken = default)
    {
        // get the source exchange
        var exchangeInstance = Exchanges.TryGetValue(exchange, out var x) ? x : null;
        if (exchangeInstance is null)
        {
            throw new ArgumentException($"Exchange '{exchange}' not found.");
        }
        // now unbind the queue from the exchange
        await exchangeInstance.QueueUnbindAsync(queue, routingKey, false, cancellationToken);
    }
    #endregion
}
