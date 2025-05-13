namespace RabbitMQ.Client.Mock.NetStandard.Domain
{
    internal class DirectExchange : Exchange
    {
        public DirectExchange(string name, int connectionNumber)
            : base(name, ExchangeType.Direct, connectionNumber)
        {
        }
    }
}
