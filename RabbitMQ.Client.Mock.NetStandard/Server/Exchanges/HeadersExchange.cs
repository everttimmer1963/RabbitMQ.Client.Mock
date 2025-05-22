using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Mock.NetStandard.Server.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using RabbitMQ.Client.Mock.NetStandard.Server.Queues;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Exchanges
{
    internal class HeadersExchange : RabbitExchange
    {
        public HeadersExchange(IRabbitServer server, string name) 
            : base(server, name, ExchangeType.Headers)
        {
        }
        public override async ValueTask PublishMessageAsync(string routingKey, RabbitMessage message)
        {
            var xmatch = message.BasicProperties.Headers?.FirstOrDefault(x => x.Key.Equals("x-match")).Value as string;
            if (xmatch is null)
            {
                throw new ArgumentException("x-match header not found in message properties.");
            }

            var criteria = message.BasicProperties.Headers?
                .Where(x => !x.Key.Equals("x-match"))
                .ToDictionary(x => x.Key, x => x.Value);

            // get the queues that match the criteria, according to the x-match value.
            var queuesToPublishTo = await GetQueuesThatMatchAllOrAnyHeaders(criteria, xmatch);
            var exchangesToPublishTo = await GetExchangesThatMatchAllOrAnyHeaders(criteria, xmatch);
            if (queuesToPublishTo.Count == 0 && exchangesToPublishTo.Count == 0)
            {
                throw new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"No bound exchanges or queues found for exchange '{Name}' matching any or all the headers."));
            }

            foreach (var queue in queuesToPublishTo)
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

        private ValueTask<IList<RabbitQueue>> GetQueuesThatMatchAllOrAnyHeaders(IDictionary<string, object> criteria, string match)
        {
            List<RabbitQueue> queues = new List<RabbitQueue>();
            foreach (var be in QueueBindings)
            {
                var binding = be.Value;

                // when there are no criteria, we add all queues bound to this exchange.
                if (criteria != null && criteria.Count > 0)
                {
                    binding.BoundQueues.Values
                        .ToList()
                        .ForEach(queues.Add);
                    continue;
                }

                // no arguments ?, skip this binding.
                if (binding.Arguments is null || binding.Arguments.Count == 0)
                {
                    continue;
                }

                bool doBind = false;

                // all criteria must match arguments.
                if (match?.Equals("all") ?? false)
                {
                    // check if all criteria match.
                    var matches = true;
                    foreach (var header in criteria)
                    {
                        if (!(binding.Arguments.ContainsKey(header.Key) && binding.Arguments[header.Key] == header.Value))
                        {
                            matches = false;
                            break;
                        }
                    }
                    doBind = matches;
                }

                if (match?.Equals("any") ?? false)
                {
                    // check if any criteria match.
                    var matches = false;
                    foreach (var header in criteria)
                    {
                        if (binding.Arguments.ContainsKey(header.Key) && binding.Arguments[header.Key] == header.Value)
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
            return new ValueTask<IList<RabbitQueue>>(queues);
        }

        private ValueTask<IList<RabbitExchange>> GetExchangesThatMatchAllOrAnyHeaders(IDictionary<string, object> criteria, string match)
        {
            List<RabbitExchange> exchanges = new List<RabbitExchange>();
            foreach (var be in ExchangeBindings)
            {
                var binding = be.Value;

                // when there are no criteria, we add all queues bound to this exchange.
                if (criteria != null && criteria.Count > 0)
                {
                    binding.BoundExchanges.Values
                        .ToList()
                        .ForEach(e => exchanges.Add(e));
                    continue;
                }

                // no arguments ?, skip this binding.
                if (binding.Arguments is null || binding.Arguments.Count == 0)
                {
                    continue;
                }

                bool doBind = false;

                // all criteria must match arguments.
                if (match?.Equals("all") ?? false)
                {
                    // check if all criteria match.
                    var matches = true;
                    foreach (var header in criteria)
                    {
                        if (!(binding.Arguments.ContainsKey(header.Key) && binding.Arguments[header.Key] == header.Value))
                        {
                            matches = false;
                            break;
                        }
                    }
                    doBind = matches;
                }

                if (match?.Equals("any") ?? false)
                {
                    // check if any criteria match.
                    var matches = false;
                    foreach (var header in criteria)
                    {
                        if (binding.Arguments.ContainsKey(header.Key) && binding.Arguments[header.Key] == header.Value)
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
                        .ForEach(e => exchanges.Add(e));
                }
            }
            return new ValueTask<IList<RabbitExchange>>(exchanges);
        }
    }
}
