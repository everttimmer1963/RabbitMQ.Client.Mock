using System;
using System.Threading.Tasks;

namespace Apache.NMS.ActiveMQ.Mock;

internal class FakeNMSConsumer : INMSConsumer
{
    private readonly FakeActiveMQServer _server = FakeActiveMQServer.Instance;
    private readonly string _queueName;
    private readonly Func<FakeMessage, bool> _selector;
    private readonly bool _durable;
    private readonly string _durableName;
    private readonly bool _shared;
    private readonly string _sharedName;
    private readonly bool _exclusive;
    private readonly bool _isTemporary;
    private readonly int _maxRedeliveries = 5;
    public ConsumerTransformerDelegate ConsumerTransformer { get; set; }
    public TimeSpan RequestTimeout { get; set; }
    public string MessageSelector { get; set; }
    public event MessageListener Listener;

    public FakeNMSConsumer(string queueName, Func<FakeMessage, bool> selector, bool durable = false, string durableName = null, bool shared = false, string sharedName = null, bool exclusive = false, bool isTemporary = false)
    {
        _queueName = queueName;
        _selector = selector;
        _durable = durable;
        _durableName = durableName;
        _shared = shared;
        _sharedName = sharedName;
        _exclusive = exclusive;
        _isTemporary = isTemporary;
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
        var msg = _server.DequeueMessage(_queueName, _selector, _isTemporary);
        if (msg != null)
        {
            if (!msg.Acknowledged)
            {
                if (Listener != null)
                    Listener(new FakeTextMessage { Text = msg.Body });
                // Manual ack: only acknowledge if mode is auto
                if (true) // TODO: check session/context ack mode
                    _server.Acknowledge(msg);
            }
            return new FakeTextMessage { Text = msg.Body };
        }
        return null;
    }
    public void Close() { }
    public Task CloseAsync() => Task.CompletedTask;
    public void Dispose() { }
}
