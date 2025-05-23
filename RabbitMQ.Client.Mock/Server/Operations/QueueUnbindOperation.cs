﻿using RabbitMQ.Client.Mock.Server.Exchanges;

namespace RabbitMQ.Client.Mock.Server.Operations;

internal class QueueUnbindOperation(IRabbitServer server, string exchange, string queue, string bindingKey, IDictionary<string, object?>? arguments = null) : Operation(server)
{
    public override bool IsValid => !(Server is null || exchange is null || string.IsNullOrWhiteSpace(queue) || string.IsNullOrWhiteSpace(bindingKey));

    public override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        { 
            if(!IsValid)
            {
                return ValueTask.FromResult(OperationResult.Warning("Exchange, Queue and BindingKey are required."));
            }

            // get the exchange to unbind from
            var exchangeToUnbindFrom = Server.Exchanges.TryGetValue(exchange, out var x) ? x : null;
            if (exchangeToUnbindFrom is null)
            {
                return ValueTask.FromResult(OperationResult.Warning($"Exchange '{exchange}' not found."));
            }

            // get the queue to unbind.
            var queueToUnbind = Server.Queues.TryGetValue(queue, out var q) ? q : null;
            if (queueToUnbind is null)
            {
                return ValueTask.FromResult(OperationResult.Warning($"Queue '{queue}' not found."));
            }

            // check if we have a binding
            var binding = Server.QueueBindings.TryGetValue(bindingKey, out var bnd) ? bnd : null;
            if (binding is null)
            {
                return ValueTask.FromResult(OperationResult.Warning($"Binding '{bindingKey}' not found."));
            }

            // remove the target queue from the binding
            if (!binding.BoundQueues.Remove(queue))
            {
                return ValueTask.FromResult(OperationResult.Success($"Queue '{queueToUnbind.Name}' has already been unbound from exchange '{exchangeToUnbindFrom.Name}' with key '{bindingKey}'."));
            }

            // check if the binding is empty and if so, remove it
            if (binding.BoundQueues.Count == 0)
            {
                Server.QueueBindings.Remove(bindingKey, out _);
                return ValueTask.FromResult(OperationResult.Success($"Binding '{bindingKey}' removed."));
            }

            // the queue binding was removed. return success.
            return ValueTask.FromResult(OperationResult.Success($"Queue '{queueToUnbind.Name}' unbound from exchange '{exchangeToUnbindFrom.Name}' with key '{bindingKey}'."));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult(OperationResult.Failure(ex));
        }
    }
}
