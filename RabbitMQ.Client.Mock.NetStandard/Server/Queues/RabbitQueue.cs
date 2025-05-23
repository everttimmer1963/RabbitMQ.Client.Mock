﻿using RabbitMQ.Client.Mock.NetStandard.Server.Bindings;
using RabbitMQ.Client.Mock.NetStandard.Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Queues
{
    internal class RabbitQueue : IDisposable, IAsyncDisposable
    {
        private bool _disposed;
        private readonly IRabbitServer _server;
        private readonly string _name;
        private readonly ConcurrentLinkedList<RabbitMessage> _queue = new ConcurrentLinkedList<RabbitMessage>();
        private readonly AsyncAutoResetEvent _waitHandle = new AsyncAutoResetEvent(false);
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public RabbitQueue(IRabbitServer server, string name)
        {
            this._server = server ?? throw new ArgumentNullException(nameof(server));
            this._name = name ?? throw new ArgumentNullException(nameof(name));
            Task.Run(() => DeliveryLoop(_tokenSource.Token));
        }

        private IRabbitServer Server => _server;

        public string Name => _name;

        public bool IsDurable { get; set; }

        public bool IsExclusive { get; set; }

        public bool AutoDelete { get; set; }

        public IDictionary<string, object> Arguments { get; internal set; }

        public uint MessageCount => Convert.ToUInt32(_queue.Count);

        public uint ConsumerCount => Convert.ToUInt32(Server.ConsumerBindings.Where(cb => cb.Value.Queue.Name.Equals(Name)).Count());

        private IDictionary<string, ConsumerBinding> Consumers => Server.ConsumerBindings
            .Where(cb => cb.Value.Queue.Name.Equals(Name))
            .ToDictionary(cb => cb.Key, cb => cb.Value);

        internal bool TryGetDeadLetterQueueInfoAsync(out string exchange, out string routingKey)
        {
            if (Arguments is null)
            {
                exchange = null;
                routingKey = null;
                return false;
            }

            exchange = Arguments.TryGetValue("x-dead-letter-exchange", out var ex) ? (string)ex : null;
            routingKey = Arguments.TryGetValue("x-dead-letter-routing-key", out var rk) ? (string)rk : null;

            return (exchange != null && routingKey != null);
        }

        public bool TryConsumeMessage(int channelNumber, bool autoAcknowledge, out RabbitMessage message)
        {
            message = _queue.TryRemoveFirst(out var msg) ? msg : null;
            if (message is null)
            {
                return false;
            }
            if (autoAcknowledge)
            {
                CopyToPendingConfirms(channelNumber, message);
            }
            return true;
        }

        public async ValueTask<RabbitMessage> ConsumeMessageAsync(int channelNumber, bool autoAcknowledge)
        {
            var message = _queue.TryRemoveFirst(out var msg) ? msg : null;
            if (message is null)
            {
                return null;
            }
            if (autoAcknowledge)
            {
                await CopyToPendingConfirmsAsync(channelNumber, message).ConfigureAwait(false);
            }
            return message;
        }

        public void CopyToPendingConfirms(int channelNumber, RabbitMessage message)
        {
            var pc = new PendingConfirm(channelNumber, message.DeliveryTag, message, TimeSpan.FromMinutes(30));
            Server.PendingConfirms.Add((channelNumber, message.DeliveryTag), pc);
        }

        public ValueTask CopyToPendingConfirmsAsync(int channelNumber, RabbitMessage message)
        {
            var pc = new PendingConfirm(channelNumber, message.DeliveryTag, message, TimeSpan.FromMinutes(30));
            Server.PendingConfirms.Add((channelNumber, message.DeliveryTag), pc);
            return new ValueTask(Task.CompletedTask);
        }

        public ValueTask PublishMessageAsync(RabbitMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            message.Queue = Name.ToString();
            _queue.AddLast(message);
            _waitHandle.Set();
            return new ValueTask(Task.CompletedTask);
        }

        public ValueTask RequeueMessageAsync(RabbitMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            message.Queue = Name.ToString();
            _queue.AddFirst(message);
            return new ValueTask(Task.CompletedTask);
        }

        public ValueTask<uint> PurgeAsync()
        {
            var count = (uint)_queue.Count;
            _queue.Clear();
            return new ValueTask<uint>(count);
        }

        private async Task DeliveryLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await _waitHandle.WaitOneAsync(cancellationToken).ConfigureAwait(false);
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    // if there are no consumers, wait for one to be added.
                    if (ConsumerCount == 0)
                    {
                        continue;
                    }

                    while (_queue.TryRemoveFirst(out var message) && message != null)
                    {
                        var rnd = new Random();

                        // okay, get the bound consumers. we are goind to select one of the consumers randomly,
                        // and deliver the message to that consumer.
                        var consumers = Consumers.ToArray();
                        var chosenIndex = consumers.Length == 0
                            ? 0
                            : rnd.Next(0, consumers.Length);

                        var binding = consumers[chosenIndex];

                        // add to pending confirms when explicit acknowledgements.
                        if (!binding.Value.AutoAcknowledge)
                        {
                            await CopyToPendingConfirmsAsync(binding.Value.ChannelNumber, message).ConfigureAwait(false);
                        }

                        // now, deliver the message to the consumer.
                        await binding.Value.Consumer.HandleBasicDeliverAsync(
                            binding.Key,
                            message.DeliveryTag,
                            message.Redelivered,
                            message.Exchange,
                            message.RoutingKey,
                            message.BasicProperties,
                            message.Body,
                            cancellationToken);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error while delivering message: {e.Message}");
                }
            }
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return new ValueTask(Task.CompletedTask);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _waitHandle.Dispose();
        }
    }
}
