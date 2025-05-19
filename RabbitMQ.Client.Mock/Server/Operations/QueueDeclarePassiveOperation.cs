using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Mock.Domain;

namespace RabbitMQ.Client.Mock.Server.Operations;

internal class QueueDeclarePassiveOperation(IRabbitServer server, string queue) : Operation(server)
{
    public override bool IsValid => !(Server is null || string.IsNullOrEmpty(queue));

    public override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            // check if all information is provided
            if (!IsValid)
            {
                return ValueTask.FromResult(OperationResult.Failure(new InvalidOperationException("Queue is required.")));
            }

            // get the exchange to bind to
            var xchg = Server.Queues.TryGetValue(queue, out var x) ? x : null;
            if (xchg is null)
            {
                return ValueTask.FromResult(OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Exchange '{exchange}' not found."))));
            }

            return ValueTask.FromResult(OperationResult.Success($"Exchange '{queue}' exists."));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult(OperationResult.Failure(ex));
        }
    }
}
