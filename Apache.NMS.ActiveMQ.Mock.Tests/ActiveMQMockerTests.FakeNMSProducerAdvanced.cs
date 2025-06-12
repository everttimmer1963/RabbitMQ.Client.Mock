using Xunit;
using Apache.NMS.ActiveMQ.Mock;
using System;

namespace Apache.NMS.ActiveMQ.Mock.Tests
{
    public class FakeNMSProducerAdvancedTests
    {
        [Fact]
        public void Setters_And_ClearProperties_Work()
        {
            var producer = new FakeNMSProducer();
            producer.SetDeliveryDelay(TimeSpan.FromSeconds(1));
            producer.SetTimeToLive(TimeSpan.FromSeconds(2));
            producer.SetDeliveryMode(MsgDeliveryMode.Persistent);
            producer.SetDisableMessageID(true);
            producer.SetDisableMessageTimestamp(true);
            producer.SetNMSCorrelationID("cid");
            producer.SetNMSReplyTo(null); // Use null to avoid dependency on FakeQueue type
            producer.SetNMSType("type");
            producer.SetPriority(MsgPriority.Highest);
            producer.SetProperty("p1", 1);
            producer.SetProperty("p2", "v");
            producer.ClearProperties();
        }

        [Fact]
        public void Close_And_Dispose_DoesNotThrow()
        {
            var producer = new FakeNMSProducer();
            producer.Close();
            producer.Dispose();
        }
    }
}
