using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Mock.NetStandard.Domain
{
    internal class RabbitQueue : IAsyncDisposable
    {
        #region Types
        class Consumer
        {
            public string ConsumerTag { get; set; } = string.Empty;
            public bool AutoAcknowledge { get; set; }
            public IDictionary<string, object?>? Arguments { get; set; }
        }
        #endregion

        private static readonly Random _random = new Random();
        private static readonly object _randomLock = new object();

        private bool _disposed;
        private int _connectionNumber;

        private readonly ConcurrentDictionary<string, Consumer> _consumers = new ConcurrentDictionary<string, Consumer>();
        private readonly ConcurrentQueue<RabbitMessage> _queue = new ConcurrentQueue<RabbitMessage>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<ulong, List<RabbitMessage>> _pendingMessages = new ConcurrentDictionary<ulong, List<RabbitMessage>>();
        private AsyncAutoResetEvent _messageAvailable = new AsyncAutoResetEvent(true);
        private CancellationTokenSource _tokenSource;

        public RabbitQueue(string name, bool nameIsServerAssigned, int connectionNumber)
        {
            Name = name;
            _connectionNumber = connectionNumber;
            HasServerGeneratedName = nameIsServerAssigned;
            _tokenSource = new CancellationTokenSource();
            Task.Run(() => MessageDeliveryLoopAsync(_tokenSource.Token));
        }

        private RabbitMQServer Server => RabbitMQServer.GetInstance(_connectionNumber);

        #region Public Properties
        public string Name { get; set; }

        public bool IsDurable { get; set; }

        public bool IsExclusive { get; set; }

        public int Connection { get; set; }

        public bool AutoDelete { get; set; }

        public bool HasServerGeneratedName { get; set; }

        public IDictionary<string, object?> Arguments { get; } = new Dictionary<string, object?>();

        public bool HasDeadLetterQueue => Arguments.ContainsKey("x-dead-letter-exchange");
        #endregion

        #region Methods

        internal ValueTask<int> ConsumerCountAsync()
        {
            return new ValueTask<int>(_consumers.Count);
        }

        internal ValueTask<uint> CountAsync()
        {
            return new ValueTask<uint>((uint)_queue.Count);
        }

        internal ValueTask<uint> PendingMessageCountAsync()
        {
            return new ValueTask<uint>((uint)_pendingMessages.Count);
        }

        internal async ValueTask<uint> PurgeMessagesAsync()
        {
            // get the number of messages for reporting purposes, and clear the queues.
            var messageCount = await CountAsync();
            _pendingMessages.Clear();
            _queue.Clear();

            // remove all bound consumers from the queue.
            _consumers.Clear();
            return messageCount;
        }

        internal async ValueTask RemoveAndNontifyConsumersAsync()
        {
            foreach (var consumer in _consumers.Values)
            {
                var consumerInstance = await Server.GetConsumerAsync(consumer.ConsumerTag);
                if (consumerInstance != null)
                {
                    await consumerInstance.HandleBasicCancelAsync(consumer.ConsumerTag);
                }
            }
            _consumers.Clear();
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

            return await Server.AddConsumerBindingAsync(consumerTag, Name);
        }

        internal async ValueTask<bool> RemoveConsumerAsync(string consumerTag)
        {
            _consumers.TryRemove(consumerTag, out var consumer);
            if (consumer is null)
            {
                throw new ArgumentException($"Consumer {consumerTag} not found.");
            }
            return await Server.RemoveConsumerBindingAsync(consumerTag, Name);
        }

        internal ValueTask<bool> PublishMessageAsync(RabbitMessage message)
        {
            // assign the name of this queue to the message in order to be able to
            // discover a message's origin.
            message.Queue = Name;

            _queue.Enqueue(message);

            // signal message arrival to the delivery loop.
            _messageAvailable.Set();

            return new ValueTask<bool>(true);
        }

        internal async ValueTask<RabbitMessage?> ConsumeMessageAsync(bool autoAck = true)
        {
            var message = _queue.TryDequeue(out var msg) ? msg : null;
            if (!autoAck && message != null)
            {
                await AddPendingMessageAsync(message.DeliveryTag, message);
                await Server.AddPendingMessageBindingAsync(message.DeliveryTag, Name);
            }
            return message;
        }

        internal async ValueTask<bool> ConfirmMessageAsync(ulong deliveryTag)
        {
            return await Server.RemovePendingMessageBindingAsync(deliveryTag, Name);
        }

        internal async ValueTask<List<RabbitMessage>?> RejectMessageAsync(ulong deliveryTag, bool multiple, bool requeue)
        {
            var messages = _pendingMessages.TryRemove(deliveryTag, out var msg) ? msg : null;
            if (messages is null)
            {
                return null;
            }

            if (requeue)
            {
                var requeued = await RequeueMessagesAsync(messages);
            }
            else if (HasDeadLetterQueue)
            {
                messages.ForEach(async message => await Server.SendToDeadLetterQueueIfExists(this, message));
            }

            await Server.RemovePendingMessageBindingAsync(deliveryTag, Name);

            if (multiple)
            {
                return await RejectMessageAsync(deliveryTag - 1, multiple, requeue);
            }

            return messages;
        }

        private async ValueTask<bool> RequeueMessagesAsync(List<RabbitMessage> messages)
        {
            await _semaphore.WaitAsync();
            try
            {
                var queue = new ConcurrentQueue<RabbitMessage>();
                messages.ForEach(queue.Enqueue);
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

        private ValueTask<bool> AddPendingMessageAsync(ulong deliveryTag, RabbitMessage message)
        {
            var messagesForTag = _pendingMessages.GetOrAdd(deliveryTag, new List<RabbitMessage>());
            messagesForTag.Add(message);
            return new ValueTask<bool>(true);
        }

        private async ValueTask MessageDeliveryLoopAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                // wait for messages to become available or for the process to be cancelled.
                await _messageAvailable.WaitOneAsync(cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                // no need to dequeue messages if there are no consumers to process them so just
                // return to the top of the loop when there are none.
                if (_consumers.Count == 0)
                {
                    continue;
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

                    // now randomly choose a consumer tag to deliver the message to
                    var consumerTag = await GetConsumerTagAsync(consumerTags);

                    // get settings and instance for this consumer tag
                    var consumerSettings = _consumers[consumerTag];
                    var consumerInstance = await Server.GetConsumerAsync(consumerTag);

                    // if the consumer has to confirm or reject the message, add it to the pending messages
                    if (!consumerSettings.AutoAcknowledge)
                    {
                        await AddPendingMessageAsync(message.DeliveryTag, message);
                        await Server.AddPendingMessageBindingAsync(message.DeliveryTag, Name);
                    }

                    await consumerInstance.HandleBasicDeliverAsync(
                        consumerTag,
                        message.DeliveryTag,
                        false,
                        message.Exchange,
                        message.RoutingKey,
                        message.BasicProperties,
                        message.Body);
                }
            }
        }

        private ValueTask<string> GetConsumerTagAsync(string[] consumerTags)
        {
            int index;
            lock (_randomLock)
            {
                index = _random.Next(consumerTags.Length);
            }
            return new ValueTask<string>(consumerTags[index]);
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            // cancel the message delivery loop
            _tokenSource.Cancel();
            _tokenSource.Dispose();
            _messageAvailable.Dispose();
            _semaphore.Dispose();

            // remove all consumers from the queue.
            await RemoveAndNontifyConsumersAsync();

            // remove all messages from the queue.
            await PurgeMessagesAsync();
        }
        #endregion
    }
}
