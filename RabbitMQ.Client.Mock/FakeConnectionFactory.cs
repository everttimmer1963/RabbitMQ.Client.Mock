using System.Reflection;
using System.Security.Authentication;
using System.Text;

namespace RabbitMQ.Client.Mock;

internal class FakeConnectionFactory : IConnectionFactory
{
    public const ushort DefaultChannelMax = 2047;
    public static readonly TimeSpan DefaultConnectionTimeout = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan DefaultContinuationTimeout = TimeSpan.FromSeconds(20);
    public static readonly TimeSpan DefaultHandshakeContinuationTimeout = TimeSpan.FromSeconds(10);
    public const ushort DefaultConsumerDispatchConcurrency = 1;
    public const uint DefaultFrameMax = 0;
    public const uint DefaultMaxInboundMessageSize = 1_048_576 * 64; // 1 MB
    public static readonly TimeSpan DefaultHeartbeat = TimeSpan.FromSeconds(60);
    public const string DefaultUser = "guest";
    public const string DefaultPass = "guest";
    public const string DefaultVHost = "/";
    public static SslProtocols DefaultAmqpUriSslProtocols = SslProtocols.None;
    public SslProtocols AmqpUriSslProtocols = DefaultAmqpUriSslProtocols;
    public static readonly IEnumerable<IAuthMechanismFactory> DefaultAuthMechanisms = new[] { new PlainMechanismFactory() };
    public IEnumerable<IAuthMechanismFactory> AuthMechanisms = DefaultAuthMechanisms;
    public static System.Net.Sockets.AddressFamily DefaultAddressFamily;
    internal static readonly Dictionary<string, object?> DefaultClientProperties = new Dictionary<string, object?>(7)
    {
        ["product"] = Encoding.UTF8.GetBytes("RabbitMQ"),
        ["version"] = Encoding.UTF8.GetBytes(typeof(ConnectionFactory).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion),
        ["platform"] = Encoding.UTF8.GetBytes(".NET"),
        ["copyright"] = Encoding.UTF8.GetBytes("Copyright (c) 2007-2023 Broadcom."),
        ["information"] = Encoding.UTF8.GetBytes("Licensed under the MPL. See https://www.rabbitmq.com/"),
        ["capabilities"] = new Dictionary<string, bool>(6)
        {
            ["publisher_confirms"] = true,
            ["exchange_exchange_bindings"] = true,
            ["basic.nack"] = true,
            ["consumer_cancel_notify"] = true,
            ["connection.blocked"] = true,
            ["authentication_failure_close"] = true
        },
        ["connection_name"] = null
    };

    private FakeConnection? _connection;

    public IDictionary<string, object?> ClientProperties { get; set; } = DefaultClientProperties;
    public string Password { get; set; } = DefaultPass;
    public ushort RequestedChannelMax { get; set; } = DefaultChannelMax;
    public uint RequestedFrameMax { get; set; } = DefaultFrameMax;
    public TimeSpan RequestedHeartbeat { get; set; } = DefaultHeartbeat;
    public string UserName { get; set; } = DefaultUser;
    public string VirtualHost { get; set; } = DefaultVHost;
    public ICredentialsProvider? CredentialsProvider { get; set; }
    public Uri Uri { get; set; } = new Uri("amqp://localhost:5672");
    public string? ClientProvidedName { get; set; }
    public TimeSpan HandshakeContinuationTimeout { get; set; } = DefaultHandshakeContinuationTimeout;
    public TimeSpan ContinuationTimeout { get; set; } = DefaultContinuationTimeout;
    public ushort ConsumerDispatchConcurrency { get; set; } = DefaultConsumerDispatchConcurrency;

    public IAuthMechanismFactory? AuthMechanismFactory(IEnumerable<string> mechanismNames)
    {
        throw new NotImplementedException("Since this is a mocking client. Authentication isn't implemented.");
    }

    public async Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var options = new FakeConnectionOptions
        {
            ChannelMax = RequestedChannelMax,
            ClientProperties = ClientProperties,
            FrameMax = RequestedFrameMax,
            Heartbeat = RequestedHeartbeat,
            ClientProvidedName = ClientProvidedName
        };
        _connection = new FakeConnection(options);
        return await Task.FromResult<IConnection>(_connection);
    }

    public async Task<IConnection> CreateConnectionAsync(string clientProvidedName, CancellationToken cancellationToken = default)
    {
        return await CreateConnectionAsync(cancellationToken);
    }

    public async Task<IConnection> CreateConnectionAsync(IEnumerable<string> hostnames, CancellationToken cancellationToken = default)
    {
        return await CreateConnectionAsync(cancellationToken);
    }

    public async Task<IConnection> CreateConnectionAsync(IEnumerable<string> hostnames, string clientProvidedName, CancellationToken cancellationToken = default)
    {
        return await CreateConnectionAsync(cancellationToken);
    }

    public async Task<IConnection> CreateConnectionAsync(IEnumerable<AmqpTcpEndpoint> endpoints, CancellationToken cancellationToken = default)
    {
        return await CreateConnectionAsync(cancellationToken);
    }

    public async Task<IConnection> CreateConnectionAsync(IEnumerable<AmqpTcpEndpoint> endpoints, string clientProvidedName, CancellationToken cancellationToken = default)
    {
        return await CreateConnectionAsync(cancellationToken);
    }
}
