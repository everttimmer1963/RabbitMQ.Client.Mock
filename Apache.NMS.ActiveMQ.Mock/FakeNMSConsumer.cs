using System;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.ActiveMQ.Mock.Server;

namespace Apache.NMS.ActiveMQ.Mock
{
    // Implements both INMSConsumer and IMessageConsumer for compatibility
    public class FakeNMSConsumer : INMSConsumer, IMessageConsumer
    {
        private readonly string _queueName;
        private readonly Func<FakeMessage, bool> _selector;
        private readonly bool _durable;
        private readonly string _durableName;
        private readonly bool _shared;
        private readonly string _sharedName;
        private readonly bool _exclusive;
        private readonly bool _isTemporary;
        private readonly int _maxRedeliveries = 5;
        private readonly FakeSession _session;
        public ConsumerTransformerDelegate ConsumerTransformer { get; set; }
        public TimeSpan RequestTimeout { get; set; }
        public string MessageSelector { get; set; }
        public event MessageListener Listener;

        public FakeNMSConsumer(string queueName, Func<FakeMessage, bool> selector, FakeSession session, bool durable = false, string durableName = null, bool shared = false, string sharedName = null, bool exclusive = false, bool isTemporary = false)
        {
            _queueName = queueName;
            _selector = selector;
            _durable = durable;
            _durableName = durableName;
            _shared = shared;
            _sharedName = sharedName;
            _exclusive = exclusive;
            _isTemporary = isTemporary;
            _session = session;
        }

        public IMessage Receive() => ReceiveInternal();
        public IMessage Receive(TimeSpan timeout) => ReceiveInternal();
        public IMessage ReceiveNoWait() => ReceiveInternal();
        public T ReceiveBody<T>() => (T)(object)ReceiveInternal();
        public T ReceiveBodyNoWait<T>() => (T)(object)ReceiveInternal();
        public T ReceiveBody<T>(TimeSpan timeout) => (T)(object)ReceiveInternal();
        public Task<IMessage> ReceiveAsync() => Task.FromResult(ReceiveInternal());
        public Task<IMessage> ReceiveAsync(TimeSpan timeout) => Task.FromResult(ReceiveInternal());
        public Task<T> ReceiveBodyAsync<T>() => Task.FromResult((T)(object)ReceiveInternal());
        public Task<T> ReceiveBodyAsync<T>(TimeSpan timeout) => Task.FromResult((T)(object)ReceiveInternal());
        private IMessage ReceiveInternal()
        {
            var msg = FakeActiveMQServer.Instance.DequeueMessage(_queueName, _selector, _isTemporary);
            if (msg != null)
            {
                _session?.OnMessageDelivered(msg);
                if (Listener != null)
                    Listener(new FakeTextMessage { Text = msg.Body });
                return new FakeTextMessage { Text = msg.Body };
            }
            return null;
        }
        public void Close() { }
        public Task CloseAsync() => Task.CompletedTask;
        public void Dispose() { }
    }
}
