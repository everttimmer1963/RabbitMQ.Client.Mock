using RabbitMQ.Client.Mock.Server.Data;

namespace RabbitMQ.Client.Mock.Server.Exchanges;
internal class HeadersExchange(IRabbitServer server, string name) : RabbitExchange(server, name, ExchangeType.Headers)
{
    public override ValueTask PublishMessageAsync(string routingKey, RabbitMessage message)
    {
        throw new NotImplementedException();
    }
}
