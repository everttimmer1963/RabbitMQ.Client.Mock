
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Mock.NetStandard.Server.Bindings;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Operations
{
    internal class ExchangeBindOperation : Operation
    {
        private readonly string _source;
        private readonly string _destination;
        private readonly string _routingKey;
        private readonly IDictionary<string, object> _arguments;

        public ExchangeBindOperation(IRabbitServer server, string source, string destination, string routingKey, IDictionary<string, object> arguments = null)
            : base(server)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _destination = destination ?? throw new ArgumentNullException(nameof(destination));
            _routingKey = routingKey ?? throw new ArgumentNullException(nameof(routingKey));
            _arguments = arguments;
        }

        public override bool IsValid => !(Server is null);

        public override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                // check if all information is provided
                if (!IsValid)
                {
                    return new ValueTask<OperationResult>(OperationResult.Failure(new InvalidOperationException("Source, Target and ResourceKey are required.")));
                }

                // the destination exchange may never be the default exchange so check for that.
                if (_destination == string.Empty)
                {
                    return new ValueTask<OperationResult>(OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"The default exchange cannot be used as the destination exchange in a binding."))));
                }

                // get the source exchange to bind to
                var sourceExchange = Server.Exchanges.TryGetValue(_source, out var x) ? x : null;
                if (sourceExchange is null)
                {
                    return new ValueTask<OperationResult>(OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Exchange '{_source}' not found."))));
                }

                // get the exchange to bind.
                var targetExchange = Server.Exchanges.TryGetValue(_destination, out var xchg) ? xchg : null;
                if (targetExchange is null)
                {
                    return new ValueTask<OperationResult>(OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Exchange '{_destination}' not found."))));
                }

                // check if we alread have binding
                var binding = Server.ExchangeBindings.TryGetValue(_routingKey, out var bnd) ? bnd : null;
                if (binding is null)
                {
                    // create a new binding and add the target exchange
                    binding = new ExchangeBinding { Exchange = sourceExchange, Arguments = _arguments };
                    binding.BoundExchanges.Add(_destination, targetExchange);

                    // finally, add the new binding and return success.
                    Server.ExchangeBindings.Add(_routingKey, binding);

                    return new ValueTask<OperationResult>(OperationResult.Success($"Exchange '{_destination}' bound to exchange '{_source}' with key '{_routingKey}'."));
                }

                // the binding exists. check if the target exchange is already bound. If so, return success.
                if ( !binding.BoundExchanges.ContainsKey(_destination))
                {
                    binding.BoundExchanges.Add(_destination, targetExchange);
                    return new ValueTask<OperationResult>(OperationResult.Success($"Exchange '{_destination}' already bound to exchange '{_source}' with key '{_routingKey}'."));
                }

                return new ValueTask<OperationResult>(OperationResult.Success($"Exchange '{_destination}' bound to exchange '{_source}' with key '{_routingKey}'."));
            }
            catch (Exception ex)
            {
                return new ValueTask<OperationResult>(OperationResult.Failure(ex));
            }
        }
    }
}