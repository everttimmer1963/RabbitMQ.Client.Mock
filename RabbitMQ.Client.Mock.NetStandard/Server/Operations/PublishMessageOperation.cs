using RabbitMQ.Client.Mock.NetStandard.Server.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Operations
{
    internal class PublishMessageOperation : Operation
    {
        readonly string _exchange;
        readonly string _routingKey;
        readonly RabbitMessage _message;

        public PublishMessageOperation(IRabbitServer server, string exchange, string routingKey, RabbitMessage message)
            : base(server)
        {
            this._exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
            this._routingKey = routingKey ?? throw new ArgumentNullException(nameof(routingKey));
            this._message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public override bool IsValid => !(Server is null || string.IsNullOrEmpty(_exchange) || string.IsNullOrWhiteSpace(_routingKey) || _message is null);

        public override async ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!IsValid)
                {
                    return OperationResult.Failure(new InvalidOperationException("RoutingKey and Message are required."));
                }

                bool published = false;

                // get the exchange to publish to.
                var exchangeToPublishTo = Server.Exchanges.TryGetValue(_exchange, out var x) ? x : null;
                if (exchangeToPublishTo is null)
                {
                    return OperationResult.Warning($"Exchange '{_exchange}' not found.");
                }

                // route the message to any bound exchanges.
                var exchangeBindings = exchangeToPublishTo.ExchangeBindings.TryGetValue(_routingKey, out var b) ? b : null;
                if (exchangeBindings != null)
                {
                    foreach (var boundExchange in exchangeBindings.BoundExchanges)
                    {
                        await boundExchange.Value.PublishMessageAsync(_routingKey, _message).ConfigureAwait(false);
                        Console.WriteLine($"{GetType().Name}: Message published to exchange: {boundExchange.Value.Name}");
                        published = true;
                    }
                }

                // route the message to any bound queues.
                var queueBindings = exchangeToPublishTo.QueueBindings.TryGetValue(_routingKey, out var q) ? q : null;
                if (queueBindings != null)
                {
                    foreach (var boundQueue in queueBindings.BoundQueues)
                    {
                        await boundQueue.Value.PublishMessageAsync(_message).ConfigureAwait(false);
                        Console.WriteLine($"{GetType().Name}: Message published to queue: {boundQueue.Value.Name}");
                        published = true;
                    }
                }

                // return success.
                if (!published)
                {
                    return OperationResult.Warning($"{GetType().Name}: No bound exchanges or queues found for message delivery.");
                }

                // done
                return OperationResult.Warning($"{GetType().Name}: The message has succesfully been published.");
            }
            catch (Exception ex)
            {
                return OperationResult.Failure(ex);
            }
        }
    }
}