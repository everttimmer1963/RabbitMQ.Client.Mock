

namespace RabbitMQ.Client.Mock.Domain;

internal class DirectExchange : Exchange
{
    public DirectExchange(string name, int connectionNumber)
        : base(name, ExchangeType.Direct, connectionNumber)
    { 
    }
}
