
namespace Apache.NMS.ActiveMQ.Mock;
internal class FakeNMSContext : INMSContext
{
    private readonly AcknowledgementMode _acknowledgementNode;
    public ConsumerTransformerDelegate ConsumerTransformer { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public ProducerTransformerDelegate ProducerTransformer { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public TimeSpan RequestTimeout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public bool Transacted => throw new NotImplementedException();

    public AcknowledgementMode AcknowledgementMode => _acknowledgementNode;

    public string ClientId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public bool AutoStart { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public bool IsStarted => throw new NotImplementedException();

    public event SessionTxEventDelegate TransactionStartedListener;
    public event SessionTxEventDelegate TransactionCommittedListener;
    public event SessionTxEventDelegate TransactionRolledBackListener;
    public event ExceptionListener ExceptionListener;
    public event ConnectionInterruptedListener ConnectionInterruptedListener;
    public event ConnectionResumedListener ConnectionResumedListener;

    public FakeNMSContext(AcknowledgementMode acknowledgementMode)
    { 
        _acknowledgementNode = acknowledgementMode;
        // Initialize other properties if needed
    }

    public void Acknowledge()
    {
        throw new NotImplementedException();
    }

    public Task AcknowledgeAsync()
    {
        throw new NotImplementedException();
    }

    public void Close()
    {
        throw new NotImplementedException();
    }

    public Task CloseAsync()
    {
        throw new NotImplementedException();
    }

    public void Commit()
    {
        throw new NotImplementedException();
    }

    public Task CommitAsync()
    {
        throw new NotImplementedException();
    }

    public IQueueBrowser CreateBrowser(IQueue queue)
    {
        throw new NotImplementedException();
    }

    public IQueueBrowser CreateBrowser(IQueue queue, string selector)
    {
        throw new NotImplementedException();
    }

    public Task<IQueueBrowser> CreateBrowserAsync(IQueue queue)
    {
        throw new NotImplementedException();
    }

    public Task<IQueueBrowser> CreateBrowserAsync(IQueue queue, string selector)
    {
        throw new NotImplementedException();
    }

    public IBytesMessage CreateBytesMessage()
    {
        throw new NotImplementedException();
    }

    public IBytesMessage CreateBytesMessage(byte[] body)
    {
        throw new NotImplementedException();
    }

    public Task<IBytesMessage> CreateBytesMessageAsync()
    {
        throw new NotImplementedException();
    }

    public Task<IBytesMessage> CreateBytesMessageAsync(byte[] body)
    {
        throw new NotImplementedException();
    }

    public INMSConsumer CreateConsumer(IDestination destination)
    {
        throw new NotImplementedException();
    }

    public INMSConsumer CreateConsumer(IDestination destination, string selector)
    {
        throw new NotImplementedException();
    }

    public INMSConsumer CreateConsumer(IDestination destination, string selector, bool noLocal)
    {
        throw new NotImplementedException();
    }

    public Task<INMSConsumer> CreateConsumerAsync(IDestination destination)
    {
        throw new NotImplementedException();
    }

    public Task<INMSConsumer> CreateConsumerAsync(IDestination destination, string selector)
    {
        throw new NotImplementedException();
    }

    public Task<INMSConsumer> CreateConsumerAsync(IDestination destination, string selector, bool noLocal)
    {
        throw new NotImplementedException();
    }

    public INMSContext CreateContext(AcknowledgementMode acknowledgementMode)
    {
        throw new NotImplementedException();
    }

    public INMSConsumer CreateDurableConsumer(ITopic destination, string subscriptionName)
    {
        throw new NotImplementedException();
    }

    public INMSConsumer CreateDurableConsumer(ITopic destination, string subscriptionName, string selector)
    {
        throw new NotImplementedException();
    }

    public INMSConsumer CreateDurableConsumer(ITopic destination, string subscriptionName, string selector, bool noLocal)
    {
        throw new NotImplementedException();
    }

    public Task<INMSConsumer> CreateDurableConsumerAsync(ITopic destination, string subscriptionName)
    {
        throw new NotImplementedException();
    }

    public Task<INMSConsumer> CreateDurableConsumerAsync(ITopic destination, string subscriptionName, string selector)
    {
        throw new NotImplementedException();
    }

    public Task<INMSConsumer> CreateDurableConsumerAsync(ITopic destination, string subscriptionName, string selector, bool noLocal)
    {
        throw new NotImplementedException();
    }

    public IMapMessage CreateMapMessage()
    {
        throw new NotImplementedException();
    }

    public Task<IMapMessage> CreateMapMessageAsync()
    {
        throw new NotImplementedException();
    }

    public IMessage CreateMessage()
    {
        throw new NotImplementedException();
    }

    public Task<IMessage> CreateMessageAsync()
    {
        throw new NotImplementedException();
    }

    public IObjectMessage CreateObjectMessage(object body)
    {
        throw new NotImplementedException();
    }

    public Task<IObjectMessage> CreateObjectMessageAsync(object body)
    {
        throw new NotImplementedException();
    }

    public INMSProducer CreateProducer()
    {
        throw new NotImplementedException();
    }

    public Task<INMSProducer> CreateProducerAsync()
    {
        throw new NotImplementedException();
    }

    public INMSConsumer CreateSharedConsumer(ITopic destination, string subscriptionName)
    {
        throw new NotImplementedException();
    }

    public INMSConsumer CreateSharedConsumer(ITopic destination, string subscriptionName, string selector)
    {
        throw new NotImplementedException();
    }

    public Task<INMSConsumer> CreateSharedConsumerAsync(ITopic destination, string subscriptionName)
    {
        throw new NotImplementedException();
    }

    public Task<INMSConsumer> CreateSharedConsumerAsync(ITopic destination, string subscriptionName, string selector)
    {
        throw new NotImplementedException();
    }

    public INMSConsumer CreateSharedDurableConsumer(ITopic destination, string subscriptionName)
    {
        throw new NotImplementedException();
    }

    public INMSConsumer CreateSharedDurableConsumer(ITopic destination, string subscriptionName, string selector)
    {
        throw new NotImplementedException();
    }

    public Task<INMSConsumer> CreateSharedDurableConsumerAsync(ITopic destination, string subscriptionName)
    {
        throw new NotImplementedException();
    }

    public Task<INMSConsumer> CreateSharedDurableConsumerAsync(ITopic destination, string subscriptionName, string selector)
    {
        throw new NotImplementedException();
    }

    public IStreamMessage CreateStreamMessage()
    {
        throw new NotImplementedException();
    }

    public Task<IStreamMessage> CreateStreamMessageAsync()
    {
        throw new NotImplementedException();
    }

    public ITemporaryQueue CreateTemporaryQueue()
    {
        throw new NotImplementedException();
    }

    public Task<ITemporaryQueue> CreateTemporaryQueueAsync()
    {
        throw new NotImplementedException();
    }

    public ITemporaryTopic CreateTemporaryTopic()
    {
        throw new NotImplementedException();
    }

    public Task<ITemporaryTopic> CreateTemporaryTopicAsync()
    {
        throw new NotImplementedException();
    }

    public ITextMessage CreateTextMessage()
    {
        throw new NotImplementedException();
    }

    public ITextMessage CreateTextMessage(string text)
    {
        throw new NotImplementedException();
    }

    public Task<ITextMessage> CreateTextMessageAsync()
    {
        throw new NotImplementedException();
    }

    public Task<ITextMessage> CreateTextMessageAsync(string text)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public IQueue GetQueue(string name)
    {
        throw new NotImplementedException();
    }

    public Task<IQueue> GetQueueAsync(string name)
    {
        throw new NotImplementedException();
    }

    public ITopic GetTopic(string name)
    {
        throw new NotImplementedException();
    }

    public Task<ITopic> GetTopicAsync(string name)
    {
        throw new NotImplementedException();
    }

    public void PurgeTempDestinations()
    {
        throw new NotImplementedException();
    }

    public void Recover()
    {
        throw new NotImplementedException();
    }

    public Task RecoverAsync()
    {
        throw new NotImplementedException();
    }

    public void Rollback()
    {
        throw new NotImplementedException();
    }

    public Task RollbackAsync()
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

    public void Unsubscribe(string name)
    {
        throw new NotImplementedException();
    }

    public Task UnsubscribeAsync(string name)
    {
        throw new NotImplementedException();
    }
}
