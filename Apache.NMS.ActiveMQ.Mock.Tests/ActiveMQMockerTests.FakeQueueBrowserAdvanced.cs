using Xunit;
using Apache.NMS.ActiveMQ.Mock;

namespace Apache.NMS.ActiveMQ.Mock.Tests
{
    public class FakeQueueBrowserAdvancedTests
    {
        [Fact]
        public void Close_And_Dispose_DoesNotThrow()
        {
            var context = new FakeNMSContext();
            var queue = context.GetQueue("bq");
            var browser = context.CreateBrowser(queue);
            browser.Close();
            browser.Dispose();
        }
    }
}
