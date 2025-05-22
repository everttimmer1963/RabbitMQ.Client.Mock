using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Mock.Server.Bindings;
using RabbitMQ.Client.Mock.Server.Data;
using RabbitMQ.Client.Mock.Server.Operations;
using RabbitMQ.Client.Mock.Server.Queues;

namespace RabbitMQ.Client.Mock.Server.Exchanges;
internal class DirectExchange(IRabbitServer server, string name) : RabbitExchange(server, name, ExchangeType.Direct)
{
    public override async ValueTask PublishMessageAsync(string routingKey, RabbitMessage message)
    {
        // check if this a the default exchange.
        // if so, routingkey is the queue name and we directly publish to the queue.
        if (Name == string.Empty)
        {
            var queue = Server.Queues.TryGetValue(routingKey, out var q) ? q : null;
            if (queue is null)
            {
                throw new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Queue '{routingKey}' not found."));
            }
            await queue.PublishMessageAsync(message).ConfigureAwait(false);
        }

        // publish to all queues bound to this exchange with the specified routing key.
        var queueBinding = QueueBindings.TryGetValue(routingKey, out var qb) ? qb : null;
        if ( queueBinding is not null)
        {
            foreach (var boundQueue in queueBinding.BoundQueues)
            {
                // publish the message to the queue.
                await boundQueue.Value.PublishMessageAsync(message).ConfigureAwait(false);
                Console.WriteLine($"{GetType().Name}: Message published to queue: {boundQueue.Value.Name}");
            }
        }

        // if there also bound exchanges, we need to forward the message to each of them.
        var exchangeBinding = ExchangeBindings.TryGetValue(routingKey, out var e) ? e : null;
        if (exchangeBinding is not null)
        {
            foreach (var boundExchange in exchangeBinding.BoundExchanges)
            {
                // publish the message to the exchange.
                await boundExchange.Value.PublishMessageAsync(routingKey, message).ConfigureAwait(false);
                Console.WriteLine($"{GetType().Name}: Message published to exchange: {boundExchange.Value.Name}");
            }
        }
    }

    public override ValueTask<OperationResult> QueueBindAsync(RabbitQueue queueToBind, IDictionary<string, object?>? arguments = null, string? bindingKey = null)
    {
        // the base implementation will suffice for direct exchanges.
        return base.QueueBindAsync(queueToBind, arguments, bindingKey);
    }
}
