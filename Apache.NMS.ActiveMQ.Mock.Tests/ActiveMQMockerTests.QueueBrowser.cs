using Xunit;
using Apache.NMS.ActiveMQ.Mock;
using System.Linq;
using System.Collections;
using Xunit.Abstractions;

namespace Apache.NMS.ActiveMQ.Mock.Tests
{
    public class QueueBrowserTests
    {
        private readonly ITestOutputHelper _output;
        public QueueBrowserTests(ITestOutputHelper output) { _output = output; }

        [Fact]
        public void Browser_Enumerates_All_Messages()
        {
            _output.WriteLine("[TEST] Starting Browser_Enumerates_All_Messages");
            var context = new FakeNMSContext();
            _output.WriteLine($"[TEST] Created FakeNMSContext: {context.GetHashCode()}");
            var queue = context.GetQueue("browse-q");
            _output.WriteLine($"[TEST] Got queue: {queue}, Hash: {queue.GetHashCode()}");
            var producer = context.CreateProducer();
            _output.WriteLine($"[TEST] Created producer: {producer.GetHashCode()}");
            producer.Send(queue, producer.CreateTextMessage("a"));
            _output.WriteLine("[TEST] Sent message: a");
            producer.Send(queue, producer.CreateTextMessage("b"));
            _output.WriteLine("[TEST] Sent message: b");
            var browser = context.CreateBrowser(queue);
            _output.WriteLine($"[TEST] Created browser: {browser.GetHashCode()}");
            var messages = browser.Cast<ITextMessage>().Select(m => m.Text).ToList();
            _output.WriteLine("[TEST] Messages in browser: " + string.Join(", ", messages));
            Assert.Contains("a", messages);
            Assert.Contains("b", messages);
        }

        [Fact]
        public void Browser_With_Selector_Enumerates_Only_Matching()
        {
            _output.WriteLine("[TEST] Starting Browser_With_Selector_Enumerates_Only_Matching");
            var context = new FakeNMSContext();
            _output.WriteLine($"[TEST] Created FakeNMSContext: {context.GetHashCode()}");
            var queue = context.GetQueue("browse-selector-q");
            _output.WriteLine($"[TEST] Got queue: {queue}, Hash: {queue.GetHashCode()}");
            var producer = context.CreateProducer();
            _output.WriteLine($"[TEST] Created producer: {producer.GetHashCode()}");
            producer.Send(queue, producer.CreateTextMessage("foo"));
            _output.WriteLine("[TEST] Sent message: foo");
            producer.Send(queue, producer.CreateTextMessage("bar"));
            _output.WriteLine("[TEST] Sent message: bar");
            var browser = context.CreateBrowser(queue, "foo");
            _output.WriteLine($"[TEST] Created browser with selector: {browser.GetHashCode()}");
            var messages = browser.Cast<ITextMessage>().Select(m => m.Text).ToList();
            _output.WriteLine("[TEST] Messages in browser with selector: " + string.Join(", ", messages));
            Assert.Contains("foo", messages);
            Assert.DoesNotContain("bar", messages);
        }
    }
}
