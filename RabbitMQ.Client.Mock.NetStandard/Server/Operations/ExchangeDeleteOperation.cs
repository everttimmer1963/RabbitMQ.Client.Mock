
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Operations
{
    internal class ExchangeDeleteOperation : Operation
    {
        readonly string _exchange;
        readonly bool _ifUnused;

        public ExchangeDeleteOperation(IRabbitServer server, string exchange, bool ifUnused = false)
            : base(server)
        {
            _exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
            _ifUnused = ifUnused;
        }

        public override bool IsValid => !(Server is null || string.IsNullOrEmpty(_exchange));

        public override async ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!IsValid)
                {
                    return OperationResult.Failure(new InvalidOperationException("Exchange is required."));
                }

                // check if the exchange exists. if it does not, return a warning message
                var exchangeInstance = Server.Exchanges.TryGetValue(_exchange, out var x) ? x : null;
                if (exchangeInstance is null)
                {
                    return OperationResult.Warning($"Exchange '{_exchange}' not found.");
                }

                // get the keys of all bindings that have the exchange as source or target.
                var ownedBindings = Server.ExchangeBindings
                    .Where(e => e.Value.Exchange.Name.Equals(_exchange))
                    .Select(e => e.Key)
                    .ToArray();
                var dependantBindings = Server.ExchangeBindings
                    .Where(e => e.Value.BoundExchanges.ContainsKey(_exchange))
                    .ToArray();

                // check if the exchange is used as source or target in any binding,
                // before we can delete it.
                if (_ifUnused && (dependantBindings.Length > 0 || ownedBindings.Length > 0))
                {
                    return OperationResult.Warning($"Exchange '{_exchange}' is in use.");
                }

                // remove all bindings that we found where the exchange is the source.
                foreach (var key in ownedBindings)
                {
                    Server.ExchangeBindings.Remove(key);
                }

                // now unbind this exchange from all bindings that have the current exchange as target.
                foreach (var binding in dependantBindings)
                {
                    var operation = new ExchangeUnbindOperation(Server, binding.Value.Exchange.Name, _exchange, binding.Key, binding.Value.Arguments);
                    await Server.Processor.EnqueueOperationAsync(operation, noWait: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                }

                // remove the exchange from the server
                if(!Server.Exchanges.ContainsKey(_exchange))
                {
                    return OperationResult.Warning($"Exchange '{_exchange}' not found.");
                }
                Server.Exchanges.Remove(_exchange);

                // return success
                return OperationResult.Success($"Exchange '{_exchange}' deleted successfully.");
            }
            catch (Exception ex)
            {
                return OperationResult.Failure(ex);
            }
        }
    }
}