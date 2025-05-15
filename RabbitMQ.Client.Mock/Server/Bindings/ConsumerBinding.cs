using RabbitMQ.Client.Mock.Server.Queues;

namespace RabbitMQ.Client.Mock.Server.Bindings;

internal class ConsumerBinding(RabbitQueue queue, IAsyncBasicConsumer consumer)
{ 
    public RabbitQueue Queue { get; } = queue;
    public IAsyncBasicConsumer Consumer { get; } = consumer;
}
