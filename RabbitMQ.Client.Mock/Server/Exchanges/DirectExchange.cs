using RabbitMQ.Client.Mock.Server.Data;
using RabbitMQ.Client.Mock.Server.Operations;
using System.Threading;

namespace RabbitMQ.Client.Mock.Server.Exchanges;
internal class DirectExchange(IRabbitServer server, string name) : RabbitExchange(server, name, ExchangeType.Direct)
{
    public override async ValueTask PublishMessageAsync(string routingKey, RabbitMessage message)
    {
        // create the operation
        var operation = new PublishMessageOperation(Server, name, routingKey, message);

        // and queue the operation for processing
        await Server.Processor.EnqueueOperationAsync(operation, true, true);
        Console.WriteLine($"Operation: {operation.OperationId.ToString()} is queued for processing.");
    }
}
