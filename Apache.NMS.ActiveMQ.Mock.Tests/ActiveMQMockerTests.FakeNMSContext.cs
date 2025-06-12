using Xunit;
using Apache.NMS.ActiveMQ.Mock;

namespace Apache.NMS.ActiveMQ.Mock.Tests
{
    public class FakeNMSContextTests
    {
        [Fact]
        public void CreateProducer_ReturnsProducer()
        {
            var context = new FakeNMSContext();
            var producer = context.CreateProducer();
            Assert.NotNull(producer);
        }

        [Fact]
        public void GetQueue_ReturnsQueue()
        {
            var context = new FakeNMSContext();
            var queue = context.GetQueue("test-queue");
            Assert.NotNull(queue);
            Assert.Equal("test-queue", queue.ToString());
        }

        [Fact]
        public void GetTopic_ReturnsTopic()
        {
            var context = new FakeNMSContext();
            var topic = context.GetTopic("test-topic");
            Assert.NotNull(topic);
            Assert.Equal("test-topic", topic.ToString());
        }
    }
}
