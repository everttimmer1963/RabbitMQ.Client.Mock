using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Mock.NetStandard.Server.Bindings;
using RabbitMQ.Client.Mock.NetStandard.Server.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Exchanges
{
    internal class DirectExchange : RabbitExchange
    {
        public DirectExchange(IRabbitServer server, string name)
            : base(server, name, ExchangeType.Direct)
        { 
        }

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
            if (queueBinding != null)
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
            if (exchangeBinding != null)
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
