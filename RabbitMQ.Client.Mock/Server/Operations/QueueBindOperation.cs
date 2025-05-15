using RabbitMQ.Client.Mock.Server.Bindings;
using RabbitMQ.Client.Mock.Server.Exchanges;

namespace RabbitMQ.Client.Mock.Server.Operations;
internal class QueueBindOperation(IRabbitServer server, RabbitExchange exchange, string queue, string bindingKey, IDictionary<string, object?>? arguments = null) : Operation(server)
{
    public override bool IsValid => !(Server is null || exchange is null || string.IsNullOrWhiteSpace(queue) || string.IsNullOrWhiteSpace(bindingKey));

    public override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            if(!IsValid)
            {
                return ValueTask.FromResult(OperationResult.Failure("Exchange, Queue and BindingKey are required."));
            }

            // get the queue to bind.
            var queueToBind = Server.Queues.TryGetValue(queue, out var q) ? q : null;
            if (queueToBind is null)
            {
                return ValueTask.FromResult(OperationResult.Failure($"Queue '{queue}' not found."));
            }

            // check if we already have binding. if we don't, create a new one.
            var binding = Server.QueueBindings.TryGetValue(bindingKey, out var bnd) ? bnd : null;
            if (binding is null)
            {
                binding = new QueueBinding { Exchange = exchange, Arguments = arguments };
                binding.BoundQueues.Add(queue, queueToBind);

                return ValueTask.FromResult(OperationResult.Success($"Queue '{queueToBind.Name}' bound to exchange '{exchange.Name}' with key '{bindingKey}'."));
            }

            // the binding exists. check if the target queue is already bound. If so, return success.
            if (!binding.BoundQueues.TryAdd(queue, queueToBind))
            {
                return ValueTask.FromResult(OperationResult.Success($"Queue '{queueToBind.Name}' already bound to exchange '{exchange.Name}' with key '{bindingKey}'."));
            }

            // the binding was added. return success.
            return ValueTask.FromResult(OperationResult.Success($"Queue '{queueToBind.Name}' bound to exchange '{exchange.Name}' with key '{bindingKey}'."));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult(OperationResult.Failure(ex));
        }
    }
}
