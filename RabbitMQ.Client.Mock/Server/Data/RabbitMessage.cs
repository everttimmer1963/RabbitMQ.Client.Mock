namespace RabbitMQ.Client.Mock.Server.Data;
internal class RabbitMessage
{
    public string Queue { get; set; } = string.Empty;
    public bool Mandatory { get; set; }
    public bool Immediate { get; set; }
    public required IReadOnlyBasicProperties BasicProperties { get; set; }
    public byte[] Body { get; set; } = [];

    #region Routing Properties
    public bool Redelivered { get; set; }
    public string Exchange { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public ulong DeliveryTag { get; set; }
    public string ConsumerTag { get; set; } = string.Empty;
    #endregion
}
