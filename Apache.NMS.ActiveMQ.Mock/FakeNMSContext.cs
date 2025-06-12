using System;
using System.Threading.Tasks;
using Apache.NMS.ActiveMQ.Mock.Server;

namespace Apache.NMS.ActiveMQ.Mock
{
    public class FakeNMSContext : INMSContext
    {
        private readonly FakeActiveMQServer _server = FakeActiveMQServer.Instance;
        public void Dispose() { }
        public INMSContext CreateContext(AcknowledgementMode mode) => this;
        public INMSProducer CreateProducer()
        {
            var producer = new FakeNMSProducer();
            return producer;
        }
        public Task<INMSProducer> CreateProducerAsync() => Task.FromResult<INMSProducer>(new FakeNMSProducer());
        public INMSConsumer CreateConsumer(IDestination destination)
        {
            var queueName = destination.ToString();
            return new FakeNMSConsumer(queueName, null, new FakeSession());
        }
        public INMSConsumer CreateConsumer(IDestination destination, string selector)
        {
            var queueName = destination.ToString();
            return new FakeNMSConsumer(queueName, msg => string.IsNullOrEmpty(selector) || msg.Body.Contains(selector), new FakeSession());
        }
        public INMSConsumer CreateConsumer(IDestination destination, string selector, bool noLocal)
        {
            var queueName = destination.ToString();
            return new FakeNMSConsumer(queueName, msg => string.IsNullOrEmpty(selector) || msg.Body.Contains(selector), new FakeSession());
        }
        public INMSConsumer CreateDurableConsumer(ITopic topic, string name) => new FakeNMSConsumer(topic.ToString(), null, null, durable: true, durableName: name);
        public INMSConsumer CreateDurableConsumer(ITopic topic, string name, string selector) => new FakeNMSConsumer(topic.ToString(), msg => string.IsNullOrEmpty(selector) || msg.Body.Contains(selector), null, durable: true, durableName: name);
        public INMSConsumer CreateDurableConsumer(ITopic topic, string name, string selector, bool noLocal) => new FakeNMSConsumer(topic.ToString(), msg => string.IsNullOrEmpty(selector) || msg.Body.Contains(selector), null, durable: true, durableName: name);
        public INMSConsumer CreateSharedConsumer(ITopic topic, string name) => new FakeNMSConsumer(topic.ToString(), null, null, shared: true, sharedName: name);
        public INMSConsumer CreateSharedConsumer(ITopic topic, string name, string selector) => new FakeNMSConsumer(topic.ToString(), msg => string.IsNullOrEmpty(selector) || msg.Body.Contains(selector), null, shared: true, sharedName: name);
        public INMSConsumer CreateSharedDurableConsumer(ITopic topic, string name) => new FakeNMSConsumer(topic.ToString(), null, null, durable: true, durableName: name, shared: true, sharedName: name);
        public INMSConsumer CreateSharedDurableConsumer(ITopic topic, string name, string selector) => new FakeNMSConsumer(topic.ToString(), msg => string.IsNullOrEmpty(selector) || msg.Body.Contains(selector), null, durable: true, durableName: name, shared: true, sharedName: name);
        public Task<INMSConsumer> CreateConsumerAsync(IDestination destination) => Task.FromResult<INMSConsumer>(null);
        public Task<INMSConsumer> CreateConsumerAsync(IDestination destination, string selector) => Task.FromResult<INMSConsumer>(null);
        public Task<INMSConsumer> CreateConsumerAsync(IDestination destination, string selector, bool noLocal) => Task.FromResult<INMSConsumer>(null);
        public Task<INMSConsumer> CreateDurableConsumerAsync(ITopic topic, string name) => Task.FromResult<INMSConsumer>(null);
        public Task<INMSConsumer> CreateDurableConsumerAsync(ITopic topic, string name, string selector) => Task.FromResult<INMSConsumer>(null);
        public Task<INMSConsumer> CreateDurableConsumerAsync(ITopic topic, string name, string selector, bool noLocal) => Task.FromResult<INMSConsumer>(null);
        public Task<INMSConsumer> CreateSharedConsumerAsync(ITopic topic, string name) => Task.FromResult<INMSConsumer>(null);
        public Task<INMSConsumer> CreateSharedConsumerAsync(ITopic topic, string name, string selector) => Task.FromResult<INMSConsumer>(null);
        public Task<INMSConsumer> CreateSharedDurableConsumerAsync(ITopic topic, string name) => Task.FromResult<INMSConsumer>(null);
        public Task<INMSConsumer> CreateSharedDurableConsumerAsync(ITopic topic, string name, string selector) => Task.FromResult<INMSConsumer>(null);
        public void Unsubscribe(string name) { }
        public Task UnsubscribeAsync(string name) => Task.CompletedTask;
        public IQueueBrowser CreateBrowser(IQueue queue) => new FakeQueueBrowser(queue, null);
        public Task<IQueueBrowser> CreateBrowserAsync(IQueue queue) => Task.FromResult<IQueueBrowser>(new FakeQueueBrowser(queue, null));
        public IQueueBrowser CreateBrowser(IQueue queue, string selector) => new FakeQueueBrowser(queue, selector);
        public Task<IQueueBrowser> CreateBrowserAsync(IQueue queue, string selector) => Task.FromResult<IQueueBrowser>(new FakeQueueBrowser(queue, selector));
        public IQueue GetQueue(string name)
        {
            _server.CreateQueue(name);
            return new Server.FakeQueue(name);
        }
        public Task<IQueue> GetQueueAsync(string name)
        {
            return Task.FromResult<IQueue>(GetQueue(name));
        }
        public ITopic GetTopic(string name)
        {
            _server.CreateTopic(name);
            return new Server.FakeTopic(name);
        }
        public Task<ITopic> GetTopicAsync(string name)
        {
            return Task.FromResult<ITopic>(GetTopic(name));
        }
        public ITemporaryQueue CreateTemporaryQueue() => new Server.FakeTemporaryQueue(Guid.NewGuid().ToString());
        public Task<ITemporaryQueue> CreateTemporaryQueueAsync() => Task.FromResult<ITemporaryQueue>(CreateTemporaryQueue());
        public ITemporaryTopic CreateTemporaryTopic() => new Server.FakeTemporaryTopic(Guid.NewGuid().ToString());
        public Task<ITemporaryTopic> CreateTemporaryTopicAsync() => Task.FromResult<ITemporaryTopic>(CreateTemporaryTopic());
        public void DeleteDestination(IDestination destination) { }
        public Task DeleteDestinationAsync(IDestination destination) => Task.CompletedTask;
        public IMessage CreateMessage() => null;
        public Task<IMessage> CreateMessageAsync() => Task.FromResult<IMessage>(null);
        public ITextMessage CreateTextMessage() => null;
        public Task<ITextMessage> CreateTextMessageAsync() => Task.FromResult<ITextMessage>(null);
        public ITextMessage CreateTextMessage(string text) => new FakeTextMessage { Text = text };
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
        public void Commit() { }
        public Task CommitAsync() => Task.CompletedTask;
        public void Rollback() { }
        public Task RollbackAsync() => Task.CompletedTask;
        public void PurgeTempDestinations() { }
        public ConsumerTransformerDelegate ConsumerTransformer { get; set; }
        public ProducerTransformerDelegate ProducerTransformer { get; set; }
        public TimeSpan RequestTimeout { get; set; }
        public bool Transacted => false;
        public AcknowledgementMode AcknowledgementMode { get; set; }
        public string ClientId { get; set; }
        public bool AutoStart { get; set; }
        public event SessionTxEventDelegate TransactionStartedListener { add { } remove { } }
        public event SessionTxEventDelegate TransactionCommittedListener { add { } remove { } }
        public event SessionTxEventDelegate TransactionRolledBackListener { add { } remove { } }
        public event ExceptionListener ExceptionListener { add { } remove { } }
        public event ConnectionInterruptedListener ConnectionInterruptedListener { add { } remove { } }
        public event ConnectionResumedListener ConnectionResumedListener { add { } remove { } }
        public void Start() { }
        public Task StartAsync() => Task.CompletedTask;
        public bool IsStarted => true;
        public void Stop() { }
        public Task StopAsync() => Task.CompletedTask;
    }
}
