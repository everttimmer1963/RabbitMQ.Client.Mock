using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Mock.NetStandard.Server.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Operations
{
    internal class BasicConsumeOperation : Operation
    {
        private string _consumerTag;
        private readonly IChannel _channel;
        private readonly string _queue;
        private readonly bool _autoAck;
        private readonly bool _noLocal;
        private readonly bool _exclusive;
        private readonly IDictionary<string, object> _arguments;
        private readonly IAsyncBasicConsumer _consumer;

        public BasicConsumeOperation(IRabbitServer server, IChannel channel, string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object> arguments, IAsyncBasicConsumer consumer)
            : base(server)
        { 
            _channel = channel;
            _queue = queue;
            _autoAck = autoAck;
            _consumerTag = consumerTag;
            _noLocal = noLocal;
            _exclusive = exclusive;
            _arguments = arguments ?? new Dictionary<string, object>();
            _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
        }
        public override bool IsValid => !(Server is null || string.IsNullOrEmpty(_queue) || _consumer is null);

        public override async ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!IsValid)
                {
                    return OperationResult.Failure(new InvalidOperationException("Queue and Consumer are required."));
                }

                // get the queue for which to add a consumer.
                var queueInstance = Server.Queues.Values.FirstOrDefault(q => q.Name == _queue);
                if (queueInstance == null)
                {
                    return OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Queue '{_queue}' not found.")));
                }

                // check if the consumer already exists.
                var exists = Server.ConsumerBindings.ContainsKey(_consumerTag);
                if (exists)
                {
                    return OperationResult.Warning($"A consumer with tag '{_consumerTag}' already exists.");
                }

                // if exclusive is true, we need to check if there are any other consumers on the queue.
                if (_exclusive && queueInstance.ConsumerCount > 0)
                {
                    return OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Queue '{_queue}' already has a consumer.")));
                }

                // do we need to create a server provided consumertag?
                if (string.IsNullOrWhiteSpace(_consumerTag))
                {
                    _consumerTag = await Server.GenerateUniqueConsumerTag(_queue).ConfigureAwait(false);
                }

                // now, add the new consumer binding.
                var binding = new ConsumerBinding(queueInstance, _consumer)
                {
                    ChannelNumber = _channel.ChannelNumber,
                    AutoAcknowledge = _autoAck,
                    Arguments = _arguments,
                    NoLocal = _noLocal,
                    Exclusive = _exclusive,
                };

                if(!Server.ConsumerBindings.ContainsKey(_consumerTag))
                {
                    Server.ConsumerBindings.Add(_consumerTag, binding);
                }
                return OperationResult.Success($"Consumer '{_consumerTag}' added to queue '{_queue}' with autoAck={_autoAck}, noLocal={_noLocal}, exclusive={_exclusive}.", _consumerTag);
            }
            catch (Exception ex)
            {
                return OperationResult.Failure(ex);
            }
        }
    }
}
