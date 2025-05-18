using RabbitMQ.Client.Mock.Server.Data;
using RabbitMQ.Client.Mock.Server.Exchanges;

namespace RabbitMQ.Client.Mock.Server.Operations;

internal class PublishMessageOperation(IRabbitServer server, string exchange, string routingKey, RabbitMessage message) : Operation(server)
{
    public override bool IsValid => !(Server is null || string.IsNullOrEmpty(exchange) || string.IsNullOrWhiteSpace(routingKey) || message is null);

    public override async ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            if(!IsValid)
            {
                return OperationResult.Failure(new InvalidOperationException("RoutingKey and Message are required."));
            }

            bool published = false;

            // get the exchange to publish to.
            var exchangeToPublishTo = Server.Exchanges.TryGetValue(exchange, out var x) ? x : null;
            if (exchangeToPublishTo is null)
            {
                return OperationResult.Warning($"Exchange '{exchange}' not found.");
            }

            // route the message to any bound exchanges.
            var exchangeBindings = exchangeToPublishTo.ExchangeBindings.TryGetValue(routingKey, out var b) ? b : null;
            if (exchangeBindings is not null)
            {
                foreach (var boundExchange in exchangeBindings.BoundExchanges)
                {
                    await boundExchange.Value.PublishMessageAsync(routingKey, message).ConfigureAwait(false);
                    Console.WriteLine($"{GetType().Name}: Message published to exchange: {boundExchange.Value.Name}");
                    published = true;
                }
            }

            // route the message to any bound queues.
            var queueBindings = exchangeToPublishTo.QueueBindings.TryGetValue(routingKey, out var q) ? q : null;
            if (queueBindings is not null)
            {
                foreach (var boundQueue in queueBindings.BoundQueues)
                {
                    await boundQueue.Value.PublishMessageAsync(message).ConfigureAwait(false);
                    Console.WriteLine($"{GetType().Name}: Message published to queue: {boundQueue.Value.Name}");
                    published = true;
                }
            }

            // return success.
            if(!published)
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
