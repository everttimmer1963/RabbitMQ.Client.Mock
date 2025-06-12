using Xunit;
using Apache.NMS.ActiveMQ.Mock;
using Xunit.Abstractions;

namespace Apache.NMS.ActiveMQ.Mock.Tests
{
    public class FakeNMSConsumerTests
    {
        private readonly ITestOutputHelper _output;
        public FakeNMSConsumerTests(ITestOutputHelper output) { _output = output; }

        [Fact]
        public void Receive_ReturnsNullIfNoMessage()
        {
            var consumer = new FakeNMSConsumer("empty-queue", null, null);
            var msg = consumer.Receive();
            _output.WriteLine($"Received message: {(msg != null ? msg.ToString() : "<null>")}");
            Assert.Null(msg);
        }
    }
}
