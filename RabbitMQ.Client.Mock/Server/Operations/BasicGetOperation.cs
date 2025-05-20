
using RabbitMQ.Client.Mock.Server.Data;
using RabbitMQ.Client.Mock.Server.Queues;

namespace RabbitMQ.Client.Mock.Server.Operations;

internal class BasicGetOperation(IRabbitServer server, int channelNumber, string queue, bool autoAck) : Operation(server)
{
    public override bool IsValid => !(Server is null || string.IsNullOrEmpty(queue));

    public async override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            // get the queue.
            if (!Server.Queues.TryGetValue(queue, out var rabbitQueue))
            {
                return OperationResult.Warning($"Queue '{queue}' not found.");
            }

            // get the message, if any. If there is no message available at this time, return success and a null value.
            var message = await rabbitQueue.ConsumeMessageAsync(channelNumber, autoAck);
            if ( message is null )
            {
                return OperationResult.Success();
            }

            // pprepare and return the result
            var result = PrepareResult(rabbitQueue, message);
            return OperationResult.Success("Message returned", result);
        }
        catch (Exception ex)
        { 
            return OperationResult.Failure(ex);
        }
    }

    private BasicGetResult PrepareResult(RabbitQueue queue, RabbitMessage? message = null)
    {
        var messageCount = queue.MessageCount;
        return new BasicGetResult(
            deliveryTag: message.DeliveryTag,
            redelivered: message.Redelivered,
            exchange: message.Exchange,
            routingKey: message.RoutingKey,
            basicProperties: message.BasicProperties,
            body: message.Body,
            messageCount: messageCount
        );
    }
}
