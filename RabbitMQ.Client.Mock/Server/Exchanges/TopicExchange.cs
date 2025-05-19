using RabbitMQ.Client.Mock.Server.Data;

namespace RabbitMQ.Client.Mock.Server.Exchanges;
internal class TopicExchange(IRabbitServer server, string name) : RabbitExchange(server, name, ExchangeType.Topic)
{
    public override ValueTask PublishMessageAsync(string routingKey, RabbitMessage message)
    {
        throw new NotImplementedException();
    }
}
