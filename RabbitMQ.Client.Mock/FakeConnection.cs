using RabbitMQ.Client.Events;

namespace RabbitMQ.Client.Mock;

internal class FakeConnection : IConnection, IDisposable, IAsyncDisposable
{
    private static int _lastConnectionNumber;

    private bool _disposed;
    private int _connectionNumber;
    private readonly FakeConnectionOptions _options;
    private readonly List<IChannel> _channels = new();

    public FakeConnection(FakeConnectionOptions options)
    {
        _options = options;
        _connectionNumber = GetNextConnectionNumber();
    }

    private RabbitMQServer Server => RabbitMQServer.GetInstance();

    public ushort ChannelMax => _options.ChannelMax;

    public IDictionary<string, object?> ClientProperties => _options.ClientProperties;

    public ShutdownEventArgs? CloseReason { get; private set; }

    public AmqpTcpEndpoint Endpoint { get; private set; }

    public uint FrameMax => _options.FrameMax;

    public TimeSpan Heartbeat => _options.Heartbeat;

    public bool IsOpen { get; private set; }

    public IProtocol Protocol { get; private set; }

    public IDictionary<string, object?>? ServerProperties { get; private set; }

    public IEnumerable<ShutdownReportEntry> ShutdownReport => throw new NotImplementedException();

    public string? ClientProvidedName => _options.ClientProvidedName;

    public int LocalPort { get; set; }

    public int RemotePort { get; set; }

    public event AsyncEventHandler<CallbackExceptionEventArgs> CallbackExceptionAsync
    { 
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }
    public event AsyncEventHandler<ShutdownEventArgs> ConnectionShutdownAsync
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }
    public event AsyncEventHandler<AsyncEventArgs> RecoverySucceededAsync;
    public event AsyncEventHandler<ConnectionRecoveryErrorEventArgs> ConnectionRecoveryErrorAsync;
    public event AsyncEventHandler<ConsumerTagChangedAfterRecoveryEventArgs> ConsumerTagChangeAfterRecoveryAsync;
    public event AsyncEventHandler<QueueNameChangedAfterRecoveryEventArgs> QueueNameChangedAfterRecoveryAsync;
    public event AsyncEventHandler<RecoveringConsumerEventArgs> RecoveringConsumerAsync;
    public event AsyncEventHandler<ConnectionBlockedEventArgs> ConnectionBlockedAsync;
    public event AsyncEventHandler<AsyncEventArgs> ConnectionUnblockedAsync;

    public Task CloseAsync(ushort reasonCode, string reasonText, TimeSpan timeout, bool abort, CancellationToken cancellationToken = default)
    {
        IsOpen = false;
        CloseReason = new ShutdownEventArgs(ShutdownInitiator.Library, reasonCode, reasonText);
        _channels.ForEach(async ch => await ch.CloseAsync(CloseReason, abort, cancellationToken));
        return Task.CompletedTask;
    }

    public Task<IChannel> CreateChannelAsync(CreateChannelOptions? options = null, CancellationToken cancellationToken = default)
    {
        var channel = new FakeChannel(options, _connectionNumber);
        _channels.Add(channel);
        return Task.FromResult<IChannel>(channel);
    }

    public Task UpdateSecretAsync(string newSecret, string reason, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private static int GetNextConnectionNumber()
    {
        return Interlocked.Increment(ref _lastConnectionNumber);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        Task.Run(async () => await DisposeAsync());
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _channels.ForEach(async ch => await ch.DisposeAsync());
        _channels.Clear();
        await Server.HandleDisconnectAsync(_connectionNumber);
    }
}
