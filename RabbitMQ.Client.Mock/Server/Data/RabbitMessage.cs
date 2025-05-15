namespace RabbitMQ.Client.Mock.Server.Data;
internal class RabbitMessage
{
    public required string Exchange { get; set; }
    public required string RoutingKey { get; set; }
    public string Queue { get; set; } = string.Empty;
    public required bool Mandatory { get; set; }
    public required bool Immediate { get; set; } = false;
    public required IReadOnlyBasicProperties BasicProperties { get; set; }
    public required byte[] Body { get; set; }
    public required ulong DeliveryTag { get; set; }

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
