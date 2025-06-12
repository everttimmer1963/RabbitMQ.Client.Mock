using System.Threading.Tasks;
using Xunit;
using Apache.NMS.ActiveMQ.Mock;

namespace Apache.NMS.ActiveMQ.Mock.Tests
{
    public class ActiveMQMockerTests
    {
        [Fact]
        public void CreateConnectionFactory_ReturnsFactoryInstance()
        {
            var factory = ActiveMQMocker.CreateConnectionFactory();
            Assert.NotNull(factory);
        }

        [Fact]
        public async Task CreateConnectionFactoryAsync_ReturnsFactoryInstance()
        {
            var factory = await ActiveMQMocker.CreateConnectionFactoryAsync();
            Assert.NotNull(factory);
        }
    }
}
