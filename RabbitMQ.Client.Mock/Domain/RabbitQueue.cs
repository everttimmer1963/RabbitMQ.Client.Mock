    
using System.Collections.Concurrent;

namespace RabbitMQ.Client.Mock.Domain;
internal class RabbitQueue
{
    #region Types
    class Consumer
    {
        public required string ConsumerTag { get; set; }
        public required bool AutoAcknowledge { get; set; }
        public IDictionary<string, object?>? Arguments { get; set; }
    }
    #endregion

    private readonly ConcurrentDictionary<string, Consumer> _consumers = new();
    private readonly ConcurrentQueue<RabbitMessage> _queue = new();
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly RabbitMQServer _server = RabbitMQServer.GetInstance();
    private readonly ConcurrentDictionary<ulong, RabbitMessage> _pendingMessages = new();
    private AutoResetEvent _messageAvailable = new AutoResetEvent(true);
    private CancellationTokenSource _tokenSource;

    public RabbitQueue(string name)
    {
        Name = name;
        _tokenSource = new CancellationTokenSource();
        Task.Run(() => MessageDeliveryLoopAsync(_tokenSource.Token));
    }

    #region Public Properties
    public string Name { get; set; }

    public bool IsDurable { get; set; }

    public bool IsExclusive { get; set; }

    public bool AutoDelete { get; set; }

    public IDictionary<string, object?> Arguments { get; } = new Dictionary<string, object?>();

    public bool HasDeadLetterQueue => Arguments.ContainsKey("x-dead-letter-exchange");
    #endregion

    #region Methods

    internal ValueTask<uint> CountAsync()
    {
        return ValueTask.FromResult((uint)_queue.Count);
    }

    internal async ValueTask<bool> AddConsumerAsync(string consumerTag, bool autoAck, IDictionary<string, object?>? arguments)
    {
        var registration = new Consumer
        {
            ConsumerTag = consumerTag,
            AutoAcknowledge = autoAck,
            Arguments = arguments
        };
        _consumers.AddOrUpdate(consumerTag, registration, (key, oldValue) => registration);

        // make sure any existing messages are delivered to this consumer.
        // normally, this would only occur if the consumer is the first to register,
        // with messages already in the queue.
        _messageAvailable.Set();

        return await _server.AddConsumerBindingAsync(consumerTag, Name);
    }

    internal async ValueTask<bool> RemoveConsumerAsync(string consumerTag)
    {
        _consumers.TryRemove(consumerTag, out var consumer);
        if (consumer is null)
        {
            throw new ArgumentException($"Consumer {consumerTag} not found.");
        }
        return await _server.RemoveConsumerBindingAsync(consumerTag, Name);
    }

    internal ValueTask<bool> PublishMessageAsync(RabbitMessage message)
    {
        // assign the name of this queue to the message in order to be able to
        // discover a message's origin.
        message.Queue = Name;

        _queue.Enqueue(message);

        // signal message arrival to the delivery loop.
        _messageAvailable.Set();

        return ValueTask.FromResult(true);
    }

    internal async ValueTask<RabbitMessage?> ConsumeMessageAsync(bool autoAck = true)
    {
        var message = _queue.TryDequeue(out var msg) ? msg : null;
        if (!autoAck && message is not null)
        {
            _pendingMessages.TryAdd(message.DeliveryTag, message);
            await _server.AddPendingMessageBindingAsync(message.DeliveryTag, Name);
        }
        return message;
    }

    internal async ValueTask<bool> ConfirmMessageAsync(ulong deliveryTag)
    {
        return await _server.RemovePendingMessageBindingAsync(deliveryTag, Name);
    }

    internal async ValueTask<RabbitMessage?> RejectMessageAsync(ulong deliveryTag, bool multiple, bool requeue)
    {
        var message = _pendingMessages.TryRemove(deliveryTag, out var msg) ? msg : null;
        if (message is null)
        {
            return null;
        }

        if (requeue)
        {
            var requeued = await RequeueMessageAsync(message);
        }

        if (HasDeadLetterQueue)
        { 
            await _server.SendToDeadLetterQueueIfApplicable(this, message);
        }

        await _server.RemovePendingMessageBindingAsync(deliveryTag, Name);

        if (multiple)
        {
            return await RejectMessageAsync(deliveryTag - 1, multiple, requeue);
        }

        return message;
    }

    private async ValueTask<bool> RequeueMessageAsync(RabbitMessage message)
    {
        await _semaphore.WaitAsync();
        try
        {
            var queue = new ConcurrentQueue<RabbitMessage>();
            queue.Enqueue(message);
            while (_queue.TryDequeue(out var item))
            {
                queue.Enqueue(item);
            }
            _queue.Clear();
            while (queue.TryDequeue(out var item))
            {
                _queue.Enqueue(item);
            }
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async ValueTask MessageDeliveryLoopAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            // wait for messages to be available or for the process to be cancelled.
            await _messageAvailable.WaitOneAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            // now deliver the message that are available to all listening consumers
            while (_queue.TryDequeue(out var message))
            {
                if (message is null)
                {
                    // this should never happen according to microsoft but better be safe than sorry
                    break;
                }

                // get the consumer tags that are bound to this queue
                var consumerTags = _consumers.Keys.ToArray();
                foreach (var consumerTag in consumerTags)
                {
                    // get settings and instance for this consumer tag
                    var consumerSettings = _consumers[consumerTag];
                    var consumerInstance = await _server.GetConsumerAsync(consumerTag);

                    await consumerInstance.HandleBasicDeliverAsync(
                        consumerTag,
                        message.DeliveryTag,
                        false,
                        message.Exchange,
                        message.RoutingKey,
                        message.BasicProperties,
                        message.Body);

                    if (!consumerSettings.AutoAcknowledge)
                    {
                        _pendingMessages.TryAdd(message.DeliveryTag, message);
                        await _server.AddPendingMessageBindingAsync(message.DeliveryTag, Name);
                    }
                }
            }
        }
    }
    #endregion
}
