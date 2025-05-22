using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace RabbitMQ.Client.Mock.Server.Operations;
internal class ExchangeUnbindOperation(IRabbitServer server, string source, string destination, string routingKey, IDictionary<string, object?>? arguments = null) : Operation(server)
{
    public override bool IsValid => !(Server is null);

    public override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        { 
            if(!IsValid)
            {
                return ValueTask.FromResult(OperationResult.Failure(new InvalidOperationException("Source, Target and ResourceKey are required.")));
            }

            // get the source exchange to unbind from
            var sourceExchange = Server.Exchanges.TryGetValue(source, out var x) ? x : null;
            if (sourceExchange is null)
            {
                return ValueTask.FromResult(OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Exchange '{source}' not found."))));
            }

            // get the exchange to unbind.
            var exchange = Server.Exchanges.TryGetValue(destination, out var xchg) ? xchg : null;
            if (exchange is null)
            {
                return ValueTask.FromResult(OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Exchange '{destination}' not found."))));
            }

            // check if we have a binding
            var binding = Server.ExchangeBindings.TryGetValue(routingKey, out var bnd) ? bnd : null;
            if (binding is null)
            {
                return ValueTask.FromResult(OperationResult.Warning($"Binding '{routingKey}' not found."));
            }

            // remove the target exchange from the binding
            if (!binding.BoundExchanges.Remove(destination))
            {
                return ValueTask.FromResult(OperationResult.Success($"Exchange '{destination}' has already been unbound from exchange '{source}' with key '{routingKey}'."));
            }

            // check if the binding is empty and if so, remove it
            if (binding.BoundExchanges.Count == 0)
            {
                Server.ExchangeBindings.Remove(routingKey, out _);
                return ValueTask.FromResult(OperationResult.Success($"Binding '{routingKey}' removed."));
            }

            // the exchange binding was removed. return success.
            return ValueTask.FromResult(OperationResult.Success($"Exchange '{destination}' unbound from exchange '{source}' with key '{routingKey}'."));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult(OperationResult.Failure(ex));
        }
    }
}
