using System;
using System.Collections;
using System.Threading.Tasks;

namespace Apache.NMS.ActiveMQ.Mock;

internal class FakeNMSProducer : INMSProducer
{
    private readonly FakeActiveMQServer _server = FakeActiveMQServer.Instance;
    public IDestination Destination { get; set; }
    public TimeSpan RequestTimeout { get; set; }
    public ProducerTransformerDelegate ProducerTransformer { get; set; }
    public MsgDeliveryMode DeliveryMode { get; set; }
    public TimeSpan DeliveryDelay { get; set; }
    public TimeSpan TimeToLive { get; set; }
    public MsgPriority Priority { get; set; }
    public bool DisableMessageID { get; set; }
    public bool DisableMessageTimestamp { get; set; }
    public string NMSCorrelationID { get; set; }
    public IDestination NMSReplyTo { get; set; }
    public string NMSType { get; set; }
    public IPrimitiveMap Properties { get; } = new FakePrimitiveMap();
    public INMSProducer Send(IDestination destination, IMessage message) { Send(message); return this; }
    public INMSProducer Send(IDestination destination, string text) { Send(destination, new FakeTextMessage { Text = text, NMSDestination = destination }); return this; }
    public INMSProducer Send(IDestination destination, IPrimitiveMap map) { return this; }
    public INMSProducer Send(IDestination destination, byte[] body) { return this; }
    public INMSProducer Send(IDestination destination, object body) { return this; }
    public Task<INMSProducer> SendAsync(IDestination destination, IMessage message) { Send(message); return Task.FromResult(this as INMSProducer); }
    public Task<INMSProducer> SendAsync(IDestination destination, string text) { Send(destination, text); return Task.FromResult(this as INMSProducer); }
    public Task<INMSProducer> SendAsync(IDestination destination, IPrimitiveMap map) { return Task.FromResult(this as INMSProducer); }
    public Task<INMSProducer> SendAsync(IDestination destination, byte[] body) { return Task.FromResult(this as INMSProducer); }
    public Task<INMSProducer> SendAsync(IDestination destination, object body) { return Task.FromResult(this as INMSProducer); }
    public INMSProducer ClearProperties() { Properties.Clear(); return this; }
    public INMSProducer SetDeliveryDelay(TimeSpan deliveryDelay) { DeliveryDelay = deliveryDelay; return this; }
    public INMSProducer SetTimeToLive(TimeSpan timeToLive) { TimeToLive = timeToLive; return this; }
    public INMSProducer SetDeliveryMode(MsgDeliveryMode deliveryMode) { DeliveryMode = deliveryMode; return this; }
    public INMSProducer SetDisableMessageID(bool value) { DisableMessageID = value; return this; }
    public INMSProducer SetDisableMessageTimestamp(bool value) { DisableMessageTimestamp = value; return this; }
    public INMSProducer SetNMSCorrelationID(string value) { NMSCorrelationID = value; return this; }
    public INMSProducer SetNMSReplyTo(IDestination value) { NMSReplyTo = value; return this; }
    public INMSProducer SetNMSType(string value) { NMSType = value; return this; }
    public INMSProducer SetPriority(MsgPriority value) { Priority = value; return this; }
    public INMSProducer SetProperty(string name, bool value) { Properties[name] = value; return this; }
    public INMSProducer SetProperty(string name, byte value) { Properties[name] = value; return this; }
    public INMSProducer SetProperty(string name, double value) { Properties[name] = value; return this; }
    public INMSProducer SetProperty(string name, float value) { Properties[name] = value; return this; }
    public INMSProducer SetProperty(string name, int value) { Properties[name] = value; return this; }
    public INMSProducer SetProperty(string name, long value) { Properties[name] = value; return this; }
    public INMSProducer SetProperty(string name, short value) { Properties[name] = value; return this; }
    public INMSProducer SetProperty(string name, char value) { Properties[name] = value; return this; }
    public INMSProducer SetProperty(string name, string value) { Properties[name] = value; return this; }
    public INMSProducer SetProperty(string name, byte[] value) { Properties[name] = value; return this; }
    public INMSProducer SetProperty(string name, IList value) { Properties[name] = value; return this; }
    public INMSProducer SetProperty(string name, IDictionary value) { Properties[name] = value; return this; }
    public IMessage CreateMessage() => new FakeTextMessage();
    public Task<IMessage> CreateMessageAsync() => Task.FromResult<IMessage>(new FakeTextMessage());
    public ITextMessage CreateTextMessage() => new FakeTextMessage();
    public Task<ITextMessage> CreateTextMessageAsync() => Task.FromResult<ITextMessage>(new FakeTextMessage());
    public ITextMessage CreateTextMessage(string text) => new FakeTextMessage { Text = text };
    public Task<ITextMessage> CreateTextMessageAsync(string text) => Task.FromResult<ITextMessage>(new FakeTextMessage { Text = text });
    public IMapMessage CreateMapMessage() => null;
    public Task<IMapMessage> CreateMapMessageAsync() => Task.FromResult<IMapMessage>(null);
    public IObjectMessage CreateObjectMessage(object body) => null;
    public Task<IObjectMessage> CreateObjectMessageAsync(object body) => Task.FromResult<IObjectMessage>(null);
    public IBytesMessage CreateBytesMessage() => null;
    public Task<IBytesMessage> CreateBytesMessageAsync() => Task.FromResult<IBytesMessage>(null);
    public IBytesMessage CreateBytesMessage(byte[] body) => null;
    public Task<IBytesMessage> CreateBytesMessageAsync(byte[] body) => Task.FromResult<IBytesMessage>(null);
    public IStreamMessage CreateStreamMessage() => null;
    public Task<IStreamMessage> CreateStreamMessageAsync() => Task.FromResult<IStreamMessage>(null);
    public void Send(IMessage message)
    {
        var dest = Destination?.ToString() ?? message.NMSDestination?.ToString();
        if (!string.IsNullOrEmpty(dest))
        {
            _server.EnqueueMessage(dest, new FakeMessage { Body = message is ITextMessage tm ? tm.Text : "", Destination = dest });
        }
    }
    public void Close() { }
    public Task CloseAsync() => Task.CompletedTask;
    public void Dispose() { }
}
