using RabbitMQ.Client.Mock.Server.Exchanges;
using RabbitMQ.Client.Mock.Server.Queues;
using System.Collections.Concurrent;

namespace RabbitMQ.Client.Mock.Server.Bindings;

internal class QueueBinding
{
    public required RabbitExchange Exchange { get; set; }
    public IDictionary<string, object?>? Arguments { get; set; } = null;
    public IDictionary<string, RabbitQueue> BoundQueues { get; } = new ConcurrentDictionary<string, RabbitQueue>();
}
