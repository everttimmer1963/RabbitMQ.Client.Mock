
using RabbitMQ.Client.Mock.Server.Bindings;
using RabbitMQ.Client.Mock.Server.Exchanges;

namespace RabbitMQ.Client.Mock.Server.Operations;

internal class ExchangeBindOperation(IRabbitServer server, RabbitExchange source, string target, string routingKey, IDictionary<string, object?>? arguments = null) : Operation(server)
{
    public override bool IsValid => !(Server is null || source is null || string.IsNullOrEmpty(target) || string.IsNullOrEmpty(routingKey));

    public override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            // check if all information is provided
            if (!IsValid)
            {
                return ValueTask.FromResult(OperationResult.Failure("Source, Target and ResourceKey are required."));
            }

            // get the exchange to bind.
            var exchange = Server.Exchanges.TryGetValue(target, out var xchg) ? xchg : null;
            if (exchange is null)
            {
                return ValueTask.FromResult(OperationResult.Failure($"Exchange '{target}' not found."));
            }

            // check if we alread have binding
            var binding = Server.ExchangeBindings.TryGetValue(routingKey, out var bnd) ? bnd : null;
            if ( binding is null)
            {
                // create a new binding and add the target exchange
                binding = new ExchangeBinding { Exchange = source, Arguments = arguments };
                binding.BoundExchanges.Add(target, exchange);

                // finally, add the new binding and return success.
                Server.ExchangeBindings.TryAdd(routingKey, binding);

                return ValueTask.FromResult(OperationResult.Success($"Exchange '{exchange.Name}' bound to exchange '{target}' with key '{routingKey}'."));
            }

            // the binding exists. check if the target exchange is already bound. If so, return success.
            if (!binding.BoundExchanges.TryAdd(target, exchange))
            {
                return ValueTask.FromResult(OperationResult.Success($"Exchange '{source.Name}' already bound to exchange '{target}' with key '{routingKey}'."));
            }

            return ValueTask.FromResult(OperationResult.Success($"Exchange '{source.Name}' bound to exchange '{target}' with key '{routingKey}'."));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult(OperationResult.Failure(ex));
        }
    }
}
