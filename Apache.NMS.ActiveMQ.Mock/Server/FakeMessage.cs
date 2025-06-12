using System.Diagnostics.CodeAnalysis;

namespace Apache.NMS.ActiveMQ.Mock.Server
{
    [ExcludeFromCodeCoverage]
    public class FakeMessage
    {
        public string Body { get; set; }
        public string Destination { get; set; }
        public int RedeliveryCount { get; set; } = 0;
        public bool Acknowledged { get; set; } = false;
        // Add more properties as needed (headers, properties, etc.)
    }
}
