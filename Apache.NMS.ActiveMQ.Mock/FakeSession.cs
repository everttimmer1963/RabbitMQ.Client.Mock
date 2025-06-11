using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Apache.NMS.ActiveMQ.Mock;

internal class FakeSession : ISession
{
    private readonly FakeActiveMQServer _server = FakeActiveMQServer.Instance;
    private readonly List<string> _consumerTags = new();
    private string _transactionId;
    public AcknowledgementMode AcknowledgementMode { get; }
    public ConsumerTransformerDelegate ConsumerTransformer { get; set; }
    public ProducerTransformerDelegate ProducerTransformer { get; set; }
    public TimeSpan RequestTimeout { get; set; }
    public bool Transacted => _transactionId != null;
    public event SessionTxEventDelegate TransactionStartedListener { add { } remove { } }
    public event SessionTxEventDelegate TransactionCommittedListener { add { } remove { } }
    public event SessionTxEventDelegate TransactionRolledBackListener { add { } remove { } }

    public FakeSession(AcknowledgementMode mode = AcknowledgementMode.AutoAcknowledge)
    {
        AcknowledgementMode = mode;
    }

    public IMessageProducer CreateProducer() => null; // Implement as needed
    public Task<IMessageProducer> CreateProducerAsync() => Task.FromResult<IMessageProducer>(null);
    public IMessageProducer CreateProducer(IDestination destination) => null;
    public Task<IMessageProducer> CreateProducerAsync(IDestination destination) => Task.FromResult<IMessageProducer>(null);

    public IMessageConsumer CreateConsumer(IDestination destination)
        => CreateConsumer(destination, null, false);
    public Task<IMessageConsumer> CreateConsumerAsync(IDestination destination)
        => Task.FromResult<IMessageConsumer>(CreateConsumer(destination));
    public IMessageConsumer CreateConsumer(IDestination destination, string selector)
        => CreateConsumer(destination, selector, false);
    public Task<IMessageConsumer> CreateConsumerAsync(IDestination destination, string selector)
        => Task.FromResult<IMessageConsumer>(CreateConsumer(destination, selector));
    public IMessageConsumer CreateConsumer(IDestination destination, string selector, bool noLocal)
    {
        // Register consumer with server
        string tag = _server.Subscribe(destination.ToString(),
            msg => string.IsNullOrEmpty(selector) || msg.Body.Contains(selector),
            msg => { /* deliver to consumer */ });
        _consumerTags.Add(tag);
        return null; // Replace with a real FakeConsumer
    }
    public Task<IMessageConsumer> CreateConsumerAsync(IDestination destination, string selector, bool noLocal)
        => Task.FromResult<IMessageConsumer>(CreateConsumer(destination, selector, noLocal));

    // Durable, shared, and browser consumers can be implemented similarly
    public IMessageConsumer CreateDurableConsumer(ITopic topic, string name) => null;
    public Task<IMessageConsumer> CreateDurableConsumerAsync(ITopic topic, string name) => Task.FromResult<IMessageConsumer>(null);
    public IMessageConsumer CreateDurableConsumer(ITopic topic, string name, string selector) => null;
    public Task<IMessageConsumer> CreateDurableConsumerAsync(ITopic topic, string name, string selector) => Task.FromResult<IMessageConsumer>(null);
    public IMessageConsumer CreateDurableConsumer(ITopic topic, string name, string selector, bool noLocal) => null;
    public Task<IMessageConsumer> CreateDurableConsumerAsync(ITopic topic, string name, string selector, bool noLocal) => Task.FromResult<IMessageConsumer>(null);
    public IMessageConsumer CreateSharedConsumer(ITopic topic, string name) => null;
    public Task<IMessageConsumer> CreateSharedConsumerAsync(ITopic topic, string name) => Task.FromResult<IMessageConsumer>(null);
    public IMessageConsumer CreateSharedConsumer(ITopic topic, string name, string selector) => null;
    public Task<IMessageConsumer> CreateSharedConsumerAsync(ITopic topic, string name, string selector) => Task.FromResult<IMessageConsumer>(null);
    public IMessageConsumer CreateSharedDurableConsumer(ITopic topic, string name) => null;
    public Task<IMessageConsumer> CreateSharedDurableConsumerAsync(ITopic topic, string name) => Task.FromResult<IMessageConsumer>(null);
    public IMessageConsumer CreateSharedDurableConsumer(ITopic topic, string name, string selector) => null;
    public Task<IMessageConsumer> CreateSharedDurableConsumerAsync(ITopic topic, string name, string selector) => Task.FromResult<IMessageConsumer>(null);

