using Xunit;
using Apache.NMS.ActiveMQ.Mock;

namespace Apache.NMS.ActiveMQ.Mock.Tests
{
    public class FakeNMSContextAdvancedTests
    {
        [Fact]
        public void TemporaryQueue_And_Topic_Creation_And_Deletion()
        {
            var context = new FakeNMSContext();
            var tempQueue = context.CreateTemporaryQueue();
            var tempTopic = context.CreateTemporaryTopic();
            Assert.NotNull(tempQueue);
            Assert.NotNull(tempTopic);
            tempQueue.Delete();
            tempTopic.Delete();
        }

        [Fact]
        public void DeleteDestination_DoesNotThrow()
        {
            var context = new FakeNMSContext();
            var queue = context.GetQueue("del-q");
            context.DeleteDestination(queue);
        }

        [Fact]
        public void PurgeTempDestinations_DoesNotThrow()
        {
            var context = new FakeNMSContext();
            context.PurgeTempDestinations();
        }

        [Fact]
        public void Commit_And_Rollback_DoesNotThrow()
        {
            var context = new FakeNMSContext();
            context.Commit();
            context.Rollback();
        }

        [Fact]
        public void Acknowledge_DoesNotThrow()
        {
            var context = new FakeNMSContext();
            context.Acknowledge();
        }

        [Fact]
        public void Close_And_Dispose_DoesNotThrow()
        {
            var context = new FakeNMSContext();
            context.Close();
            context.Dispose();
        }

        [Fact]
        public void Start_And_Stop_DoesNotThrow()
        {
            var context = new FakeNMSContext();
            context.Start();
            context.Stop();
        }
    }
}
