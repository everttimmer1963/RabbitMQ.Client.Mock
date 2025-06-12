using Xunit;
using Apache.NMS.ActiveMQ.Mock;
using Apache.NMS.ActiveMQ.Mock.Server;

namespace Apache.NMS.ActiveMQ.Mock.Tests
{
    public class FakeNMSConsumerFullTests
    {
        [Fact]
        public void Receive_ReturnsMessageIfPresent()
        {
            var queueName = "consumer-q";
            var server = FakeActiveMQServer.Instance;
            server.CreateQueue(queueName);
            server.EnqueueMessage(queueName, new FakeMessage { Body = "foo", Destination = queueName });
            var consumer = new FakeNMSConsumer(queueName, null, null);
            var msg = consumer.Receive();
            Assert.NotNull(msg);
            Assert.Equal("foo", ((ITextMessage)msg).Text);
        }

        [Fact]
        public void Receive_WithSelector_ReturnsMatching()
        {
            var queueName = "consumer-q2";
            var server = FakeActiveMQServer.Instance;
            server.CreateQueue(queueName);
            server.EnqueueMessage(queueName, new FakeMessage { Body = "foo", Destination = queueName });
            server.EnqueueMessage(queueName, new FakeMessage { Body = "bar", Destination = queueName });
            var consumer = new FakeNMSConsumer(queueName, m => m.Body == "bar", null);
            var msg = consumer.Receive();
            Assert.NotNull(msg);
            Assert.Equal("bar", ((ITextMessage)msg).Text);
        }

        [Fact]
        public void ReceiveNoWait_ReturnsMessage()
        {
            var queueName = "consumer-q3";
            var server = FakeActiveMQServer.Instance;
            server.CreateQueue(queueName);
            server.EnqueueMessage(queueName, new FakeMessage { Body = "baz", Destination = queueName });
            var consumer = new FakeNMSConsumer(queueName, null, null);
            var msg = consumer.ReceiveNoWait();
            Assert.NotNull(msg);
            Assert.Equal("baz", ((ITextMessage)msg).Text);
        }

        [Fact]
        public void ReceiveBody_ReturnsText()
        {
            var queueName = "consumer-q4";
            var server = FakeActiveMQServer.Instance;
            server.CreateQueue(queueName);
            server.EnqueueMessage(queueName, new FakeMessage { Body = "body", Destination = queueName });
            var consumer = new FakeNMSConsumer(queueName, null, null);
            var msg = consumer.Receive();
            Assert.Equal("body", ((ITextMessage)msg).Text);
        }

        [Fact]
        public void Close_And_Dispose_DoesNotThrow()
        {
            var consumer = new FakeNMSConsumer("q", null, null);
            consumer.Close();
            consumer.Dispose();
        }
    }
}
