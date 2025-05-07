namespace RabbitMQ.Client.Mock.Domain;

internal class ExchangeBinding
{
    public IDictionary<string, object?>? Arguments { get; set; } = null;
    public IList<Exchange> BoundExchanges { get; } = new List<Exchange>();
}
