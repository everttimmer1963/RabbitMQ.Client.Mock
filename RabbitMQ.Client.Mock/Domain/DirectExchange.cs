

namespace RabbitMQ.Client.Mock.Domain;

internal class DirectExchange : Exchange
{
    public DirectExchange(string name)
        : base(name, ExchangeType.Direct)
    { 
    }
}
