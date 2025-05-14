using RabbitMQ.Client.Mock.Server.Queues;

namespace RabbitMQ.Client.Mock.Server.Bindings;

internal class QueueBinding
{
    public IDictionary<string, object?>? Arguments { get; set; } = null;
    public IList<RabbitQueue> BoundQueues { get; } = new List<RabbitQueue>();
}
