using RabbitMQ.Client.Mock.Server.Data;
using RabbitMQ.Client.Mock.Server.Exchanges;

namespace RabbitMQ.Client.Mock.Server.Operations;

internal class PublishMessageOperation(IRabbitServer server, RabbitExchange exchange, string routingKey, RabbitMessage message) : Operation<object>(server)
{
    public override bool IsValid => !(Server is null || exchange is null || string.IsNullOrWhiteSpace(routingKey) || message is null);

    public override ValueTask<OperationResult<object>> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            if(!IsValid)
            {
                return ValueTask.FromResult(OperationResult.Failure<object>(new InvalidOperationException("RoutingKey and Message are required.")));
            }

            bool published = false;

            // route the message to any bound exchanges.
            var exchangeBindings = exchange.ExchangeBindings.TryGetValue(routingKey, out var b) ? b : null;
            if (exchangeBindings is not null)
            {
                foreach (var boundExchange in exchangeBindings.BoundExchanges)
                {
                    boundExchange.Value.PublishMessageAsync(routingKey, message);
                    Console.WriteLine($"{GetType().Name}: Message published to exchange: {boundExchange.Value.Name}");
                    published = true;
                }
            }

            // route the message to any bound queues.
            var queueBindings = exchange.QueueBindings.TryGetValue(routingKey, out var q) ? q : null;
            if (queueBindings is not null)
            {
                foreach (var boundQueue in queueBindings.BoundQueues)
                {
                    boundQueue.Value.PublishMessageAsync(message);
                    Console.WriteLine($"{GetType().Name}: Message published to queue: {boundQueue.Value.Name}");
                    published = true;
                }
            }

            // return success.
            if(!published)
            {
                return ValueTask.FromResult(OperationResult.Failure<object>($"{GetType().Name}: No bound exchanges or queues found for message delivery."));
            }

            // done
            return ValueTask.FromResult(OperationResult.Success<object>($"{GetType().Name}: The message has succesfully been published."));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult(OperationResult.Failure<object>(ex));
        }
    }
}
