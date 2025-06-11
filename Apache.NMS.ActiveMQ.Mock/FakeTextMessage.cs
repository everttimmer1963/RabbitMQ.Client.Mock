using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Apache.NMS.ActiveMQ.Mock;

internal class FakeTextMessage : ITextMessage
{
    public string Text { get; set; }
    public IDestination NMSDestination { get; set; }
    public string NMSCorrelationID { get; set; }
    public TimeSpan NMSTimeToLive { get; set; }
    public string NMSMessageId { get; set; }
    public MsgDeliveryMode NMSDeliveryMode { get; set; }
    public MsgPriority NMSPriority { get; set; }
    public bool NMSRedelivered { get; set; }
    public IDestination NMSReplyTo { get; set; }
    public DateTime NMSTimestamp { get; set; }
    public string NMSType { get; set; }
    public DateTime NMSDeliveryTime { get; set; }
    public IPrimitiveMap Properties { get; } = new FakePrimitiveMap();
    public void Acknowledge() { }
    public Task AcknowledgeAsync() => Task.CompletedTask;
    public void ClearBody() { Text = null; }
    public void ClearProperties() => Properties.Clear();
    public T Body<T>() => (T)(object)Text;
    public bool IsBodyAssignableTo(Type type) => type == typeof(string);
}
