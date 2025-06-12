using Xunit;
using Apache.NMS.ActiveMQ.Mock.Server;

namespace Apache.NMS.ActiveMQ.Mock.Tests
{
    public class FakeServerAndEntitiesTests
    {
        [Fact]
        public void FakeMessage_Properties_Work()
        {
            var msg = new FakeMessage { Body = "body", Destination = "dest" };
            Assert.Equal("body", msg.Body);
            Assert.Equal("dest", msg.Destination);
            Assert.Equal(0, msg.RedeliveryCount);
            Assert.False(msg.Acknowledged);
            msg.RedeliveryCount = 2;
            msg.Acknowledged = true;
            Assert.Equal(2, msg.RedeliveryCount);
            Assert.True(msg.Acknowledged);
        }

        [Fact]
        public void FakeQueue_And_Topic_Properties_Work()
        {
            var queue = new FakeQueue("q");
            Assert.Equal("q", queue.QueueName);
            Assert.True(queue.IsQueue);
            Assert.False(queue.IsTopic);
            Assert.False(queue.IsTemporary);
            Assert.Equal("q", queue.ToString());
            queue.Dispose();

            var topic = new FakeTopic("t");
            Assert.Equal("t", topic.TopicName);
            Assert.True(topic.IsTopic);
            Assert.False(topic.IsQueue);
            Assert.False(topic.IsTemporary);
            Assert.Equal("t", topic.ToString());
            topic.Dispose();
        }

        [Fact]
        public void FakeTemporaryQueue_And_Topic_Properties_Work()
        {
            var tempQueue = new FakeTemporaryQueue("tempQ");
            Assert.Equal("tempQ", tempQueue.QueueName);
            Assert.True(tempQueue.IsQueue);
            Assert.False(tempQueue.IsTopic);
            Assert.True(tempQueue.IsTemporary);
            Assert.Equal("tempQ", tempQueue.ToString());
            tempQueue.Delete();
            tempQueue.Dispose();

            var tempTopic = new FakeTemporaryTopic("tempT");
            Assert.Equal("tempT", tempTopic.TopicName);
            Assert.True(tempTopic.IsTopic);
            Assert.False(tempTopic.IsQueue);
            Assert.True(tempTopic.IsTemporary);
            Assert.Equal("tempT", tempTopic.ToString());
            tempTopic.Delete();
            tempTopic.Dispose();
        }

        [Fact]
        public void FakeActiveMQServer_Queue_And_Topic_Operations_Work()
        {
            var server = FakeActiveMQServer.Instance;
            server.CreateQueue("q1");
            server.CreateTopic("t1");
            Assert.True(server.Queues.ContainsKey("q1"));
            Assert.True(server.Topics.ContainsKey("t1"));
            server.DeleteQueue("q1");
            server.DeleteTopic("t1");
            Assert.False(server.Queues.ContainsKey("q1"));
            Assert.False(server.Topics.ContainsKey("t1"));
        }

        [Fact]
        public void FakeActiveMQServer_DeadLetter_And_Transaction_Work()
        {
            var server = FakeActiveMQServer.Instance;
            var msg = new FakeMessage { Body = "dead", Destination = "dlq" };
            server.CreateDeadLetterQueue("dlq");
            server.AddToDeadLetterQueue("dlq", msg);
            Assert.Contains(msg, server.DeadLetterQueues["dlq"]);
            server.BeginTransaction("tx1");
            server.EnqueueMessage("q2", new FakeMessage { Body = "txmsg", Destination = "q2" }, "tx1");
            server.CommitTransaction("tx1");
            server.RollbackTransaction("tx2"); // Should not throw
        }

        [Fact]
        public void FakeActiveMQServer_Enqueue_And_Dequeue_Message()
        {
            var server = FakeActiveMQServer.Instance;
            server.CreateQueue("q3");
            var msg = new FakeMessage { Body = "msg", Destination = "q3" };
            server.EnqueueMessage("q3", msg);
            var dequeued = server.DequeueMessage("q3");
            Assert.Equal(msg.Body, dequeued.Body);
        }
    }
}
