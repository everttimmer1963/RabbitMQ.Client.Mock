using RabbitMQ.Client.Mock.NetStandard.Server.Exchanges;
using RabbitMQ.Client.Mock.NetStandard.Server.Queues;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Bindings
{
    internal class QueueBinding
    {
        public RabbitExchange Exchange { get; set; }
        public IDictionary<string, object> Arguments { get; set; } = null;
        public IDictionary<string, RabbitQueue> BoundQueues { get; } = new ConcurrentDictionary<string, RabbitQueue>();
    }
}
