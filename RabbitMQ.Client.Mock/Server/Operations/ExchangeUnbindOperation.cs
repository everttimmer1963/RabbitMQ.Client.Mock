using RabbitMQ.Client.Mock.Server.Exchanges;

namespace RabbitMQ.Client.Mock.Server.Operations;
internal class ExchangeUnbindOperation(IRabbitServer server, RabbitExchange source, string target, string routingKey, IDictionary<string, object?>? arguments = null) : Operation(server)
{
    public override bool IsValid => !(Server is null || source is null || string.IsNullOrEmpty(target) || string.IsNullOrEmpty(routingKey));

    public override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        { 
            if(!IsValid)
            {
                return ValueTask.FromResult(OperationResult.Failure("Source, Target and ResourceKey are required."));
            }

            // get the exchange to unbind.
            var exchange = Server.Exchanges.TryGetValue(target, out var xchg) ? xchg : null;
            if (exchange is null)
            {
                return ValueTask.FromResult(OperationResult.Failure($"Exchange '{target}' not found."));
            }

            // check if we have a binding
            var binding = Server.ExchangeBindings.TryGetValue(routingKey, out var bnd) ? bnd : null;
            if (binding is null)
            {
                return ValueTask.FromResult(OperationResult.Failure($"Binding '{routingKey}' not found."));
            }

            // remove the target exchange from the binding
            if (!binding.BoundExchanges.Remove(target))
            {
                return ValueTask.FromResult(OperationResult.Success($"Exchange '{source.Name}' has already been unbound from exchange '{target}' with key '{routingKey}'."));
            }

            // check if the binding is empty and if so, remove it
            if (binding.BoundExchanges.Count == 0)
            {
                Server.ExchangeBindings.Remove(routingKey, out _);
                return ValueTask.FromResult(OperationResult.Success($"Binding '{routingKey}' removed."));
            }

            // the exchange binding was removed. return success.
            return ValueTask.FromResult(OperationResult.Success($"Exchange '{source.Name}' unbound from exchange '{target}' with key '{routingKey}'."));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult(OperationResult.Failure(ex));
        }
    }
}
