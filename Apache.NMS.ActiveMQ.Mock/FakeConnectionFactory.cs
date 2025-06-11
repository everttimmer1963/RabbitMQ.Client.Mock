namespace Apache.NMS.ActiveMQ.Mock;

internal class FakeConnectionFactory : IConnectionFactory
{
    public Uri BrokerUri { get; set; } = new Uri("mock://localhost");
    public IRedeliveryPolicy RedeliveryPolicy { get; set; }
    public ConsumerTransformerDelegate ConsumerTransformer { get; set; }
    public ProducerTransformerDelegate ProducerTransformer { get; set; }

    public IConnection CreateConnection() => new FakeConnection();
    public IConnection CreateConnection(string userName, string password) => new FakeConnection();
    public Task<IConnection> CreateConnectionAsync() => Task.FromResult<IConnection>(new FakeConnection());
    public Task<IConnection> CreateConnectionAsync(string userName, string password) => Task.FromResult<IConnection>(new FakeConnection());
    public INMSContext CreateContext() => new FakeNMSContext();
    public INMSContext CreateContext(AcknowledgementMode acknowledgementMode) => new FakeNMSContext();
    public INMSContext CreateContext(string userName, string password) => new FakeNMSContext();
    public INMSContext CreateContext(string userName, string password, AcknowledgementMode acknowledgementMode) => new FakeNMSContext();
    public Task<INMSContext> CreateContextAsync() => Task.FromResult<INMSContext>(new FakeNMSContext());
    public Task<INMSContext> CreateContextAsync(AcknowledgementMode acknowledgementMode) => Task.FromResult<INMSContext>(new FakeNMSContext());
    public Task<INMSContext> CreateContextAsync(string userName, string password) => Task.FromResult<INMSContext>(new FakeNMSContext());
    public Task<INMSContext> CreateContextAsync(string userName, string password, AcknowledgementMode acknowledgementMode) => Task.FromResult<INMSContext>(new FakeNMSContext());
}
