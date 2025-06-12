using System.Threading.Tasks;
using Xunit;
using Apache.NMS.ActiveMQ.Mock;

namespace Apache.NMS.ActiveMQ.Mock.Tests
{
    public class FakeConnectionFactoryTests
    {
        [Fact]
        public void CreateConnection_ReturnsConnection()
        {
            var factory = new FakeConnectionFactory();
            var connection = factory.CreateConnection();
            Assert.NotNull(connection);
        }

        [Fact]
        public async Task CreateConnectionAsync_ReturnsConnection()
        {
            var factory = new FakeConnectionFactory();
            var connection = await factory.CreateConnectionAsync();
            Assert.NotNull(connection);
        }

        [Fact]
        public void CreateContext_ReturnsContext()
        {
            var factory = new FakeConnectionFactory();
            var context = factory.CreateContext();
            Assert.NotNull(context);
        }
    }
}
