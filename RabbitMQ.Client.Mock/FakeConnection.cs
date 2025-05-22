using RabbitMQ.Client.Events;
using RabbitMQ.Client.Mock.Server;
using System.Diagnostics.CodeAnalysis;

namespace RabbitMQ.Client.Mock;

internal class FakeConnection : IConnection, IDisposable, IAsyncDisposable
{
    public const int DefaultRemotePort = 5672;

    private static int _lastConnectionNumber;

    private bool _disposed;
    private int _connectionNumber;
    private readonly FakeConnectionOptions _options;
    private readonly List<IChannel> _channels = new();
    private IRabbitServer _server;

    public FakeConnection(FakeConnectionOptions options)
    {
        _options = options;
        _server = new RabbitServer();
        _connectionNumber = _server.RegisterConnection(this);
        IsOpen = true;
    }

    public ushort ChannelMax => _options.ChannelMax;

    public IDictionary<string, object?> ClientProperties => _options.ClientProperties;

    public ShutdownEventArgs? CloseReason { get; private set; }

    public AmqpTcpEndpoint Endpoint { get; private set; }

    public uint FrameMax => _options.FrameMax;

    public TimeSpan Heartbeat => _options.Heartbeat;

    public bool IsOpen { get; private set; }

    public IProtocol Protocol { get; private set; }

    public IDictionary<string, object?>? ServerProperties { get; private set; }

    public IEnumerable<ShutdownReportEntry> ShutdownReport { get; private set; } = Enumerable.Empty<ShutdownReportEntry>();

    public string? ClientProvidedName => _options.ClientProvidedName;

    public int LocalPort { get; set; }

    public int RemotePort { get; set; } = DefaultRemotePort;

    [ExcludeFromCodeCoverage]
    public event AsyncEventHandler<CallbackExceptionEventArgs> CallbackExceptionAsync
    { 
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }
    [ExcludeFromCodeCoverage]
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
        var channel = new FakeChannel(_server, options, _connectionNumber);
        _channels.Add(channel);
        return Task.FromResult<IChannel>(channel);
    }

    [ExcludeFromCodeCoverage]
    public Task UpdateSecretAsync(string newSecret, string reason, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) { return; }
        _disposed = true;

        _channels.Clear();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
