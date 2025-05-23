﻿using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Mock.Server.Data;
using RabbitMQ.Client.Mock.Server.Operations;
using RabbitMQ.Client.Mock.Server.Queues;

namespace RabbitMQ.Client.Mock.Server.Exchanges;
internal class TopicExchange(IRabbitServer server, string name) : RabbitExchange(server, name, ExchangeType.Topic)
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

        // get all bindings of which the routing key is a topic match
        var matchingQueueBindings = QueueBindings.Where(x => IsTopicMatch(x.Key, routingKey)).Select(x => x.Key).ToArray();
        var matchingExchangeBindings = ExchangeBindings.Where(x => IsTopicMatch(x.Key, routingKey)).Select(x => x.Key).ToArray();
        if (matchingQueueBindings.Length == 0 && matchingExchangeBindings.Length == 0)
        {
            throw new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"No bound exchanges or queues found for exchange '{Name}' matching the routing key topics '{routingKey}'."));
        }

        // publish to all queues bound to this exchange with the specified routing key.
        foreach (var bindingKey in matchingQueueBindings)
        { 
            var queueBinding = QueueBindings.TryGetValue(bindingKey, out var qb) ? qb : null;
            if (queueBinding is not null)
            {
                foreach (var boundQueue in queueBinding.BoundQueues)
                {
                    // publish the message to the queue.
                    await boundQueue.Value.PublishMessageAsync(message).ConfigureAwait(false);
                    Console.WriteLine($"{GetType().Name}: Message published to queue: {boundQueue.Value.Name}");
                }
            }
        }

        // publish to all exchanges bound to this exchange with the specified routing key.
        foreach (var bindingKey in matchingExchangeBindings)
        { 
            var exchangeBinding = ExchangeBindings.TryGetValue(bindingKey, out var e) ? e : null;
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
    }

    public override ValueTask<OperationResult> QueueBindAsync(RabbitQueue queue, IDictionary<string, object?>? arguments = null, string? routingKey = null)
    {
        return base.QueueBindAsync(queue, arguments, routingKey);
    }

    private bool IsTopicMatch(string bindingKey, string routingKey)
    {
        // split the binding key and routing key into parts
        var routingParts = routingKey.Split(".");
        var bindingParts = NormalizeBindingKey(bindingKey, routingParts.Length);

        // the number of parts should be the same now.
        if (bindingParts.Length != routingParts.Length)
        {
            return false;
        }

        // now check all topics. topicMatch is true by default
        var topicMatch = true;
        for (int index = 0; index < routingParts.Length; index++)
        {
            // if the binding part is a wildcard, continue.
            if (bindingParts[index].Equals("*"))
            {
                continue;
            }

            topicMatch &= (!string.IsNullOrWhiteSpace(routingParts[index]) && routingParts[index].Equals(bindingParts[index], StringComparison.OrdinalIgnoreCase));
        }

        // return the result
        return topicMatch;
    }

    private string[] NormalizeBindingKey(string bindingKey, int length)
    {
        var parts = bindingKey.Split('.');
        if (bindingKey.StartsWith("#"))
        {
            parts[0] = parts[0].TrimStart('#');
            var partsToInsert = CreatePadding(length - parts.Length);
            parts = partsToInsert.Concat(parts).ToArray();
        }
        if (bindingKey.EndsWith("#"))
        {
            parts[^1] = parts[^1].TrimEnd('#');
            var partsToAppend = CreatePadding(length - parts.Length);
            parts = parts.Concat(partsToAppend).ToArray();
        }
        return parts;
    }

    private string[] CreatePadding(int length)
    {
        var padding = new string[length];
        for (int i = 0; i < length; i++)
        {
            padding[i] = "*";
        }
        return padding;
    }
}
