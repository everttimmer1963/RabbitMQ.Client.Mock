using RabbitMQ.Client.Mock.Server.Data;
using RabbitMQ.Client.Mock.Server.Operations;

namespace RabbitMQ.Client.Mock.Server.Exchanges;
internal class DirectExchange(IRabbitServer server, string name) : RabbitExchange(server, name, ExchangeType.Direct)
{
    #region Exchange Bindings
    public override async ValueTask ExchangeBindAsync(string exchange, string routingKey, IDictionary<string, object?>? arguments = null, bool noWait = false, CancellationToken cancellationToken = default)
    {
        var operation = new ExchangeBindOperation(Server, this, exchange, routingKey, arguments);

        // fire-and-forget ?
        if (noWait)
        {
            await Server.Processor.EnqueueOperationAsync(operation, async (result) =>
            {
                await Task.Run(() => Console.WriteLine($"Operation: {GetType().Name} - {result.Message}"));
            });
            Console.WriteLine($"Operation: {operation.OperationId.ToString()} is queued for processing.");
            return;
        }

        // or wait until the operation is done.
        await Server.Processor.EnqueueOperationAsyncWithWait(operation, cancellationToken);
    }

    public override async ValueTask ExchangeUnbindAsync(string exchange, string routingKey, bool noWait = false, CancellationToken cancellationToken = default)
    {
        var operation = new ExchangeUnbindOperation(Server, this, exchange, routingKey);

        // fire-and-forget ?
        if (noWait)
        {
            await Server.Processor.EnqueueOperationAsync(operation, async (result) =>
            {
                await Task.Run(() => Console.WriteLine($"Operation: {GetType().Name} - {result.Message}"));
            });
            Console.WriteLine($"Operation: {operation.OperationId.ToString()} is queued for processing.");
            return;
        }

        // or wait until the operation is done.
        await Server.Processor.EnqueueOperationAsyncWithWait(operation, cancellationToken);
    }
    #endregion

    #region Queue Bindings
    public override async ValueTask QueueBindAsync(string queue, string bindingKey, IDictionary<string, object?>? arguments = null, bool noWait = false, CancellationToken cancellationToken = default)
    {
        var operation = new QueueBindOperation(Server, this, queue, bindingKey, arguments);

        // fire-and-forget ?
        if (noWait)
        {
            await Server.Processor.EnqueueOperationAsync(operation, async (result) =>
            {
                await Task.Run(() => Console.WriteLine($"Operation: {GetType().Name} - {result.Message}"));
            });
            Console.WriteLine($"Operation: {operation.OperationId.ToString()} is queued for processing.");
            return;
        }

        // or wait until the operation is done.
        await Server.Processor.EnqueueOperationAsyncWithWait(operation, cancellationToken);
    }

    public override async ValueTask QueueUnbindAsync(string queue, string bindingKey, bool noWait = false, CancellationToken cancellationToken = default)
    {
        var operation = new QueueUnbindOperation(Server, this, queue, bindingKey);

        // fire-and-forget ?
        if (noWait)
        {
            await Server.Processor.EnqueueOperationAsync(operation, async (result) =>
            {
                await Task.Run(() => Console.WriteLine($"Operation: {GetType().Name} - {result.Message}"));
            });
            Console.WriteLine($"Operation: {operation.OperationId.ToString()} is queued for processing.");
            return;
        }

        // or wait until the operation is done.
        await Server.Processor.EnqueueOperationAsyncWithWait(operation, cancellationToken);
    }
    #endregion

    public override async ValueTask PublishMessageAsync(string routingKey, RabbitMessage message)
    {
        var operation = new PublishMessageOperation(Server, this, routingKey, message);
        await Server.Processor.EnqueueOperationAsync(operation, async (result) =>
        {
            await Task.Run(() => Console.WriteLine($"Operation: {GetType().Name} - {result.Message}"));
        });
        Console.WriteLine($"Operation: {operation.OperationId.ToString()} is queued for processing.");
    }
}
