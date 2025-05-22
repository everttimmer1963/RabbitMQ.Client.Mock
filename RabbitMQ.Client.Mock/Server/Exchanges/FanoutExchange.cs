using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Mock.Server.Bindings;
using RabbitMQ.Client.Mock.Server.Data;
using RabbitMQ.Client.Mock.Server.Operations;
using RabbitMQ.Client.Mock.Server.Queues;
using System.Xml.Linq;

namespace RabbitMQ.Client.Mock.Server.Exchanges;
internal class FanoutExchange(IRabbitServer server, string name) : RabbitExchange(server, name, ExchangeType.Fanout)
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

        // publish to all queues bound to this exchange, ignoring the routing key.
        var queueBindings = QueueBindings.Values.ToArray();
        if (queueBindings is { Length: > 0})
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
        if (exchangeBindings is { Length: > 0})
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

    public override ValueTask<OperationResult> QueueBindAsync(RabbitQueue queueToBind, IDictionary<string, object?>? arguments = null, string? bindingKey = null)
    {
        // Fanout exchanges ignore the routing key for queue bindings.
        // To get all queues in the same binding, we use the exchange name as the binding key.
        string key = this.Name;

        // check if we already have binding. if we don't, create a new one.
        var binding = Server.QueueBindings.TryGetValue(key, out var bnd) ? bnd : null;
        if (binding is null)
        {
            binding = new QueueBinding { Exchange = this, Arguments = arguments };
            binding.BoundQueues.Add(queueToBind.Name, queueToBind);
            Server.QueueBindings.TryAdd(key, binding);

            return ValueTask.FromResult(OperationResult.Success($"Queue '{queueToBind.Name}' bound to exchange '{this.Name}' with key '{key}'."));
        }

        // the binding exists. check if the target queue is already bound. If so, return success.
        if (!binding.BoundQueues.TryAdd(queueToBind.Name, queueToBind))
        {
            return ValueTask.FromResult(OperationResult.Success($"Queue '{queueToBind.Name}' already bound to exchange '{Name}' with key '{key}'."));
        }

        // the binding was added. return success.
        return ValueTask.FromResult(OperationResult.Success($"Queue '{queueToBind.Name}' bound to exchange '{Name}' with key '{key}'."));
    }
}
