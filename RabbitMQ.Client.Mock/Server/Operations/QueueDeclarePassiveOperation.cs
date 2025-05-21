using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

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
            var queueInstance = Server.Queues.TryGetValue(queue, out var x) ? x : null;
            if (queueInstance is null)
            {
                return ValueTask.FromResult(OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Queue '{queue}' not found."))));
            }

            var messageCount = queueInstance.MessageCount;
            var consumerCount = queueInstance.ConsumerCount;
            var result = new QueueDeclareOk(queue, messageCount, consumerCount);
            return ValueTask.FromResult(OperationResult.Success($"Queue '{queue}' exists.", result));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult(OperationResult.Failure(ex));
        }
    }
}
