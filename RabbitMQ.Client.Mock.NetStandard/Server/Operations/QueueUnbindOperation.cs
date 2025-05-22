using RabbitMQ.Client.Mock.NetStandard.Server.Exchanges;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Operations
{
    internal class QueueUnbindOperation : Operation
    {
        readonly string _exchange;
        readonly string _queue;
        readonly string _bindingKey;
        readonly IDictionary<string, object> _arguments;

        public QueueUnbindOperation(IRabbitServer server, string exchange, string queue, string bindingKey, IDictionary<string, object> arguments = null)
            : base(server)
        {
            this._exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
            this._queue = queue ?? throw new ArgumentNullException(nameof(queue));
            this._bindingKey = bindingKey ?? throw new ArgumentNullException(nameof(bindingKey));
            this._arguments = arguments;
        }

        public override bool IsValid => !(Server is null || _exchange is null || string.IsNullOrWhiteSpace(_queue) || string.IsNullOrWhiteSpace(_bindingKey));

        public override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!IsValid)
                {
                    return new ValueTask<OperationResult>(OperationResult.Warning("Exchange, Queue and BindingKey are required."));
                }

                // get the exchange to unbind from
                var exchangeToUnbindFrom = Server.Exchanges.TryGetValue(_exchange, out var x) ? x : null;
                if (exchangeToUnbindFrom is null)
                {
                    return new ValueTask<OperationResult>(OperationResult.Warning($"Exchange '{_exchange}' not found."));
                }

                // get the queue to unbind.
                var queueToUnbind = Server.Queues.TryGetValue(_queue, out var q) ? q : null;
                if (queueToUnbind is null)
                {
                    return new ValueTask<OperationResult>(OperationResult.Warning($"Queue '{_queue}' not found."));
                }

                // check if we have a binding
                var binding = Server.QueueBindings.TryGetValue(_bindingKey, out var bnd) ? bnd : null;
                if (binding is null)
                {
                    return new ValueTask<OperationResult>(OperationResult.Warning($"Binding '{_bindingKey}' not found."));
                }

                // remove the target queue from the binding
                if (!binding.BoundQueues.Remove(_queue))
                {
                    return new ValueTask<OperationResult>(OperationResult.Success($"Queue '{queueToUnbind.Name}' has already been unbound from exchange '{exchangeToUnbindFrom.Name}' with key '{_bindingKey}'."));
                }

                // check if the binding is empty and if so, remove it
                if (binding.BoundQueues.Count == 0)
                {
                    Server.QueueBindings.Remove(_bindingKey);
                    return new ValueTask<OperationResult>(OperationResult.Success($"Binding '{_bindingKey}' removed."));
                }

                // the queue binding was removed. return success.
                return new ValueTask<OperationResult>(OperationResult.Success($"Queue '{queueToUnbind.Name}' unbound from exchange '{exchangeToUnbindFrom.Name}' with key '{_bindingKey}'."));
            }
            catch (Exception ex)
            {
                return new ValueTask<OperationResult>(OperationResult.Failure(ex));
            }
        }
    }
}