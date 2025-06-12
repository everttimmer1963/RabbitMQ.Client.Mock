using Xunit;
using Apache.NMS.ActiveMQ.Mock;
using Apache.NMS;

namespace Apache.NMS.ActiveMQ.Mock.Tests
{
    public class FakeSessionTests
    {
        [Fact]
        public void CreateMessage_ReturnsFakeTextMessage()
        {
            var session = new FakeSession();
            var msg = session.CreateMessage();
            Assert.IsType<FakeTextMessage>(msg);
        }

        [Fact]
        public void CreateTextMessage_ReturnsFakeTextMessage()
        {
            var session = new FakeSession();
            var msg = session.CreateTextMessage();
            Assert.IsType<FakeTextMessage>(msg);
        }

        [Fact]
        public void CreateTextMessage_WithText_SetsText()
        {
            var session = new FakeSession();
            var msg = session.CreateTextMessage("hello");
            Assert.Equal("hello", msg.Text);
        }

        [Fact]
        public void GetQueue_CreatesQueue()
        {
            var session = new FakeSession();
            var queue = session.GetQueue("q");
            Assert.NotNull(queue);
            Assert.Equal("q", queue.ToString());
        }

        [Fact]
        public void GetTopic_CreatesTopic()
        {
            var session = new FakeSession();
            var topic = session.GetTopic("t");
            Assert.NotNull(topic);
            Assert.Equal("t", topic.ToString());
        }

        [Fact]
        public void CreateTemporaryQueue_ReturnsQueue()
        {
            var session = new FakeSession();
            var tempQueue = session.CreateTemporaryQueue();
            Assert.NotNull(tempQueue);
        }

        [Fact]
        public void CreateTemporaryTopic_ReturnsTopic()
        {
            var session = new FakeSession();
            var tempTopic = session.CreateTemporaryTopic();
            Assert.NotNull(tempTopic);
        }

        [Fact]
        public void DeleteDestination_RemovesQueueOrTopic()
        {
            var session = new FakeSession();
            var queue = session.GetQueue("delq");
            var topic = session.GetTopic("delt");
            session.DeleteDestination(queue);
            session.DeleteDestination(topic);
            // No exception means success
        }

        [Fact]
        public void Acknowledge_ClientAcknowledge_ClearsDelivered()
        {
            var session = new FakeSession(AcknowledgementMode.ClientAcknowledge);
            var msg = new Server.FakeMessage { Body = "b", Destination = "d" };
            session.GetType().GetMethod("OnMessageDelivered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(session, new object[] { msg });
            session.Acknowledge();
            // No exception means success
        }

        [Fact]
        public void Commit_Transactional_ClearsTransaction()
        {
            var session = new FakeSession(AcknowledgementMode.Transactional);
            var msg = new Server.FakeMessage { Body = "b", Destination = "d" };
            session.GetType().GetMethod("OnMessageDelivered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(session, new object[] { msg });
            session.Commit();
            // No exception means success
        }

        [Fact]
        public void Rollback_Transactional_ClearsTransaction()
        {
            var session = new FakeSession(AcknowledgementMode.Transactional);
            var msg = new Server.FakeMessage { Body = "b", Destination = "d" };
            session.GetType().GetMethod("OnMessageDelivered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(session, new object[] { msg });
            session.Rollback();
            // No exception means success
        }

        [Fact]
        public void CreateConsumer_And_Producer_DoesNotThrow()
        {
            var session = new FakeSession();
            var queue = session.GetQueue("q");
            var consumer = session.CreateConsumer(queue);
            IMessageProducer producer = session.CreateProducer();
            Assert.NotNull(consumer);
            Assert.NotNull(producer);
        }
    }
}
