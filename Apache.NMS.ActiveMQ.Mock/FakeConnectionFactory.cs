
using Apache.NMS.Policies;

namespace Apache.NMS.ActiveMQ.Mock;

internal class FakeConnectionFactory : IConnectionFactory
{
    NMSConnectionFactory factory;

    public Uri BrokerUri { get; set; }
    public AcknowledgementMode AcknowledgementMode { get; set; }
    public IRedeliveryPolicy RedeliveryPolicy { get; set; } = new RedeliveryPolicy();
    public ConsumerTransformerDelegate ConsumerTransformer { get; set; }
    public ProducerTransformerDelegate ProducerTransformer { get; set; }

    public IConnection CreateConnection()
    {
        return new FakeConnection
        {
            BrokerUri = this.BrokerUri,
            RedeliveryPolicy = this.RedeliveryPolicy,
            ConsumerTransformer = this.ConsumerTransformer,
            ProducerTransformer = this.ProducerTransformer,
            AcknowledgementMode = this.AcknowledgementMode
        };
    }

    public IConnection CreateConnection(string userName, string password)
    {
        return new FakeConnection
        {
            BrokerUri = this.BrokerUri,
            UserName = userName,
            Password = password,
            RedeliveryPolicy = this.RedeliveryPolicy,
            ConsumerTransformer = this.ConsumerTransformer,
            ProducerTransformer = this.ProducerTransformer,
            AcknowledgementMode = this.AcknowledgementMode
        };
    }

    public Task<IConnection> CreateConnectionAsync()
    {
        return Task.FromResult(CreateConnection());
    }

    public Task<IConnection> CreateConnectionAsync(string userName, string password)
    {
        return Task.FromResult(CreateConnection(userName, password));
    }

    public INMSContext CreateContext()
    {
        NmsContext ctx;
        return new FakeNMSContext
        {
            BrokerUri = this.BrokerUri,
            RedeliveryPolicy = this.RedeliveryPolicy,
            ConsumerTransformer = this.ConsumerTransformer,
            ProducerTransformer = this.ProducerTransformer,
            AcknowledgementMode = this.AcknowledgementMode
        };
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
