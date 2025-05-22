using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Mock.NetStandard.Server.Bindings;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Operations
{
    internal class QueueBindOperation : Operation
    {
        readonly string _exchange;
        readonly string _queue;
        readonly string _bindingKey;
        readonly IDictionary<string, object> arguments;

        public QueueBindOperation(IRabbitServer server, string exchange, string queue, string bindingKey, IDictionary<string, object> arguments = null)
            : base(server)
        {
            this._exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
            this._queue = queue ?? throw new ArgumentNullException(nameof(queue));
            this._bindingKey = bindingKey ?? throw new ArgumentNullException(nameof(bindingKey));
            this.arguments = arguments;
        }

        public override bool IsValid => !(Server is null || string.IsNullOrWhiteSpace(_queue));

        public override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!IsValid)
                {
                    return new ValueTask<OperationResult>(OperationResult.Failure(new InvalidOperationException("Exchange, Queue and BindingKey are required.")));
                }

                // We cannot bind a queue to the default exchange so check it.
                if (_exchange == string.Empty)
                {
                    return new ValueTask<OperationResult>(OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"The default exchange cannot be used as the exchange in a queue binding."))));
                }

                // get the exchange to bind to.
                var exchangeToBind = Server.Exchanges.TryGetValue(_exchange, out var x) ? x : null;
                if (exchangeToBind is null)
                {
                    return new ValueTask<OperationResult>(OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Exchange '{_exchange}' not found."))));
                }

                // get the queue to bind.
                var queueToBind = Server.Queues.TryGetValue(_queue, out var q) ? q : null;
                if (queueToBind is null)
                {
                    return new ValueTask<OperationResult>(OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Queue '{_queue}' not found."))));
                }

                // check if we already have binding. if we don't, create a new one.
                var binding = Server.QueueBindings.TryGetValue(_bindingKey, out var bnd) ? bnd : null;
                if (binding is null)
                {
                    binding = new QueueBinding { Exchange = exchangeToBind, Arguments = arguments };
                    binding.BoundQueues.Add(_queue, queueToBind);
                    Server.QueueBindings.Add(_bindingKey, binding);

                    return new ValueTask<OperationResult>(OperationResult.Success($"Queue '{_queue}' bound to exchange '{_exchange}' with key '{_bindingKey}'."));
                }

                // the binding exists. check if the target queue is already bound. If so, return success.
                if (binding.BoundQueues.TryGetValue(_queue, out _))
                {
                    return new ValueTask<OperationResult>(OperationResult.Success($"Queue '{_queue}' already bound to exchange '{_exchange}' with key '{_bindingKey}'."));
                }
                binding.BoundQueues.Add(_queue, queueToBind);

                // the binding was added. return success.
                return new ValueTask<OperationResult>(OperationResult.Success($"Queue '{_queue}' bound to exchange '{_exchange}' with key '{_bindingKey}'."));
            }
            catch (Exception ex)
            {
                return new ValueTask<OperationResult>(OperationResult.Failure(ex));
            }
        }
    }
}