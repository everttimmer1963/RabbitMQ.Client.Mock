namespace RabbitMQ.Client.Mock.Domain;

internal class FanoutExchange : Exchange
{
    public FanoutExchange(string name)
        : base(name, ExchangeType.Fanout)
    {
    }
}
