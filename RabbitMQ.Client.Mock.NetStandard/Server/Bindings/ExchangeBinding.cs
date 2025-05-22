using RabbitMQ.Client.Mock.NetStandard.Server.Exchanges;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Bindings
{
    internal class ExchangeBinding
    {
        public RabbitExchange Exchange { get; set; }
        public IDictionary<string, object> Arguments { get; set; } = null;
        public IDictionary<string, RabbitExchange> BoundExchanges { get; } = new ConcurrentDictionary<string, RabbitExchange>();
    }
}
