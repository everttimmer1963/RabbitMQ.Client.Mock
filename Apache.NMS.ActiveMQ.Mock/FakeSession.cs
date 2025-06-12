using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apache.NMS.ActiveMQ.Mock.Server;

namespace Apache.NMS.ActiveMQ.Mock
{
    public class FakeSession : ISession
    {
        private readonly FakeActiveMQServer _server;
        private readonly List<FakeMessage> _deliveredMessages = new();
        private readonly List<FakeMessage> _transactionMessages = new();
        private bool _inTransaction = false;
        public AcknowledgementMode AcknowledgementMode { get; }
        public bool Transacted => AcknowledgementMode == AcknowledgementMode.Transactional;
        public TimeSpan RequestTimeout { get; set; }
        public ConsumerTransformerDelegate ConsumerTransformer { get; set; }
        public ProducerTransformerDelegate ProducerTransformer { get; set; }
        public string ClientId { get; set; }
        public event SessionTxEventDelegate TransactionStartedListener { add { } remove { } }
        public event SessionTxEventDelegate TransactionCommittedListener { add { } remove { } }
        public event SessionTxEventDelegate TransactionRolledBackListener { add { } remove { } }
        public event ExceptionListener ExceptionListener { add { } remove { } }
        public event ConnectionInterruptedListener ConnectionInterruptedListener { add { } remove { } }
        public event ConnectionResumedListener ConnectionResumedListener { add { } remove { } }
        public FakeSession() : this(AcknowledgementMode.AutoAcknowledge) { }
        public FakeSession(AcknowledgementMode mode)
        {
            AcknowledgementMode = mode;
            _server = FakeActiveMQServer.Instance;
        }
        public void Dispose() { }
        public IMessage CreateMessage() => new FakeTextMessage();
        public ITextMessage CreateTextMessage() => new FakeTextMessage();
        public ITextMessage CreateTextMessage(string text) => new FakeTextMessage { Text = text };
        public IMapMessage CreateMapMessage() => null;
        public IObjectMessage CreateObjectMessage(object body) => null;
        public IBytesMessage CreateBytesMessage() => null;
        public IBytesMessage CreateBytesMessage(byte[] body) => null;
        public IStreamMessage CreateStreamMessage() => null;
        public IQueue GetQueue(string name)
        {
            _server.CreateQueue(name);
            return new FakeQueue(name);
        }
        public ITopic GetTopic(string name)
        {
            _server.CreateTopic(name);
            return new FakeTopic(name);
        }
        public ITemporaryQueue CreateTemporaryQueue()
        {
            var name = Guid.NewGuid().ToString();
            _server.CreateTemporaryQueue(name);
            return new FakeTemporaryQueue(name);
        }
        public ITemporaryTopic CreateTemporaryTopic()
        {
            var name = Guid.NewGuid().ToString();
            _server.CreateTemporaryTopic(name);
            return new FakeTemporaryTopic(name);
        }
        public void DeleteDestination(IDestination destination)
        {
            if (destination is FakeQueue q) _server.DeleteQueue(q.QueueName);
            if (destination is FakeTopic t) _server.DeleteTopic(t.TopicName);
            if (destination is FakeTemporaryQueue tq) _server.DeleteTemporaryQueue(tq.QueueName);
            if (destination is FakeTemporaryTopic tt) _server.DeleteTemporaryTopic(tt.TopicName);
        }
        public void Close() { }
        public void Recover() { }
        public void Acknowledge()
        {
            if (AcknowledgementMode == AcknowledgementMode.ClientAcknowledge)
            {
                foreach (var msg in _deliveredMessages)
                    _server.Acknowledge(msg);
                _deliveredMessages.Clear();
            }
        }
        public void Commit()
        {
            if (AcknowledgementMode == AcknowledgementMode.Transactional)
            {
                foreach (var msg in _transactionMessages)
                    _server.Acknowledge(msg);
                _transactionMessages.Clear();
                _inTransaction = false;
            }
        }
        public void Rollback()
        {
            if (AcknowledgementMode == AcknowledgementMode.Transactional)
            {
                foreach (var msg in _transactionMessages)
                {
                    msg.Acknowledged = false;
                    msg.RedeliveryCount++;
                    // Optionally re-enqueue for redelivery
                    _server.EnqueueMessage(msg.Destination, msg);
                }
                _transactionMessages.Clear();
                _inTransaction = false;
            }
        }
        public void PurgeTempDestinations() { }
        public IMessageConsumer CreateConsumer(IDestination destination) => (IMessageConsumer)new FakeNMSConsumer(destination.ToString(), null, this);
        public IMessageConsumer CreateConsumer(IDestination destination, string selector) => (IMessageConsumer)new FakeNMSConsumer(destination.ToString(), msg => string.IsNullOrEmpty(selector) || msg.Body.Contains(selector), this);
        public IMessageConsumer CreateConsumer(IDestination destination, string selector, bool noLocal) => (IMessageConsumer)new FakeNMSConsumer(destination.ToString(), msg => string.IsNullOrEmpty(selector) || msg.Body.Contains(selector), this);
        public IMessageProducer CreateProducer() => (IMessageProducer)(new FakeNMSProducer());
        public IMessageProducer CreateProducer(IDestination destination) => (IMessageProducer)(new FakeNMSProducer());
        public IQueueBrowser CreateBrowser(IQueue queue) => new FakeQueueBrowser(queue, null);

