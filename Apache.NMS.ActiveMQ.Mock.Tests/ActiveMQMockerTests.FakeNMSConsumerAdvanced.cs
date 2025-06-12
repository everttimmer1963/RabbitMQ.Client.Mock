using Xunit;
using Apache.NMS.ActiveMQ.Mock;
using Xunit.Abstractions;

namespace Apache.NMS.ActiveMQ.Mock.Tests
{
    public class FakeNMSConsumerAdvancedTests
    {
        private readonly ITestOutputHelper _output;
        public FakeNMSConsumerAdvancedTests(ITestOutputHelper output) { _output = output; }

        [Fact]
        public void Close_And_Dispose_DoesNotThrow()
        {
            var consumer = new FakeNMSConsumer("q", null, null);
            consumer.Close();
            consumer.Dispose();
            _output.WriteLine("Closed and disposed consumer without exception.");
        }
    }
}
