using Xunit;
using Apache.NMS.ActiveMQ.Mock;
using Apache.NMS.ActiveMQ.Mock.Server;
using System.Linq;

namespace Apache.NMS.ActiveMQ.Mock.Tests
{
    public class FakeQueueBrowserTests
    {
        [Fact]
        public void GetEnumerator_ReturnsAllMessages()
        {
            var server = FakeActiveMQServer.Instance;
            var queueName = "browser-q";
            server.CreateQueue(queueName);
            server.EnqueueMessage(queueName, new FakeMessage { Body = "a", Destination = queueName });
            server.EnqueueMessage(queueName, new FakeMessage { Body = "b", Destination = queueName });
            var browser = new FakeQueueBrowser(new FakeQueue(queueName), null);
            var messages = browser.Cast<ITextMessage>().Select(m => m.Text).ToList();
            Assert.Contains("a", messages);
            Assert.Contains("b", messages);
        }

        [Fact]
        public void GetEnumerator_WithSelector_ReturnsMatchingMessages()
        {
            var server = FakeActiveMQServer.Instance;
            var queueName = "browser-selector-q";
            server.CreateQueue(queueName);
            server.EnqueueMessage(queueName, new FakeMessage { Body = "foo", Destination = queueName });
            server.EnqueueMessage(queueName, new FakeMessage { Body = "bar", Destination = queueName });
            var browser = new FakeQueueBrowser(new FakeQueue(queueName), "foo");
            var messages = browser.Cast<ITextMessage>().Select(m => m.Text).ToList();
            Assert.Contains("foo", messages);
            Assert.DoesNotContain("bar", messages);
        }

        [Fact]
        public void GetEnumerator_EmptyQueue_ReturnsEmpty()
        {
            var server = FakeActiveMQServer.Instance;
            var queueName = "empty-browser-q";
            server.CreateQueue(queueName);
            var browser = new FakeQueueBrowser(new FakeQueue(queueName), null);
            var messages = browser.Cast<ITextMessage>().ToList();
            Assert.Empty(messages);
        }

        [Fact]
        public void Close_And_Dispose_DoesNotThrow()
        {
            var browser = new FakeQueueBrowser(new FakeQueue("q"), null);
            browser.Close();
            browser.Dispose();
        }
    }
}
