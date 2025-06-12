using Apache.NMS.ActiveMQ.Mock.Server;
using System.Threading.Tasks;
using Xunit;
using Apache.NMS.ActiveMQ.Mock;
using System.Collections;

namespace Apache.NMS.ActiveMQ.Mock.Tests
{
    public class FakeNMSProducerAsyncAndOverloadsTests
    {
        [Fact]
        public async Task SendAsync_String_Works()
        {
            var producer = new FakeNMSProducer();
            var queue = new FakeQueue("async-pq");
            var server = FakeActiveMQServer.Instance;
            server.CreateQueue(queue.ToString());
            await producer.SendAsync(queue, new FakeTextMessage { Text = "async", NMSDestination = queue });
            Assert.Contains(server.Queues[queue.ToString()], m => m.Body == "async");
        }

        [Fact]
        public void Send_IPrimitiveMap_Works()
        {
            var producer = new FakeNMSProducer();
            var queue = new FakeQueue("map-pq");
            var server = FakeActiveMQServer.Instance;
            server.CreateQueue(queue.ToString());
            var map = new FakePrimitiveMap();
            producer.Send(queue, map);
            // No exception means success
        }

        [Fact]
        public void Send_ByteArray_Works()
        {
            var producer = new FakeNMSProducer();
            var queue = new FakeQueue("bytes-pq");
            var server = FakeActiveMQServer.Instance;
            server.CreateQueue(queue.ToString());
            producer.Send(queue, new byte[] { 1, 2, 3 });
            // No exception means success
        }
    }
}
