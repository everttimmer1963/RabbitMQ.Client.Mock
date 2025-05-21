using System.Collections.Generic;

namespace RabbitMQ.Client.Mock.NetStandard.Domain
{
    internal class QueueBinding
    {
        public IDictionary<string, object?>? Arguments { get; set; } = null;
        public IList<RabbitQueue> BoundQueues { get; } = new List<RabbitQueue>();
    }
}
