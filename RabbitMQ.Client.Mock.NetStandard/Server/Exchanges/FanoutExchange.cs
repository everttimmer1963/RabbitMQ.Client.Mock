using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Mock.NetStandard.Server.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Exchanges
{
    internal class FanoutExchange : RabbitExchange
    {
        public FanoutExchange(IRabbitServer server, string name)
            : base(server, name, ExchangeType.Fanout)
        { }

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

            // publish to all queues bound to this exchange, ignoring the routing key.
            var queueBindings = QueueBindings.Values.ToArray();
            if (queueBindings != null && queueBindings.Length > 0)
            {
                foreach (var queueBinding in queueBindings)
                {
                    foreach (var boundQueue in queueBinding.BoundQueues)
                    {
                        // publish the message to the queue.
                        await boundQueue.Value.PublishMessageAsync(message).ConfigureAwait(false);
                        Console.WriteLine($"{GetType().Name}: Message published to queue: {boundQueue.Value.Name}");
                    }
                }
            }

            // if there also bound exchanges, we need to forward the message to each of them.
            var exchangeBindings = ExchangeBindings.Values.ToArray();
            if (exchangeBindings != null && exchangeBindings.Length > 0)
            {
                foreach (var exchangeBinding in exchangeBindings)
                {
                    foreach (var boundExchange in exchangeBinding.BoundExchanges)
                    {
                        // publish the message to the exchange.
                        await boundExchange.Value.PublishMessageAsync(routingKey, message).ConfigureAwait(false);
                        Console.WriteLine($"{GetType().Name}: Message published to exchange: {boundExchange.Value.Name}");
                    }
                }
            }
        }

    }
}
