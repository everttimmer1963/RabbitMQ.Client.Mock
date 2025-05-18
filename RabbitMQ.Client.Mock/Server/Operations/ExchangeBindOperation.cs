
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Mock.Server.Bindings;

namespace RabbitMQ.Client.Mock.Server.Operations;

internal class ExchangeBindOperation(IRabbitServer server, string source, string destination, string routingKey, IDictionary<string, object?>? arguments = null) : Operation(server)
{
    public override bool IsValid => !(Server is null || string.IsNullOrEmpty(source) || string.IsNullOrEmpty(destination) || string.IsNullOrEmpty(routingKey));

    public override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            // check if all information is provided
            if (!IsValid)
            {
                return ValueTask.FromResult(OperationResult.Failure(new InvalidOperationException("Source, Target and ResourceKey are required.")));
            }

            // get the source exchange to bind to
            var sourceExchange = Server.Exchanges.TryGetValue(source, out var x) ? x : null;
            if (sourceExchange is null)
            {
                return ValueTask.FromResult(OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Exchange '{source}' not found."))));
            }

            // get the exchange to bind.
            var targetExchange = Server.Exchanges.TryGetValue(destination, out var xchg) ? xchg : null;
            if (targetExchange is null)
            {
                return ValueTask.FromResult(OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Exchange '{destination}' not found."))));
            }

            // check if we alread have binding
            var binding = Server.ExchangeBindings.TryGetValue(routingKey, out var bnd) ? bnd : null;
            if ( binding is null)
            {
                // create a new binding and add the target exchange
                binding = new ExchangeBinding { Exchange = targetExchange, Arguments = arguments };
                binding.BoundExchanges.Add(destination, targetExchange);

                // finally, add the new binding and return success.
                Server.ExchangeBindings.TryAdd(routingKey, binding);

                return ValueTask.FromResult(OperationResult.Success($"Exchange '{destination}' bound to exchange '{source}' with key '{routingKey}'."));
            }

            // the binding exists. check if the target exchange is already bound. If so, return success.
            if (!binding.BoundExchanges.TryAdd(destination, targetExchange))
            {
                return ValueTask.FromResult(OperationResult.Success($"Exchange '{destination}' already bound to exchange '{source}' with key '{routingKey}'."));
            }

            return ValueTask.FromResult(OperationResult.Success($"Exchange '{destination}' bound to exchange '{source}' with key '{routingKey}'."));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult(OperationResult.Failure(ex));
        }
    }
}
