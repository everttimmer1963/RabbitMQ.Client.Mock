
using RabbitMQ.Client.Events;
using System.Diagnostics;

namespace RabbitMQ.Client.Mock.Server.Operations;

internal class BasicAckOperation(IRabbitServer server, FakeChannel channel, ulong deliveryTag, bool multiple) : Operation(server)
{
    public override bool IsValid => (Server is not null);

    public override async ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            if(!IsValid)
            {
                return OperationResult.Failure(new InvalidOperationException("Server is required."));
            }

            // get the pending message(s)
            // if multiple is specified we need to ack all messages up to, and including, the delivery tag
            var messagesToAck = multiple
                ? Server.PendingConfirms.Where(pc => pc.Value.ChannelNumber == channel.ChannelNumber && pc.Value.DeliveryTag <= deliveryTag).Select(pc => pc.Key).ToArray()
                : Server.PendingConfirms.Where(pc => pc.Value.ChannelNumber == channel.ChannelNumber && pc.Value.DeliveryTag == deliveryTag).Select(pc => pc.Key).ToArray();

            // since an acknowledment means that the message was processed successfully, there is no need to
            // kee it on the server. Since it has already been removed from the original queue, deleting the
            // pending confirm is the only thing we need to do.
            foreach (var key in messagesToAck)
            {
                var pc = Server.PendingConfirms[key];
                await channel.HandleBasicAckAsync(new BasicAckEventArgs(pc.DeliveryTag, multiple));
                Server.PendingConfirms.Remove(key);
            }

            // return success.
            return OperationResult.Success($"Acknowledged {messagesToAck.Length} message(s) on channel '{channel.ChannelNumber}'.");
        }
        catch (Exception ex)
        {
            return OperationResult.Failure(ex);
        }
    }
}
