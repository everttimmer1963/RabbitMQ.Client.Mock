using System.Threading.Tasks;
using Xunit;
using Apache.NMS.ActiveMQ.Mock;

namespace Apache.NMS.ActiveMQ.Mock.Tests
{
    public class FakeSessionAsyncTests
    {
        [Fact]
        public async Task CreateProducerAsync_ReturnsProducer()
        {
            var session = new FakeSession();
            IMessageProducer producer = await session.CreateProducerAsync();
            Assert.NotNull(producer);
        }

        [Fact]
        public async Task CreateConsumerAsync_ReturnsConsumer()
        {
            var session = new FakeSession();
            var queue = session.GetQueue("async-session-q");
            var consumer = await session.CreateConsumerAsync(queue);
            Assert.NotNull(consumer);
        }

        [Fact]
        public async Task CreateBrowserAsync_ReturnsBrowser()
        {
            var session = new FakeSession();
            var queue = session.GetQueue("async-session-bq");
            var browser = await session.CreateBrowserAsync(queue);
            Assert.NotNull(browser);
        }

        [Fact]
        public async Task GetQueueAsync_ReturnsQueue()
        {
            var session = new FakeSession();
            var queue = await session.GetQueueAsync("async-session-q2");
            Assert.NotNull(queue);
        }

        [Fact]
        public async Task GetTopicAsync_ReturnsTopic()
        {
            var session = new FakeSession();
            var topic = await session.GetTopicAsync("async-session-t");
            Assert.NotNull(topic);
        }

        [Fact]
        public async Task CreateTemporaryQueueAsync_ReturnsQueue()
        {
            var session = new FakeSession();
            var tempQueue = await session.CreateTemporaryQueueAsync();
            Assert.NotNull(tempQueue);
        }

        [Fact]
        public async Task CreateTemporaryTopicAsync_ReturnsTopic()
        {
            var session = new FakeSession();
            var tempTopic = await session.CreateTemporaryTopicAsync();
            Assert.NotNull(tempTopic);
        }

        [Fact]
        public async Task DeleteDestinationAsync_DoesNotThrow()
        {
            var session = new FakeSession();
            var queue = session.GetQueue("async-delq");
            await session.DeleteDestinationAsync(queue);
        }

        [Fact]
        public async Task CloseAsync_DoesNotThrow()
        {
            var session = new FakeSession();
            await session.CloseAsync();
        }

        [Fact]
        public async Task RecoverAsync_DoesNotThrow()
        {
            var session = new FakeSession();
            await session.RecoverAsync();
        }

        [Fact]
        public async Task AcknowledgeAsync_DoesNotThrow()
        {
            var session = new FakeSession();
            await session.AcknowledgeAsync();
        }

        [Fact]
        public async Task CommitAsync_DoesNotThrow()
        {
            var session = new FakeSession();
            await session.CommitAsync();
        }

        [Fact]
        public async Task RollbackAsync_DoesNotThrow()
        {
            var session = new FakeSession();
            await session.RollbackAsync();
        }
    }
}
