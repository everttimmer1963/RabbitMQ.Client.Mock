using RabbitMQ.Client.Mock.NetStandard.Server.Queues;
using System.Collections.Generic;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Bindings
{
    internal class ConsumerBinding
    {
        public ConsumerBinding(RabbitQueue queue, IAsyncBasicConsumer consumer)
        {
            Queue = queue;
            Consumer = consumer;
        }
        public RabbitQueue Queue { get; }
        public int ChannelNumber { get; set; }
        public bool AutoAcknowledge { get; set; }
        public bool NoLocal { get; set; }
        public bool Exclusive { get; set; }
        public IDictionary<string, object> Arguments { get; set; }
        public IAsyncBasicConsumer Consumer { get; }
    }
}
