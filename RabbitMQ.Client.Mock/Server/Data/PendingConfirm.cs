namespace RabbitMQ.Client.Mock.Server.Data;

internal class PendingConfirm(int channelNumber, ulong deliveryTag, RabbitMessage message, TimeSpan timeOut)
{
    private DateTime _createdAt = DateTime.UtcNow;

    public int ChannelNumber { get; } = channelNumber;
    public ulong DeliveryTag { get; } = deliveryTag;
    public RabbitMessage Message { get; } = message;
    public TimeSpan TimeOut { get; } = timeOut;
    public bool HasExpired => DateTime.UtcNow - _createdAt > TimeOut;
}
