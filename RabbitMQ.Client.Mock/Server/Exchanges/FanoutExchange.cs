using RabbitMQ.Client.Mock.Server.Data;

namespace RabbitMQ.Client.Mock.Server.Exchanges;
internal class FanoutExchange : RabbitExchange
{
    public FanoutExchange(IRabbitServer server, string name) : base(server, name, ExchangeType.Fanout)
    {
    }

    public override ValueTask PublishMessageAsync(string routingKey, RabbitMessage message)
    {
        throw new NotImplementedException();
    }
}
