
using RabbitMQ.Client.Events;

namespace RabbitMQ.Client.Mock.Server.Operations;

internal class BasicRejectOperation(IRabbitServer server, FakeChannel channel, ulong deliveryTag, bool requeue) : Operation(server)
{
    public override bool IsValid => !(server is null);

    public override async ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!IsValid)
            {
                return OperationResult.Warning("Server is not valid.");
            }

            // get the message(s) that need to be nacked.
            var pm = Server.PendingConfirms.Where(pm => pm.Key.Channel == channel.ChannelNumber & pm.Key.DeliveryTag == deliveryTag).Select(pm => pm.Value).FirstOrDefault();
            if (pm is null)
            {
                return OperationResult.Warning($"No message found for delivery tag {deliveryTag}.");
            }

            // get the queue.
            var queueInstance = Server.Queues.TryGetValue(pm.Message.Queue, out var q) ? q : default;

            // requeue the messages if requested.
            if (requeue)
            {
                if (queueInstance is null)
                {
                    return OperationResult.Warning($"Queue '{pm.Message.Queue}' not found.");
                }
                await queueInstance.RequeueMessageAsync(pm.Message).ConfigureAwait(false);
                pm.Message.Redelivered = true;
            }
            Server.PendingConfirms.Remove((channel.ChannelNumber, pm.Message.DeliveryTag));

            // retrieve deadletter information for delivery. if dead-letter information is available,
            // move the message to the dead-letter queue; otherwise, remove the message from pending
            // confirms, only.
            // get the queue that contained the message. if we cannot get the queue, discard the message.
            if (queueInstance is null)
            {
                Server.PendingConfirms.Remove((channel.ChannelNumber, pm.Message.DeliveryTag));
                return OperationResult.Warning($"Queue '{pm.Message.Queue}' not found.");
            }

            // try to get the dead-letter queue information. if we cannot get the dead-letter queue information,
            // remove the message from pending confirms only. (discard the message)
            if (queueInstance.TryGetDeadLetterQueueInfoAsync(out string? exchange, out string? routingKey))
            {
                if (string.IsNullOrEmpty(exchange) || string.IsNullOrEmpty(routingKey))
                {
                    Server.PendingConfirms.Remove((channel.ChannelNumber, pm.Message.DeliveryTag));
                    return OperationResult.Warning($"No dead-letter information is available.");
                }

                // get the exchange that will receive the message. if we cannot get the exchange, discard the message.
                var exchangeInstance = Server.Exchanges.TryGetValue(exchange, out var xchg) ? xchg : default;
                if (exchangeInstance is null)
                {
                    Server.PendingConfirms.Remove((channel.ChannelNumber, pm.Message.DeliveryTag));
                    return OperationResult.Warning($"The dead-letter exchange does not exist.");
                }

                // publish the message to the dead-letter exchange and remve the message from pending confirms.
                await exchangeInstance.PublishMessageAsync(routingKey, pm.Message).ConfigureAwait(false);
                Server.PendingConfirms.Remove((channel.ChannelNumber, pm.Message.DeliveryTag));

                return OperationResult.Success($"Operation cmompleted succesfully. The message has been dead-lettered.");
            }
            else
            {
                // no dead-letter queue information available. remove the message from pending confirms.
                Server.PendingConfirms.Remove((channel.ChannelNumber, pm.Message.DeliveryTag));
                return OperationResult.Success($"Operation cmompleted succesfully. The message has been discarded.");
            }
        }
        catch (Exception ex)
        {
            return OperationResult.Failure(ex);
        }
    }
}
