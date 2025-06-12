using System.Threading.Tasks;
using Xunit;
using Apache.NMS.ActiveMQ.Mock;

namespace Apache.NMS.ActiveMQ.Mock.Tests
{
    public class FakeNMSConsumerAsyncTests
    {
        [Fact]
        public async Task ReceiveAsync_ReturnsNullIfNoMessage()
        {
            var consumer = new FakeNMSConsumer("empty-queue-async", null, null);
            var msg = await consumer.ReceiveAsync();
            Assert.Null(msg);
        }

        [Fact]
        public async Task ReceiveAsync_WithTimeout_ReturnsNullIfNoMessage()
        {
            var consumer = new FakeNMSConsumer("empty-queue-async2", null, null);
            var msg = await consumer.ReceiveAsync(System.TimeSpan.FromMilliseconds(10));
            Assert.Null(msg);
        }

        [Fact]
        public async Task ReceiveBodyAsync_ReturnsNullIfNoMessage()
        {
            var consumer = new FakeNMSConsumer("empty-queue-async3", null, null);
            var msg = await consumer.ReceiveBodyAsync<string>();
            Assert.Null(msg);
        }

        [Fact]
        public async Task ReceiveBodyAsync_WithTimeout_ReturnsNullIfNoMessage()
        {
            var consumer = new FakeNMSConsumer("empty-queue-async4", null, null);
            var msg = await consumer.ReceiveBodyAsync<string>(System.TimeSpan.FromMilliseconds(10));
            Assert.Null(msg);
        }
    }
}