        // --- STUBS FOR INTERFACE ---
        public Task<IMessageProducer> CreateProducerAsync() => Task.FromResult((IMessageProducer)new FakeNMSProducer());
        public Task<IMessageProducer> CreateProducerAsync(IDestination destination) => Task.FromResult((IMessageProducer)new FakeNMSProducer());
        public Task<IMessageConsumer> CreateConsumerAsync(IDestination destination) => Task.FromResult((IMessageConsumer)new FakeNMSConsumer(destination.ToString(), null, this));
        public Task<IMessageConsumer> CreateConsumerAsync(IDestination destination, string selector) => Task.FromResult((IMessageConsumer)new FakeNMSConsumer(destination.ToString(), msg => string.IsNullOrEmpty(selector) || msg.Body.Contains(selector), this));
        public Task<IMessageConsumer> CreateConsumerAsync(IDestination destination, string selector, bool noLocal) => Task.FromResult((IMessageConsumer)new FakeNMSConsumer(destination.ToString(), msg => string.IsNullOrEmpty(selector) || msg.Body.Contains(selector), this));
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
        public void Unsubscribe(string name) { }
        public Task UnsubscribeAsync(string name) => Task.CompletedTask;
        public Task<IQueueBrowser> CreateBrowserAsync(IQueue queue) => Task.FromResult<IQueueBrowser>(new FakeQueueBrowser(queue, null));
        public IQueueBrowser CreateBrowser(IQueue queue, string selector) => new FakeQueueBrowser(queue, selector);
        public Task<IQueueBrowser> CreateBrowserAsync(IQueue queue, string selector) => Task.FromResult<IQueueBrowser>(new FakeQueueBrowser(queue, selector));
        public Task<IQueue> GetQueueAsync(string name) => Task.FromResult((IQueue)new FakeQueue(name));
        public Task<ITopic> GetTopicAsync(string name) => Task.FromResult((ITopic)new FakeTopic(name));
        public Task<ITemporaryQueue> CreateTemporaryQueueAsync() => Task.FromResult((ITemporaryQueue)CreateTemporaryQueue());
        public Task<ITemporaryTopic> CreateTemporaryTopicAsync() => Task.FromResult((ITemporaryTopic)CreateTemporaryTopic());
        public Task DeleteDestinationAsync(IDestination destination) { DeleteDestination(destination); return Task.CompletedTask; }
        public Task<IMessage> CreateMessageAsync() => Task.FromResult((IMessage)new FakeTextMessage());
        public Task<ITextMessage> CreateTextMessageAsync() => Task.FromResult((ITextMessage)new FakeTextMessage());
        public Task<ITextMessage> CreateTextMessageAsync(string text) => Task.FromResult((ITextMessage)new FakeTextMessage { Text = text });
        public Task<IMapMessage> CreateMapMessageAsync() => Task.FromResult<IMapMessage>(null);
        public Task<IObjectMessage> CreateObjectMessageAsync(object body) => Task.FromResult<IObjectMessage>(null);
        public Task<IBytesMessage> CreateBytesMessageAsync() => Task.FromResult<IBytesMessage>(null);
        public Task<IBytesMessage> CreateBytesMessageAsync(byte[] body) => Task.FromResult<IBytesMessage>(null);
        public Task<IStreamMessage> CreateStreamMessageAsync() => Task.FromResult<IStreamMessage>(null);
        public Task CloseAsync() { Close(); return Task.CompletedTask; }
        public Task RecoverAsync() { Recover(); return Task.CompletedTask; }
        public Task AcknowledgeAsync() { Acknowledge(); return Task.CompletedTask; }
        public Task CommitAsync() { Commit(); return Task.CompletedTask; }
        public Task RollbackAsync() { Rollback(); return Task.CompletedTask; }

        // Called by consumer when a message is delivered
        internal void OnMessageDelivered(FakeMessage msg)
        {
            switch (AcknowledgementMode)
            {
                case AcknowledgementMode.AutoAcknowledge:
                case AcknowledgementMode.DupsOkAcknowledge:
                    _server.Acknowledge(msg);
                    break;
                case AcknowledgementMode.ClientAcknowledge:
                    _deliveredMessages.Add(msg);
                    break;
                case AcknowledgementMode.Transactional:
                    _transactionMessages.Add(msg);
                    _inTransaction = true;
                    break;
            }
        }
    }
}
