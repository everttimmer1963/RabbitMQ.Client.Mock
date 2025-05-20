using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Mock.Server.Bindings;

namespace RabbitMQ.Client.Mock.Server.Operations;
internal class QueueBindOperation(IRabbitServer server, string exchange, string queue, string bindingKey, IDictionary<string, object?>? arguments = null) : Operation(server)
{
    public override bool IsValid => !(Server is null || string.IsNullOrWhiteSpace(queue));

    public override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            if(!IsValid)
            {
                return ValueTask.FromResult(OperationResult.Failure(new InvalidOperationException("Exchange, Queue and BindingKey are required.")));
            }

            // We cannot bind a queue to the default exchange so check it.
            if (exchange == string.Empty)
            {
                return ValueTask.FromResult(OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"The default exchange cannot be used as the exchange in a queue binding."))));
            }

            // get the exchange to bind to.
            var exchangeToBind = Server.Exchanges.TryGetValue(exchange, out var x) ? x : null;
            if (exchangeToBind is null)
            {
                return ValueTask.FromResult(OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Exchange '{exchange}' not found."))));
            }

            // get the queue to bind.
            var queueToBind = Server.Queues.TryGetValue(queue, out var q) ? q : null;
            if (queueToBind is null)
            {
                return ValueTask.FromResult(OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Queue '{queue}' not found."))));
            }

            // check if we already have binding. if we don't, create a new one.
            var binding = Server.QueueBindings.TryGetValue(bindingKey, out var bnd) ? bnd : null;
            if (binding is null)
            {
                binding = new QueueBinding { Exchange = exchangeToBind, Arguments = arguments };
                binding.BoundQueues.Add(queue, queueToBind);

                return ValueTask.FromResult(OperationResult.Success($"Queue '{queue}' bound to exchange '{exchange}' with key '{bindingKey}'."));
            }

            // the binding exists. check if the target queue is already bound. If so, return success.
            if (!binding.BoundQueues.TryAdd(queue, queueToBind))
            {
                return ValueTask.FromResult(OperationResult.Success($"Queue '{queue}' already bound to exchange '{exchange}' with key '{bindingKey}'."));
            }

            // the binding was added. return success.
            return ValueTask.FromResult(OperationResult.Success($"Queue '{queue}' bound to exchange '{exchange}' with key '{bindingKey}'."));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult(OperationResult.Failure(ex));
        }
    }
}
