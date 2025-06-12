
using Apache.NMS.Policies;

namespace Apache.NMS.ActiveMQ.Mock;

internal class FakeConnectionFactory : IConnectionFactory
{
    public const string DEFAULT_BROKER_URL = "failover:tcp://localhost:61616";

    public const string ENV_BROKER_URL = "ACTIVEMQ_BROKER_URL";

    private ConnectionFactory _realFactory;

    public Uri BrokerUri { get; set; } = new Uri(Environment.GetEnvironmentVariable(ENV_BROKER_URL) ?? DEFAULT_BROKER_URL);
    public string UserName { get; set; } = "admin";
    public string Password { get; set; } = "admin";
    public string ClientId { get; set; } = string.Empty;
    public string ClientIdPrefix { get; set; } = string.Empty;
    public bool UseCompression { get; set; }
    public bool CopyMessageOnSend { get; set; }

    public IRedeliveryPolicy RedeliveryPolicy { get; set; } = new RedeliveryPolicy();
    public ConsumerTransformerDelegate ConsumerTransformer { get; set; } = null!;
    public ProducerTransformerDelegate ProducerTransformer { get; set; } = null!;

    public IConnection CreateConnection()
    {
        return new FakeConnection
        {
            ConsumerTransformer = ConsumerTransformer,
            ProducerTransformer = ProducerTransformer,
            AcknowledgementMode = AcknowledgementMode.AutoAcknowledge,
            RedeliveryPolicy = RedeliveryPolicy,
        };
    }

    public IConnection CreateConnection(string userName, string password)
    {
        throw new NotImplementedException();
    }

    public Task<IConnection> CreateConnectionAsync()
    {
        throw new NotImplementedException();
    }

    public Task<IConnection> CreateConnectionAsync(string userName, string password)
    {
        throw new NotImplementedException();
    }

    public INMSContext CreateContext()
    {
        throw new NotImplementedException();
    }

    public INMSContext CreateContext(AcknowledgementMode acknowledgementMode)
    {
        throw new NotImplementedException();
    }

    public INMSContext CreateContext(string userName, string password)
    {
        throw new NotImplementedException();
    }

    public INMSContext CreateContext(string userName, string password, AcknowledgementMode acknowledgementMode)
    {
        throw new NotImplementedException();
    }

    public Task<INMSContext> CreateContextAsync()
    {
        throw new NotImplementedException();
    }

    public Task<INMSContext> CreateContextAsync(AcknowledgementMode acknowledgementMode)
    {
        throw new NotImplementedException();
    }

    public Task<INMSContext> CreateContextAsync(string userName, string password)
    {
        throw new NotImplementedException();
    }

    public Task<INMSContext> CreateContextAsync(string userName, string password, AcknowledgementMode acknowledgementMode)
    {
        throw new NotImplementedException();
    }
}
