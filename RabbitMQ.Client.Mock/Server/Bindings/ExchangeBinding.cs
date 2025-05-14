using RabbitMQ.Client.Mock.Server.Exchanges;
using System.Collections.Concurrent;

namespace RabbitMQ.Client.Mock.Server.Bindings;

internal class ExchangeBinding
{
    public IDictionary<string, object?>? Arguments { get; set; } = null;
    public IDictionary<string, RabbitExchange> BoundExchanges { get; } = new ConcurrentDictionary<string, RabbitExchange>();
}
