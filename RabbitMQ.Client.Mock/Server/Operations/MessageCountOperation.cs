
namespace RabbitMQ.Client.Mock.Server.Operations;

internal class MessageCountOperation(IRabbitServer server, string queue) : Operation(server)
{
    public override bool IsValid => !(Server is null || string.IsNullOrEmpty(queue));

    public override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        { 
            if(!IsValid)
            {
                return ValueTask.FromResult(OperationResult.Failure(new ArgumentException("Either Server or queue is null or empty.")));
            }

            // count the number of messages in the queue
            var count = Convert.ToUInt64(Server.Queues.TryGetValue(queue, out var q) ? q.MessageCount : 0);

            return ValueTask.FromResult(OperationResult.Success(result: count));
        }
        catch(Exception ex)
        {
            return ValueTask.FromResult(OperationResult.Failure(ex));
        }
    }
}
