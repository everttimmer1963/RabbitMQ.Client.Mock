using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Mock.Server.Bindings;
using RabbitMQ.Client.Mock.Server.Data;
using RabbitMQ.Client.Mock.Server.Operations;
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

    public override async ValueTask<OperationResult> QueueBindAsync(RabbitQueue queueToBind, IDictionary<string, object?>? arguments = null, string? bindingKey = null)
    {
        // Header exchanges differentiate it bound queues by the arguments. The binding key isn't used.
        // In order to avoid confusion, we use the binding arguments as the key for the binding by conveting
        // it to a string.
        //
        // this is a header exchange so we should have at least one header in the arguments.
        if (arguments is null || arguments.Count == 0)
        {
            return OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Header exchange '{Name}' requires at least one header in the arguments.")));
        }

        // create a bindingkey string from the arguments.
        var key = await ConvertToBindingStringAsync(arguments);

        // check if we already have binding. if we don't, create a new one.
        var binding = Server.QueueBindings.TryGetValue(key, out var bnd) ? bnd : null;
        if (binding is null)
        {
            binding = new QueueBinding { Exchange = this, Arguments = arguments };
            binding.BoundQueues.Add(queueToBind.Name, queueToBind);
            Server.QueueBindings.TryAdd(key, binding);

            return OperationResult.Success($"Queue '{queueToBind.Name}' bound to exchange '{this.Name}' with key '{key}'.");
        }

        // the binding exists. check if the target queue is already bound. If so, return success.
        if (!binding.BoundQueues.TryAdd(queueToBind.Name, queueToBind))
        {
            return OperationResult.Success($"Queue '{queueToBind.Name}' already bound to exchange '{Name}' with key '{key}'.");
        }

        // the binding was added. return success.
        return OperationResult.Success($"Queue '{queueToBind.Name}' bound to exchange '{Name}' with key '{key}'.");
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
            if (match.Equals("all") || match.Equals("all-with-x"))
            {
                // check if all criteria match.
                var filteredHeaders = match.Equals("all")
                    ? headers.Where(x => !x.Key.StartsWith("x-")).ToDictionary(x => x.Key, x => x.Value)
                    : headers;
                var matches = true;
                foreach (var header in filteredHeaders)
                {
                    if (!(criteria.ContainsKey(header.Key) && criteria[header.Key] == header.Value))
                    {
                        matches = false;
                        break;
                    }
                }
                doBind = matches;
            }

            if (match.Equals("any") || match.Equals("any-with-x"))
            {
                // check if any criteria match.
                var filteredHeaders = match.Equals("any") 
                    ? headers.Where(x => !x.Key.StartsWith("x-")).ToDictionary(x => x.Key, x => x.Value)
                    : headers;
                var matches = false;
                foreach (var header in filteredHeaders)
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
            if (match.Equals("all") || match.Equals("all-with-x"))
            {
                // check if all criteria match.
                var filteredHeaders = match.Equals("all")
                    ? headers.Where(x => !x.Key.StartsWith("x-")).ToDictionary(x => x.Key, x => x.Value)
                    : headers;
                var matches = true;
                foreach (var header in filteredHeaders)
                {
                    if (!(criteria.ContainsKey(header.Key) && criteria[header.Key] == header.Value))
                    {
                        matches = false;
                        break;
                    }
                }
                doBind = matches;
            }

            if (match.Equals("any") || match.Equals("any-with-x"))
            {
                // check if any criteria match.
                var filteredHeaders = match.Equals("any")
                    ? headers.Where(x => !x.Key.StartsWith("x-")).ToDictionary(x => x.Key, x => x.Value)
                    : headers;
                var matches = false;
                foreach (var header in filteredHeaders)
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

    private ValueTask<string> ConvertToBindingStringAsync(IDictionary<string, object?> arguments)
    {
        var entries = arguments
            .Where(arg => !arg.Key.Equals("x-match"))
            .OrderBy(arg => arg.Key)
            .Select(entry => $"{entry.Key}={entry.Value}")
            .ToArray();
        return ValueTask.FromResult(string.Join(';', entries));
    }
}
