
namespace RabbitMQ.Client.Mock.Server.Operations;
internal class ExchangeDeleteOperation(IRabbitServer server, string exchange, bool ifUnused = false) : Operation(server)
{
    public override bool IsValid => !(Server is null || string.IsNullOrEmpty(exchange));

    public override async ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            if(!IsValid)
            {
                return OperationResult.Failure(new InvalidOperationException("Exchange is required."));
            }

            // check if the exchange exists. if it does not, return a warning message
            var exchangeInstance = Server.Exchanges.TryGetValue(exchange, out var x) ? x : null;
            if (exchangeInstance is null)
            {
                return OperationResult.Warning($"Exchange '{exchange}' not found.");
            }

            // get the keys of all bindings that have the exchange as source or target.
            var ownedBindings = Server.ExchangeBindings
                .Where(e => e.Value.Exchange.Name.Equals(exchange))
                .Select(e => e.Key)
                .ToArray();
            var dependantBindings = Server.ExchangeBindings
                .Where(e => e.Value.BoundExchanges.ContainsKey(exchange))
                .ToArray();

            // check if the exchange is used as source or target in any binding,
            // before we can delete it.
            if (ifUnused && (dependantBindings.Length > 0 || ownedBindings.Length > 0))
            {
                return OperationResult.Warning($"Exchange '{exchange}' is in use.");
            }

            // remove all bindings that we found where the exchange is the source.
            foreach (var key in ownedBindings)
            {
                Server.ExchangeBindings.Remove(key);
            }

            // now unbind this exchange from all bindings that have the current exchange as target.
            foreach (var binding in dependantBindings)
            {
                var operation = new ExchangeUnbindOperation(Server, binding.Value.Exchange.Name, exchange, binding.Key, binding.Value.Arguments);
                await Server.Processor.EnqueueOperationAsync(operation, noWait: true, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            // remove the exchange from the server
            if (!Server.Exchanges.Remove(exchange, out _))
            {
                return OperationResult.Success($"Exchange '{exchange}' not found.");
            }

            // return success
            return OperationResult.Success($"Exchange '{exchange}' deleted successfully.");
        }
        catch (Exception ex)
        {
            return OperationResult.Failure(ex);
        }
    }
}
