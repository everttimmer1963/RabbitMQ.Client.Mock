using RabbitMQ.Client.Mock.Server.Data;
using RabbitMQ.Client.Mock.Server.Operations;
using System.Threading;

namespace RabbitMQ.Client.Mock.Server.Exchanges;
internal class DirectExchange(IRabbitServer server, string name) : RabbitExchange(server, name, ExchangeType.Direct)
{
    #region Exchange Bindings
    public override async ValueTask ExchangeBindAsync(string exchange, string routingKey, IDictionary<string, object?>? arguments = null, bool noWait = false, CancellationToken cancellationToken = default)
    {
        // create the operation
        var operation = new ExchangeBindOperation(Server, this, exchange, routingKey, arguments);

        // and queue the operation for processing
        await Server.Processor.EnqueueOperationAsync(operation, noWait, true, cancellationToken: cancellationToken);
        if (noWait)
        {
            Console.WriteLine($"Operation: {operation.OperationId.ToString()} is queued for processing.");
        }
    }

    public override async ValueTask ExchangeUnbindAsync(string exchange, string routingKey, bool noWait = false, CancellationToken cancellationToken = default)
    {
        // create the operation
        var operation = new ExchangeUnbindOperation(Server, this, exchange, routingKey);

        // and queue the operation for processing
        await Server.Processor.EnqueueOperationAsync(operation, noWait, true, cancellationToken: cancellationToken);
        if (noWait)
        {
            Console.WriteLine($"Operation: {operation.OperationId.ToString()} is queued for processing.");
        }
    }
    #endregion

    #region Queue Bindings
    public override async ValueTask QueueBindAsync(string queue, string bindingKey, IDictionary<string, object?>? arguments = null, bool noWait = false, CancellationToken cancellationToken = default)
    {
        // create the operation
        var operation = new QueueBindOperation(Server, this, queue, bindingKey, arguments);

        // and queue the operation for processing
        await Server.Processor.EnqueueOperationAsync(operation, noWait, true, cancellationToken: cancellationToken);
        if (noWait)
        {
            Console.WriteLine($"Operation: {operation.OperationId.ToString()} is queued for processing.");
        }
    }

    public override async ValueTask QueueUnbindAsync(string queue, string bindingKey, bool noWait = false, CancellationToken cancellationToken = default)
    {
        // create the operation
        var operation = new QueueUnbindOperation(Server, this, queue, bindingKey);

        // and queue the operation for processing
        await Server.Processor.EnqueueOperationAsync(operation, noWait, true, cancellationToken: cancellationToken);
        if (noWait)
        {
            Console.WriteLine($"Operation: {operation.OperationId.ToString()} is queued for processing.");
        }
    }
    #endregion

    public override async ValueTask PublishMessageAsync(string routingKey, RabbitMessage message)
    {
        // create the operation
        var operation = new PublishMessageOperation(Server, this, routingKey, message);

        // and queue the operation for processing
        await Server.Processor.EnqueueOperationAsync(operation, true, true);
        Console.WriteLine($"Operation: {operation.OperationId.ToString()} is queued for processing.");
    }
}
