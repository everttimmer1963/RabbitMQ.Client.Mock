using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Mock.Server.Bindings;
using RabbitMQ.Client.Mock.Server.Data;
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
    public IDictionary<string, ConsumerBinding> ConsumerBindings { get; } = new ConcurrentDictionary<string, ConsumerBinding>();
    public IDictionary<(int,ulong),PendingConfirm> PendingConfirms { get; } = new ConcurrentDictionary<(int, ulong),PendingConfirm>();
    public IDictionary<int, IChannel> Channels { get; } = new ConcurrentDictionary<int, IChannel>();

    public OperationsProcessor Processor { get; private set; }
    #endregion

    #region Server Side Names Generation
    public ValueTask<string> GenerateUniqueConsumerTag(string queueName)
    {
        return ValueTask.FromResult($"consumer-{queueName.ToLowerInvariant()}-{Guid.NewGuid().ToString("D")}");
    }
    #endregion

    #region Channel Registration
    public void RegisterChannel(int channelNumber, IChannel channel)
    {
        if (channel is null)
        {
            throw new ArgumentNullException(nameof(channel));
        }
        if (Channels.ContainsKey(channelNumber))
        {
            throw new InvalidOperationException($"Channel '{channelNumber}' already registered.");
        }
        Channels[channelNumber] = channel;
    }

    public void UnregisterChannel(int channelNumber)
    {
        if (Channels.ContainsKey(channelNumber))
        {
            Channels.Remove(channelNumber);
        }
    }
    #endregion

    #region Channel Forwarded Implementation
    public async ValueTask BasicAckAsync(int channelNumber, ulong deliveryTag, bool multiple, CancellationToken cancellationToken = default)
    {
        var operation = new BasicAckAsyncOperation(this, channelNumber, deliveryTag, multiple);
        var outcome = await Processor.EnqueueOperationAsync(operation).ConfigureAwait(false);
        await HandleOperationResult(outcome, operation.OperationId).ConfigureAwait(false);
    }

    public async Task BasicCancelAsync(string consumerTag, bool noWait = false, CancellationToken cancellationToken = default)
    {
        var operation = new BasicCancelAsyncOperation(this, consumerTag);
        var outcome = await Processor.EnqueueOperationAsync(operation, noWait, true, cancellationToken: cancellationToken);
        if(noWait)
        {
            // if noWait is true, we don't need to wait for the operation to complete.
            Console.WriteLine($"Operation: {operation.OperationId.ToString()} is queued for processing.");
            return;
        }
        await HandleOperationResult(outcome, operation.OperationId).ConfigureAwait(false);
    }

    public async Task<string> BasicConsumeAsync(string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object?>? arguments, IAsyncBasicConsumer consumer, CancellationToken cancellationToken = default)
    {
        var operation = new BasicConsumeAsyncOperation(this, queue, autoAck, consumerTag, noLocal, exclusive, arguments, consumer);
        var outcome = await Processor.EnqueueOperationAsync(operation).ConfigureAwait(false);
        return await HandleOperationResult<string>(outcome, operation.OperationId).ConfigureAwait(false);
    }

    public async Task<BasicGetResult?> BasicGetAsync(int channelNumber, string queue, bool autoAck, CancellationToken cancellationToken = default)
    {
        var operation = new BasicGetAsyncOperation(this, channelNumber, queue, autoAck);
        var outcome = await Processor.EnqueueOperationAsync(operation);
        return await HandleOperationResult<BasicGetResult>(outcome, operation.OperationId).ConfigureAwait(false);
    }

    public ValueTask BasicNackAsync(int channelNumber, ulong deliveryTag, bool multiple, bool requeue, CancellationToken cancellationToken = default)
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
        var operation = new ExchangeBindOperation(this, source, destination, routingKey, arguments);
        var outcome = await Processor.EnqueueOperationAsync(operation, noWait, true, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (noWait)
        {
            Console.WriteLine($"Operation: {operation.OperationId.ToString()} is queued for processing.");
            return;
        }

        await HandleOperationResult(outcome, operation.OperationId).ConfigureAwait(false);
    }

    public async Task ExchangeDeclareAsync(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object?>? arguments = null, bool passive = false, bool noWait = false, CancellationToken cancellationToken = default)
    {
        var operation = new ExchangeDeclareOperation(this, exchange, type, durable, autoDelete, arguments, passive);
        var outcome = await Processor.EnqueueOperationAsync(operation, noWait, true, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (noWait)
        {
            Console.WriteLine($"Operation: {operation.OperationId.ToString()} is queued for processing.");
            return;
        }
        await HandleOperationResult(outcome, operation.OperationId).ConfigureAwait(false);
    }

    public async Task ExchangeDeclarePassiveAsync(string exchange, CancellationToken cancellationToken = default)
    {
        var operation = new ExchangeDeclarePassiveOperation(this, exchange);
        var outcome = await Processor.EnqueueOperationAsync(operation).ConfigureAwait(false);
        await HandleOperationResult(outcome, operation.OperationId).ConfigureAwait(false);
    }

    public Task ExchangeDeleteAsync(string exchange, bool ifUnused = false, bool noWait = false, CancellationToken cancellationToken = default)
    {
        var operation = new ExchangeDeleteOperation(this, exchange, ifUnused);
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

    public async Task<QueueDeclareOk> QueueDeclareAsync(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object?>? arguments = null, bool passive = false, bool noWait = false, CancellationToken cancellationToken = default)
    {
        var operation = new QueueDeclareOperation(this, queue, durable, exclusive, autoDelete, arguments, passive);
        var outcome = await Processor.EnqueueOperationAsync(operation, noWait, true, cancellationToken: cancellationToken);
        if (noWait)
        {
            // if noWait is true, we don't need to wait for the operation to complete.
            Console.WriteLine($"Operation: {operation.OperationId.ToString()} is queued for processing.");
            return new QueueDeclareOk(queue, 0, 0);
        }

        // the call was synchronous so we should have an oeration result.
        if (outcome is null)
        {
            throw new InvalidOperationException($"Operation: {operation.OperationId.ToString()} - The operation did not return a result.");
        }

        // if the outcome reports a failure, and an exception is included, we should throw it.
        if (outcome is { IsFailure: true })
        {
            // if the outcome has an exception, we should throw it.
            if (outcome.Exception is not null)
            {
                throw outcome.Exception;
            }
        }

        // this method should return a QueueDeclareOk result, so if the result is null, we should throw an exception.
        var result = outcome.GetResult<QueueDeclareOk>();
        if(result is null)
        {
            throw new InvalidOperationException($"Operation: {operation.OperationId.ToString()} - The operation did not return a result.");
        }

        return result;
    }

    public Task<QueueDeclareOk> QueueDeclarePassiveAsync(string queue, CancellationToken cancellationToken = default)
    {
        // we only need to check if the queue exists
        var queueInstance = Queues.TryGetValue(queue, out var q) ? q : null;
        if (queueInstance is null)
        {
            throw new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Queue '{queue}' not found."));
        }

        return Task.FromResult(new QueueDeclareOk(queue, queueInstance.MessageCount, queueInstance.ConsumerCount));
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

    private ValueTask HandleOperationResult(OperationResult? outcome, Guid operationId)
    {
        if (outcome is null)
        {
            return ValueTask.CompletedTask;
        }

        // if the outcome reports a failure, and an exception is included, we should throw it.
        if (outcome is { Exception: not null })
        {
            throw outcome.Exception;
        }

        // if the outcome is a success, we should log it.
        Console.WriteLine($"Operation: {operationId.ToString()} - {outcome.Message}");
        return ValueTask.CompletedTask;
    }

    private ValueTask<TResult> HandleOperationResult<TResult>(OperationResult? outcome, Guid operationId) where TResult : class
    {
        // an operation result is expected to be returned.
        if (outcome is null)
        { 
            throw new InvalidOperationException($"Operation '{operationId}': - The operation did not return a result.");
        }

        // if the outcome reports a failure, and an exception is included, we should throw it.
        if (outcome is { Exception: not null })
        {
            throw outcome.Exception;
        }

        // this method should return a TResult result, so if the result is null, we should throw an exception.
        var result = outcome.GetResult<TResult>();
        if (result is null)
        {
            throw new InvalidOperationException($"Operation '{operationId}': - The operation did not return a result.");
        }

        // return the return value.
        return ValueTask.FromResult(result);
    }
}
