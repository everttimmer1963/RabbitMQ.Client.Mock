
using RabbitMQ.Client.Mock.Server.Bindings;
using RabbitMQ.Client.Mock.Server.Exchanges;

namespace RabbitMQ.Client.Mock.Server.Operations;

internal class BindExchangeOperation(IRabbitServer server, RabbitExchange source, string target, string resourceKey, IDictionary<string, object?>? arguments = null) : Operation(server)
{
    public RabbitExchange Source { get; } = source;
    public string Target { get; } = target;
    public string ResourceKey { get; } = resourceKey;
    public IDictionary<string, object?>? Arguments { get; set; } = arguments;
    public bool IsValid => !(Server is null || Source is null || string.IsNullOrEmpty(Target) || string.IsNullOrEmpty(ResourceKey));
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
            var exchange = Server.Exchanges.TryGetValue(Target, out var xchg) ? xchg : null;
            if (exchange is null)
            {
                return ValueTask.FromResult(OperationResult.Failure($"Exchange '{Target}' not found."));
            }

            // check if we alread have binding
            var binding = Server.ExchangeBindings.TryGetValue(ResourceKey, out var bnd) ? bnd : null;
            if ( binding is null)
            {
                // create a new binding and add the target exchange
                binding = new ExchangeBinding { Arguments = Arguments };
                binding.BoundExchanges.Add(Target, exchange);

                // finally, add the new binding and return success.
                Server.ExchangeBindings.TryAdd(ResourceKey, binding);

                return ValueTask.FromResult(OperationResult.Success($"Exchange '{Source.Name}' bound to exchange '{Target}' with key '{ResourceKey}'."));
            }

            // the binding exists. check if the target exchange is already bound. If so, return success.
            if (!binding.BoundExchanges.TryAdd(Target, exchange))
            {
                return ValueTask.FromResult(OperationResult.Success($"Exchange '{Source.Name}' already bound to exchange '{Target}' with key '{ResourceKey}'."));
            }

            return ValueTask.FromResult(OperationResult.Success($"Exchange '{Source.Name}' bound to exchange '{Target}' with key '{ResourceKey}'."));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult(OperationResult.Failure(ex));
        }
    }
}
