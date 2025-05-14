using RabbitMQ.Client.Mock.Domain;
using RabbitMQ.Client.Mock.Server.Operations;

namespace RabbitMQ.Client.Mock.Server.Exchanges;
internal class DirectExchange(IRabbitServer server, string name, string type) : RabbitExchange(server, name, type)
{
    public override async ValueTask BindExchangeAsync(string exchange, string routingKey, IDictionary<string, object?>? arguments = null)
    {
        var operation = new BindExchangeOperation(Server, this, exchange, routingKey)
        {
            Arguments = arguments
        };
        await Server.Processor.EnqueueOperationAsync(operation, async (result) =>
        {
            Console.WriteLine($"Operation: {GetType().Name} - {result.Message}");
        });
        Console.WriteLine($"Operation: {operation.OperationId.ToString()} is queued for pprocessing.");
    }

    public override ValueTask BindQueueAsync(string queue, string bindingKey, IDictionary<string, object?>? arguments = null)
    {
        throw new NotImplementedException();
    }

    public override ValueTask PublishMessageAsync(string routingKey, RabbitMessage message)
    {
        throw new NotImplementedException();
    }

    public override ValueTask UnbindExchangeAsync(string exchange, string routingKey)
    {
        throw new NotImplementedException();
    }

    public override ValueTask UnbindQueueAsync(string queue, string bindingKey)
    {
        throw new NotImplementedException();
    }
}