    public void DeleteDurableConsumer(string name) { }
    public void Unsubscribe(string name) => _server.Unsubscribe(name);
    public Task UnsubscribeAsync(string name) { Unsubscribe(name); return Task.CompletedTask; }
    public IQueueBrowser CreateBrowser(IQueue queue) => null;
    public Task<IQueueBrowser> CreateBrowserAsync(IQueue queue) => Task.FromResult<IQueueBrowser>(null);
    public IQueueBrowser CreateBrowser(IQueue queue, string selector) => null;
    public Task<IQueueBrowser> CreateBrowserAsync(IQueue queue, string selector) => Task.FromResult<IQueueBrowser>(null);
    public IQueue GetQueue(string name) => null;
    public Task<IQueue> GetQueueAsync(string name) => Task.FromResult<IQueue>(null);
    public ITopic GetTopic(string name) => null;
    public Task<ITopic> GetTopicAsync(string name) => Task.FromResult<ITopic>(null);
    public ITemporaryQueue CreateTemporaryQueue() => null;
    public Task<ITemporaryQueue> CreateTemporaryQueueAsync() => Task.FromResult<ITemporaryQueue>(null);
    public ITemporaryTopic CreateTemporaryTopic() => null;
    public Task<ITemporaryTopic> CreateTemporaryTopicAsync() => Task.FromResult<ITemporaryTopic>(null);
    public void DeleteDestination(IDestination destination) { }
    public Task DeleteDestinationAsync(IDestination destination) => Task.CompletedTask;
    public IMessage CreateMessage() => null;
    public Task<IMessage> CreateMessageAsync() => Task.FromResult<IMessage>(null);
    public ITextMessage CreateTextMessage() => null;
    public Task<ITextMessage> CreateTextMessageAsync() => Task.FromResult<ITextMessage>(null);
    public ITextMessage CreateTextMessage(string text) => null;
    public Task<ITextMessage> CreateTextMessageAsync(string text) => Task.FromResult<ITextMessage>(null);
    public IMapMessage CreateMapMessage() => null;
    public Task<IMapMessage> CreateMapMessageAsync() => Task.FromResult<IMapMessage>(null);
    public IObjectMessage CreateObjectMessage(object body) => null;
    public Task<IObjectMessage> CreateObjectMessageAsync(object body) => Task.FromResult<IObjectMessage>(null);
    public IBytesMessage CreateBytesMessage() => null;
    public Task<IBytesMessage> CreateBytesMessageAsync(byte[] body) => Task.FromResult<IBytesMessage>(null);
    public IBytesMessage CreateBytesMessage(byte[] body) => null;
    public Task<IBytesMessage> CreateBytesMessageAsync() => Task.FromResult<IBytesMessage>(null);
    public IStreamMessage CreateStreamMessage() => null;
    public Task<IStreamMessage> CreateStreamMessageAsync() => Task.FromResult<IStreamMessage>(null);
    public void Close() { }
    public Task CloseAsync() => Task.CompletedTask;
    public void Recover() { }
    public Task RecoverAsync() => Task.CompletedTask;
    public void Acknowledge() { }
    public Task AcknowledgeAsync() => Task.CompletedTask;
    public void Commit() { if (_transactionId != null) { _server.CommitTransaction(_transactionId); _transactionId = null; } }
    public Task CommitAsync() { Commit(); return Task.CompletedTask; }
    public void Rollback() { if (_transactionId != null) { _server.RollbackTransaction(_transactionId); _transactionId = null; } }
    public Task RollbackAsync() { Rollback(); return Task.CompletedTask; }
    public void Dispose() { foreach (var tag in _consumerTags) _server.Unsubscribe(tag); }
}
