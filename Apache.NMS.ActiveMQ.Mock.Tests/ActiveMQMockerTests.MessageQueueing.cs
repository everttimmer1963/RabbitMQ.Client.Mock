using Apache.NMS.ActiveMQ.Mock;
using Xunit;
using Xunit.Abstractions;

namespace Apache.NMS.ActiveMQ.Mock.Tests
{
    public class MessageQueueingTests
    {
        private readonly ITestOutputHelper _output;
        public MessageQueueingTests(ITestOutputHelper output) { _output = output; }

        [Fact]
        public void Producer_Enqueues_And_Consumer_Dequeues_Message()
        {
            _output.WriteLine("[TEST] Starting Producer_Enqueues_And_Consumer_Dequeues_Message");
            var context = new FakeNMSContext();
            _output.WriteLine($"[TEST] Created FakeNMSContext: {context.GetHashCode()}");
            var queue = context.GetQueue("q1");
            _output.WriteLine($"[TEST] Got queue: {queue}, Hash: {queue.GetHashCode()}");
            var producer = context.CreateProducer();
            _output.WriteLine($"[TEST] Created producer: {producer.GetHashCode()}");
            var consumer = context.CreateConsumer(queue);
            _output.WriteLine($"[TEST] Created consumer: {consumer.GetHashCode()}");
            var text = "test-message";
            var msg = producer.CreateTextMessage(text);
            _output.WriteLine($"[TEST] Created text message: {msg.Text}");
            producer.Send(queue, msg);
            _output.WriteLine($"[TEST] Sent message: {text} to queue: {queue}");
            var received = consumer.Receive() as ITextMessage;
            _output.WriteLine($"[TEST] Received message: {(received != null ? received.Text : "<null>")}");
            Assert.NotNull(received);
            Assert.Equal(text, received.Text);
        }

        [Fact]
        public void Consumer_Receives_Null_When_Queue_Empty()
        {
            _output.WriteLine("[TEST] Starting Consumer_Receives_Null_When_Queue_Empty");
            var context = new FakeNMSContext();
            _output.WriteLine($"[TEST] Created FakeNMSContext: {context.GetHashCode()}");
            var queue = context.GetQueue("empty-q");
            _output.WriteLine($"[TEST] Got queue: {queue}, Hash: {queue.GetHashCode()}");
            var consumer = context.CreateConsumer(queue);
            _output.WriteLine($"[TEST] Created consumer: {consumer.GetHashCode()}");
            var received = consumer.Receive();
            _output.WriteLine($"[TEST] Received message from empty queue: {(received != null ? received.ToString() : "<null>")}");
            Assert.Null(received);
        }
    }
}