
using System.Diagnostics;

namespace RabbitMQ.Client.Mock.Server.Operations;

internal class BasicAckAsyncOperation(IRabbitServer server, int channelNumber, ulong deliveryTag, bool multiple) : Operation(server)
{
    public override bool IsValid => (Server is not null);

    public override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            if(!IsValid)
            {
                return ValueTask.FromResult(OperationResult.Failure(new InvalidOperationException("Server is required.")));
            }

            // check if the channel exists
            if (!Server.Channels.TryGetValue(channelNumber, out var channel))
            {
                return ValueTask.FromResult(OperationResult.Failure(new InvalidOperationException($"Channel '{channelNumber}' not found.")));
            }

            // get the pending message(s)
            // if multiple is specified we need to ack all messages up to, and including, the delivery tag
            var messagesToAck = multiple
                ? Server.PendingConfirms.Where(pc => pc.Value.ChannelNumber == channelNumber && pc.Value.DeliveryTag <= deliveryTag).Select(pc => pc.Key).ToArray()
                : Server.PendingConfirms.Where(pc => pc.Value.ChannelNumber == channelNumber && pc.Value.DeliveryTag == deliveryTag).Select(pc => pc.Key).ToArray();

            // since an acknowledment means that the message was processed successfully, there is no need to
            // kee it on the server. Since it has already been removed from the original queue, deleting the
            // pending confirm is the only thing we need to do.
            foreach (var key in messagesToAck)
            {
                Server.PendingConfirms.Remove(key);
            }

            // return success.
            return ValueTask.FromResult(OperationResult.Success($"Acknowledged {messagesToAck.Length} message(s) on channel '{channelNumber}'."));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult(OperationResult.Failure(ex));
        }
    }
}
