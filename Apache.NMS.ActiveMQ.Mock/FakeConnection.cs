
namespace Apache.NMS.ActiveMQ.Mock;

internal class FakeConnection : IConnection
{
    public ConsumerTransformerDelegate ConsumerTransformer { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public ProducerTransformerDelegate ProducerTransformer { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public TimeSpan RequestTimeout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public AcknowledgementMode AcknowledgementMode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public string ClientId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public IRedeliveryPolicy RedeliveryPolicy { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public IConnectionMetaData MetaData => throw new NotImplementedException();

    public bool IsStarted => throw new NotImplementedException();

    public event ExceptionListener ExceptionListener;
    public event ConnectionInterruptedListener ConnectionInterruptedListener;
    public event ConnectionResumedListener ConnectionResumedListener;

    public void Close()
    {
        throw new NotImplementedException();
    }

    public Task CloseAsync()
    {
        throw new NotImplementedException();
    }

    public ISession CreateSession()
    {
        throw new NotImplementedException();
    }

    public ISession CreateSession(AcknowledgementMode acknowledgementMode)
    {
        throw new NotImplementedException();
    }

    public Task<ISession> CreateSessionAsync()
    {
        throw new NotImplementedException();
    }

    public Task<ISession> CreateSessionAsync(AcknowledgementMode acknowledgementMode)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void PurgeTempDestinations()
    {
        throw new NotImplementedException();
    }

    public void Start()
    {
        throw new NotImplementedException();
    }

    public Task StartAsync()
    {
        throw new NotImplementedException();
    }

    public void Stop()
    {
        throw new NotImplementedException();
    }

    public Task StopAsync()
    {
        throw new NotImplementedException();
    }
}
