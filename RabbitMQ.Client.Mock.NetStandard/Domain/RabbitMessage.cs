namespace RabbitMQ.Client.Mock.NetStandard.Domain
{
    internal class RabbitMessage
    {
        public string Exchange { get; set; } = string.Empty;
        public string RoutingKey { get; set; } = string.Empty;
        public string Queue { get; set; } = string.Empty;
        public bool Mandatory { get; set; }
        public bool Immediate { get; set; } = false;
        public IReadOnlyBasicProperties BasicProperties { get; set; }
        public byte[] Body { get; set; }
        public ulong DeliveryTag { get; set; }

        public RabbitMessage Copy()
        {
            return new RabbitMessage
            {
                Exchange = this.Exchange,
                RoutingKey = this.RoutingKey,
                Queue = this.Queue,
                Mandatory = this.Mandatory,
                Immediate = this.Immediate,
                BasicProperties = this.BasicProperties,
                Body = this.Body,
                DeliveryTag = this.DeliveryTag
            };
        }
    }
}