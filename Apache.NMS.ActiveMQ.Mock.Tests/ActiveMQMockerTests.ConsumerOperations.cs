using Xunit;
using Apache.NMS.ActiveMQ.Mock;
using Xunit.Abstractions;

namespace Apache.NMS.ActiveMQ.Mock.Tests
{
    public class ConsumerOperationsTests
    {
        private readonly ITestOutputHelper _output;
        public ConsumerOperationsTests(ITestOutputHelper output) { _output = output; }

        [Fact]
        public void Consumer_With_Selector_Receives_Only_Matching()
        {
            _output.WriteLine("[TEST] Starting Consumer_With_Selector_Receives_Only_Matching");
            var context = new FakeNMSContext();
            _output.WriteLine($"[TEST] Created FakeNMSContext: {context.GetHashCode()}");
            var queue = context.GetQueue("selector-q");
            _output.WriteLine($"[TEST] Got queue: {queue}, Hash: {queue.GetHashCode()}");
            var producer = context.CreateProducer();
            _output.WriteLine($"[TEST] Created producer: {producer.GetHashCode()}");
            producer.Send(queue, producer.CreateTextMessage("foo"));
            _output.WriteLine("[TEST] Sent message: foo");
            producer.Send(queue, producer.CreateTextMessage("bar"));
            _output.WriteLine("[TEST] Sent message: bar");
            var consumer = context.CreateConsumer(queue, "foo");
            _output.WriteLine($"[TEST] Created consumer with selector: {consumer.GetHashCode()}");
            var received = consumer.Receive() as ITextMessage;
            _output.WriteLine($"[TEST] Received message: {(received != null ? received.Text : "<null>")}");
            Assert.NotNull(received);
            Assert.Equal("foo", received.Text);
        }
    }
}
