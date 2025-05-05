namespace RabbitMQ.Client.Mock;

internal class FakeConnectionOptions
{
    public ushort ChannelMax { get; set; } = FakeConnectionFactory.DefaultChannelMax;
    public IDictionary<string, object?> ClientProperties { get; set; } = FakeConnectionFactory.DefaultClientProperties;
    public uint FrameMax { get; set; } = FakeConnectionFactory.DefaultFrameMax;
    public TimeSpan Heartbeat { get; set; } = FakeConnectionFactory.DefaultHeartbeat;
    public string? ClientProvidedName { get; set; }
}
