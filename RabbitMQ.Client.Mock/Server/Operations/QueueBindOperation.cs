using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Mock.Server.Bindings;

namespace RabbitMQ.Client.Mock.Server.Operations;
internal class QueueBindOperation(IRabbitServer server, string exchange, string queue, string bindingKey, IDictionary<string, object?>? arguments = null) : Operation(server)
{
    public override bool IsValid => !(Server is null || string.IsNullOrWhiteSpace(queue));

    public override async ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            if(!IsValid)
            {
                return OperationResult.Failure(new InvalidOperationException("Exchange, Queue and BindingKey are required."));
            }

            // We cannot bind a queue to the default exchange so check it.
            if (exchange == string.Empty)
            {
                return OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"The default exchange cannot be used as the exchange in a queue binding.")));
            }

            // get the exchange to bind to.
            var exchangeToBind = Server.Exchanges.TryGetValue(exchange, out var x) ? x : null;
            if (exchangeToBind is null)
            {
                return OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Exchange '{exchange}' not found.")));
            }

            // get the queue to bind.
            var queueToBind = Server.Queues.TryGetValue(queue, out var q) ? q : null;
            if (queueToBind is null)
            {
                return OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Queue '{queue}' not found.")));
            }

            return await exchangeToBind.QueueBindAsync(queueToBind, arguments, bindingKey);
        }
        catch (Exception ex)
        {
            return OperationResult.Failure(ex);
        }
    }
}
