using System.Threading.Tasks;
using Xunit;
using Apache.NMS.ActiveMQ.Mock;

namespace Apache.NMS.ActiveMQ.Mock.Tests
{
    public class FakeNMSContextAsyncTests
    {
        [Fact]
        public async Task CreateProducerAsync_ReturnsProducer()
        {
            var context = new FakeNMSContext();
            var producer = await context.CreateProducerAsync();
            Assert.NotNull(producer);
        }

        [Fact]
        public async Task CreateConsumerAsync_ReturnsNull()
        {
            var context = new FakeNMSContext();
            var consumer = await context.CreateConsumerAsync(context.GetQueue("q"));
            Assert.Null(consumer);
        }

        [Fact]
        public async Task GetQueueAsync_ReturnsQueue()
        {
            var context = new FakeNMSContext();
            var queue = await context.GetQueueAsync("async-q");
            Assert.NotNull(queue);
            Assert.Equal("async-q", queue.ToString());
        }

        [Fact]
        public async Task GetTopicAsync_ReturnsTopic()
        {
            var context = new FakeNMSContext();
            var topic = await context.GetTopicAsync("async-t");
            Assert.NotNull(topic);
            Assert.Equal("async-t", topic.ToString());
        }

        [Fact]
        public async Task CreateBrowserAsync_ReturnsBrowser()
        {
            var context = new FakeNMSContext();
            var queue = context.GetQueue("bq");
            var browser = await context.CreateBrowserAsync(queue);
            Assert.NotNull(browser);
        }

        [Fact]
        public async Task CreateBrowserAsync_WithSelector_ReturnsBrowser()
        {
            var context = new FakeNMSContext();
            var queue = context.GetQueue("bq2");
            var browser = await context.CreateBrowserAsync(queue, "foo");
            Assert.NotNull(browser);
        }

        [Fact]
        public async Task CreateTemporaryQueueAsync_ReturnsQueue()
        {
            var context = new FakeNMSContext();
            var tempQueue = await context.CreateTemporaryQueueAsync();
            Assert.NotNull(tempQueue);
        }

        [Fact]
        public async Task CreateTemporaryTopicAsync_ReturnsTopic()
        {
            var context = new FakeNMSContext();
            var tempTopic = await context.CreateTemporaryTopicAsync();
            Assert.NotNull(tempTopic);
        }
    }
}
