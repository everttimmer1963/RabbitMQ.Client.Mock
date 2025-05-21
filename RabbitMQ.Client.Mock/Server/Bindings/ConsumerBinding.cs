using RabbitMQ.Client.Mock.Server.Queues;

namespace RabbitMQ.Client.Mock.Server.Bindings;

internal class ConsumerBinding(RabbitQueue queue, IAsyncBasicConsumer consumer)
{
    public RabbitQueue Queue { get; } = queue;
    public int ChannelNumber { get; set; }
    public bool AutoAcknowledge { get; set; }
    public bool NoLocal { get; set; }
    public bool Exclusive { get; set; }
    public IDictionary<string, object?>? Arguments { get; set; }
    public IAsyncBasicConsumer Consumer { get; } = consumer;
}
