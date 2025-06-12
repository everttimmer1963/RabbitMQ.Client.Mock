using System.Threading.Tasks;
using Xunit;
using Apache.NMS.ActiveMQ.Mock;
using System;

namespace Apache.NMS.ActiveMQ.Mock.Tests
{
    public class AsyncAndEventTests
    {
        [Fact]
        public async Task Async_Methods_Complete()
        {
            var context = new FakeNMSContext();
            var queue = context.GetQueue("async-q");
            var tempQueue = await context.CreateTemporaryQueueAsync();
            var tempTopic = await context.CreateTemporaryTopicAsync();
            await context.DeleteDestinationAsync(queue);
            await context.CloseAsync();
            await context.RecoverAsync();
            await context.AcknowledgeAsync();
            await context.CommitAsync();
            await context.RollbackAsync();
            await context.UnsubscribeAsync("foo");
            await context.StartAsync();
            await context.StopAsync();
            var producer = await context.CreateProducerAsync();
            var consumer = await context.CreateConsumerAsync(queue);
            var browser = await context.CreateBrowserAsync(queue);
            await tempQueue.DeleteAsync();
            await tempTopic.DeleteAsync();
        }
    }
}
