using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Operations
{
    internal class ExchangeUnbindOperation : Operation
    {
        readonly string _source;
        readonly string _destination;
        readonly string _routingKey;
        readonly IDictionary<string, object> _arguments;

        public ExchangeUnbindOperation(IRabbitServer server, string source, string destination, string routingKey, IDictionary<string, object> arguments = null)
            : base(server)
        {
            this._source = source ?? throw new ArgumentNullException(nameof(source));
            this._destination = destination ?? throw new ArgumentNullException(nameof(destination));
            this._routingKey = routingKey ?? throw new ArgumentNullException(nameof(routingKey));
            this._arguments = arguments;
        }
        public override bool IsValid => !(Server is null || _source is null || string.IsNullOrEmpty(_destination) || string.IsNullOrEmpty(_routingKey));

        public override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!IsValid)
                {
                    return new ValueTask<OperationResult>(OperationResult.Failure(new InvalidOperationException("Source, Target and ResourceKey are required.")));
                }

                // get the source exchange to unbind from
                var sourceExchange = Server.Exchanges.TryGetValue(_source, out var x) ? x : null;
                if (sourceExchange is null)
                {
                    return new ValueTask<OperationResult>(OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Exchange '{_source}' not found."))));
                }

                // get the exchange to unbind.
                var exchange = Server.Exchanges.TryGetValue(_destination, out var xchg) ? xchg : null;
                if (exchange is null)
                {
                    return new ValueTask<OperationResult>(OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Exchange '{_destination}' not found."))));
                }

                // check if we have a binding
                var binding = Server.ExchangeBindings.TryGetValue(_routingKey, out var bnd) ? bnd : null;
                if (binding is null)
                {
                    return new ValueTask<OperationResult>(OperationResult.Warning($"Binding '{_routingKey}' not found."));
                }

                // remove the target exchange from the binding
                if (!binding.BoundExchanges.Remove(_destination))
                {
                    return new ValueTask<OperationResult>(OperationResult.Success($"Exchange '{_destination}' has already been unbound from exchange '{_source}' with key '{_routingKey}'."));
                }

                // check if the binding is empty and if so, remove it
                if (binding.BoundExchanges.Count == 0)
                {
                    Server.ExchangeBindings.Remove(_routingKey);
                    return new ValueTask<OperationResult>(OperationResult.Success($"Binding '{_routingKey}' removed."));
                }

                // the exchange binding was removed. return success.
                return new ValueTask<OperationResult>(OperationResult.Success($"Exchange '{_destination}' unbound from exchange '{_source}' with key '{_routingKey}'."));
            }
            catch (Exception ex)
            {
                return new ValueTask<OperationResult>(OperationResult.Failure(ex));
            }
        }
    }
}