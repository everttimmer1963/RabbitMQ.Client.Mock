namespace RabbitMQ.Client.Mock.Server.Operations;

internal class BasicNackAsyncOperation(IRabbitServer server, int channelNumber, ulong deliveryTag, bool multiple, bool requeue) : Operation(server)
{
    public override bool IsValid => Server is not null;

    public override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        { 
            if(!IsValid)
            {
                return OperationResult.Warning("Server is not valid.");
            }

            // get the message(s) that need to be nacked.
            var 
        }
        catch (Exception ex)
        {
            return OperationResult.Failure(ex);
        }
    }
}
