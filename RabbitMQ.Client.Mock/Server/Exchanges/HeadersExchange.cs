using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Mock.Server.Data;
using RabbitMQ.Client.Mock.Server.Queues;

namespace RabbitMQ.Client.Mock.Server.Exchanges;
internal class HeadersExchange(IRabbitServer server, string name) : RabbitExchange(server, name, ExchangeType.Headers)
{
    public override async ValueTask PublishMessageAsync(string routingKey, RabbitMessage message)
    {
        var criteria = message.BasicProperties.Headers?
            .Where(x => !x.Key.Equals("x-match"))
            .ToDictionary(x => x.Key, x => x.Value);

        // get the queues that match the criteria, according to the x-match value.
        var queuesToPublishTo = await GetQueuesThatMatchAllOrAnyHeaders(criteria);
        var exchangesToPublishTo = await GetExchangesThatMatchAllOrAnyHeaders(criteria);
        if (queuesToPublishTo.Count == 0 && exchangesToPublishTo.Count == 0)
        {
            throw new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"No bound exchanges or queues found for exchange '{Name}' matching any or all the headers."));
        }

        foreach(var queue in queuesToPublishTo)
        {
            // publish the message to the queue.
            await queue.PublishMessageAsync(message).ConfigureAwait(false);
            Console.WriteLine($"{GetType().Name}: Message published to queue: {queue.Name}");
        }

        foreach (var exchange in exchangesToPublishTo)
        {
            await exchange.PublishMessageAsync(routingKey, message).ConfigureAwait(false);
            Console.WriteLine($"{GetType().Name}: Message published to exchange: {exchange.Name}");
        }
    }

    private ValueTask<IList<RabbitQueue>> GetQueuesThatMatchAllOrAnyHeaders(IDictionary<string, object?>? arguments)
    {
        List<RabbitQueue> queues = new();
        foreach (var be in QueueBindings)
        {
            var binding = be.Value;
            Dictionary<string,object?> bindingHeaders = binding.Arguments?.ToDictionary(arg => arg.Key, arg => arg.Value) ?? new Dictionary<string, object?>();

            // no operator found, then we skip this binding.
            var exists = bindingHeaders.TryGetValue("x-match", out var m);
            if(!exists || string.IsNullOrWhiteSpace((string?)m))
            {
                continue;
            }

            // get remaining criteria.
            string match = (string)m!;
            var criteria = bindingHeaders.Where(x => !x.Key.Equals("x-match")).ToDictionary(x => x.Key, x => x.Value);

            // when there are no criteria, we add all queues bound to this exchange.
            if (criteria.Count == 0)
            {
                binding.BoundQueues.Values
                    .ToList()
                    .ForEach(queues.Add);
                continue;
            }

            // okay, we have criteria. now we must have arguments too.
            Dictionary<string,object?> headers = arguments?.ToDictionary(arg => arg.Key, arg => arg.Value) ?? new Dictionary<string, object?>();
            bool doBind = false;

            // all criteria must match arguments.
            if (match.Equals("all"))
            {
                // check if all criteria match.
                var matches = true;
                foreach (var header in headers)
                {
                    if (!(criteria.ContainsKey(header.Key) && criteria[header.Key] == header.Value))
                    {
                        matches = false;
                        break;
                    }
                }
                doBind = matches;
            }

            if (match.Equals("any"))
            {
                // check if any criteria match.
                var matches = false;
                foreach (var header in headers)
                {
                    if (criteria.ContainsKey(header.Key) && criteria[header.Key] == header.Value)
                    {
                        matches = true;
                        break;
                    }
                }
                doBind = matches;
            }

            // add the bound queues to the list, if applicable
            if (doBind)
            {
                binding.BoundQueues.Values
                    .ToList()
                    .ForEach(queues.Add);
            }
        }
        return ValueTask.FromResult<IList<RabbitQueue>>(queues);
    }

    private ValueTask<IList<RabbitExchange>> GetExchangesThatMatchAllOrAnyHeaders(IDictionary<string, object?>? arguments)
    {
        List<RabbitExchange> exchanges = new();
        foreach (var be in ExchangeBindings)
        {
            var binding = be.Value;
            Dictionary<string,object?> bindingHeaders = binding.Arguments?.ToDictionary(arg => arg.Key, arg => arg.Value) ?? new Dictionary<string, object?>();


            // no operator found, then we skip this binding.
            var exists = bindingHeaders.TryGetValue("x-match", out var m);
            if (!exists || string.IsNullOrWhiteSpace((string?)m))
            {
                continue;
            }

            // get remaining criteria.
            string match = (string)m!;
            var criteria = bindingHeaders.Where(x => !x.Key.Equals("x-match")).ToDictionary(x => x.Key, x => x.Value);

            // when there are no criteria, we add all queues bound to this exchange.
            if (arguments is not { Count: > 0 })
            {
                binding.BoundExchanges.Values
                    .ToList()
                    .ForEach(exchanges.Add);
                continue;
            }

            // no arguments ?, skip this binding.
            if (binding.Arguments is null || binding.Arguments.Count == 0)
            {
                continue;
            }

            // okay, we have criteria. now we must have arguments too.
            Dictionary<string, object?> headers = arguments?.ToDictionary(arg => arg.Key, arg => arg.Value) ?? new Dictionary<string, object?>();
            bool doBind = false;

            // all criteria must match arguments.
            if (match.Equals("all"))
            {
                // check if all criteria match.
                var matches = true;
                foreach (var header in headers)
                {
                    if (!(criteria.ContainsKey(header.Key) && criteria[header.Key] == header.Value))
                    {
                        matches = false;
                        break;
                    }
                }
                doBind = matches;
            }

            if (match.Equals("any"))
            {
                // check if any criteria match.
                var matches = false;
                foreach (var header in headers)
                {
                    if (criteria.ContainsKey(header.Key) && criteria[header.Key] == header.Value)
                    {
                        matches = true;
                        break;
                    }
                }
                doBind = matches;
            }

            // add the bound queues to the list, if applicable
            if (doBind)
            {
                binding.BoundExchanges.Values
                    .ToList()
                    .ForEach(exchanges.Add);
            }
        }
        return ValueTask.FromResult<IList<RabbitExchange>>(exchanges);
    }
}
