using Xunit;
using Apache.NMS.ActiveMQ.Mock;
using Apache.NMS.ActiveMQ.Mock.Server;
using System.Threading.Tasks;

namespace Apache.NMS.ActiveMQ.Mock.Tests
{
    public class FakeNMSProducerTests
    {
        [Fact]
        public void Send_TextMessage_EnqueuesMessage()
        {
            var producer = new FakeNMSProducer();
            var queue = new FakeQueue("prod-q");
            var server = FakeActiveMQServer.Instance;
            server.CreateQueue(queue.ToString());
            var msg = new FakeTextMessage { Text = "hello", NMSDestination = queue };
            producer.Send(queue, msg);
            Assert.Contains(server.Queues[queue.ToString()], m => m.Body == "hello");
        }

        [Fact]
        public void Send_String_EnqueuesMessage()
        {
            var producer = new FakeNMSProducer();
            var queue = new FakeQueue("prod-q2");
            var server = FakeActiveMQServer.Instance;
            server.CreateQueue(queue.ToString());
            producer.Send(queue, "hi");
            Assert.Contains(server.Queues[queue.ToString()], m => m.Body == "hi");
        }

        [Fact]
        public void Send_GenericMessage_EnqueuesMessage()
        {
            var producer = new FakeNMSProducer();
            var queue = new FakeQueue("prod-q3");
            var server = FakeActiveMQServer.Instance;
            server.CreateQueue(queue.ToString());
            var msg = new FakeTextMessage { Text = "generic", NMSDestination = queue };
            producer.Send(msg);
            Assert.Contains(server.Queues[queue.ToString()], m => m.Body == "generic");
        }

        [Fact]
        public async Task SendAsync_TextMessage_EnqueuesMessage()
        {
            var producer = new FakeNMSProducer();
            var queue = new FakeQueue("prod-q4");
            var server = FakeActiveMQServer.Instance;
            server.CreateQueue(queue.ToString());
            var msg = new FakeTextMessage { Text = "async", NMSDestination = queue };
            await producer.SendAsync(queue, msg);
            Assert.Contains(server.Queues[queue.ToString()], m => m.Body == "async");
        }

        [Fact]
        public void CreateTextMessage_SetsText()
        {
            var producer = new FakeNMSProducer();
            var msg = producer.CreateTextMessage("foo");
            Assert.Equal("foo", msg.Text);
        }

        [Fact]
        public void Setters_Work()
        {
            var producer = new FakeNMSProducer();
            producer.SetDeliveryDelay(System.TimeSpan.FromSeconds(1));
            producer.SetTimeToLive(System.TimeSpan.FromSeconds(2));
            producer.SetDeliveryMode(MsgDeliveryMode.Persistent);
            producer.SetDisableMessageID(true);
            producer.SetDisableMessageTimestamp(true);
            producer.SetNMSCorrelationID("cid");
            producer.SetNMSReplyTo(new FakeQueue("r"));
            producer.SetNMSType("type");
            producer.SetPriority(MsgPriority.Highest);
            producer.SetProperty("p1", 1);
            producer.SetProperty("p2", "v");
            producer.ClearProperties();
        }
    }
}
