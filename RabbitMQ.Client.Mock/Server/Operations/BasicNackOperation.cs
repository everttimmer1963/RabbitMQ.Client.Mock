using RabbitMQ.Client.Events;

namespace RabbitMQ.Client.Mock.Server.Operations;

internal class BasicNackOperation(IRabbitServer server, FakeChannel channel, ulong deliveryTag, bool multiple, bool requeue) : Operation(server)
{
    public override bool IsValid => Server is not null;

    public override async ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        { 
            if(!IsValid)
            {
                return OperationResult.Warning("Server is not valid.");
            }

            // get the message(s) that need to be nacked.
            var messages = multiple
                ? Server.PendingConfirms.Where(pm => pm.Key.Channel == channel.ChannelNumber & pm.Key.DeliveryTag <= deliveryTag).Select(pm => pm.Value).ToArray()
                : Server.PendingConfirms.Where(pm => pm.Key.Channel == channel.ChannelNumber & pm.Key.DeliveryTag == deliveryTag).Select(pm => pm.Value).ToArray();

            if (messages.Length == 0)
            {
                return OperationResult.Warning($"No messages found for delivery tag {deliveryTag}.");
            }

            // requeue the messages if requested.
            if (requeue)
            {
                foreach (var pm in messages)
                {
                    var queue = Server.Queues.TryGetValue(pm.Message.Queue, out var q) ? q : default;
                    if (queue is null)
                    {
                        return OperationResult.Warning($"Queue '{pm.Message.Queue}' not found.");
                    }
                    await queue.RequeueMessageAsync(pm.Message).ConfigureAwait(false);
                    pm.Message.Redelivered = true;
                    Server.PendingConfirms.Remove((channel.ChannelNumber, pm.Message.DeliveryTag));
                    await channel.HandleBasicNackAsync(new BasicNackEventArgs(pm.Message.DeliveryTag, multiple, true));
                }
                return OperationResult.Success($"Operation completed succesfully. {messages.Length} messages were re-queued.");
            }

            int discarded = 0;
            int deadlettered = 0;

            // retrieve deadletter information for delivery. if dead-letter information is available,
            // move the message to the dead-letter queue; otherwise, remove the message from pending
            // confirms, only.
            foreach (var pm in messages)
            {
                // get the queue that contained the message. if we cannot get the queue, discard the message.
                var queue = Server.Queues.TryGetValue(pm.Message.Queue, out var q) ? q : default;
                if (queue is null)
                {
                    discarded++;
                    Server.PendingConfirms.Remove((channel.ChannelNumber, pm.Message.DeliveryTag));
                    await channel.HandleBasicNackAsync(new BasicNackEventArgs(pm.Message.DeliveryTag, multiple, false));
                    continue;
                }

                // try to get the dead-letter queue information. if we cannot get the dead-letter queue information,
                // remove the message from pending confirms only. (discard the message)
                if (queue.TryGetDeadLetterQueueInfoAsync(out string? exchange, out string? routingKey))
                {
                    if(string.IsNullOrEmpty(exchange) || string.IsNullOrEmpty(routingKey))
                    {
                        discarded++;
                        Server.PendingConfirms.Remove((channel.ChannelNumber, pm.Message.DeliveryTag));
                        await channel.HandleBasicNackAsync(new BasicNackEventArgs(pm.Message.DeliveryTag, multiple, false)).ConfigureAwait(false);
                        continue;
                    }

                    // get the exchange that will receive the message. if we cannot get the exchange, discard the message.
                    var exchangeInstance = Server.Exchanges.TryGetValue(exchange, out var xchg) ? xchg : default;
                    if (exchangeInstance is null)
                    {
                        discarded++;
                        Server.PendingConfirms.Remove((channel.ChannelNumber, pm.Message.DeliveryTag));
                        await channel.HandleBasicNackAsync(new BasicNackEventArgs(pm.Message.DeliveryTag, multiple, false)).ConfigureAwait(false);
                        continue;
                    }

                    // publish the message to the dead-letter exchange and remve the message from pending confirms.
                    await exchangeInstance.PublishMessageAsync(routingKey, pm.Message).ConfigureAwait(false);
                    Server.PendingConfirms.Remove((channel.ChannelNumber, pm.Message.DeliveryTag));
                    await channel.HandleBasicNackAsync(new BasicNackEventArgs(pm.Message.DeliveryTag, multiple, false)).ConfigureAwait(false);
                    deadlettered++;
                }
                else
                {
                    // no dead-letter queue information available. remove the message from pending confirms.
                    discarded++;
                    Server.PendingConfirms.Remove((channel.ChannelNumber, pm.Message.DeliveryTag));
                    await channel.HandleBasicNackAsync(new BasicNackEventArgs(pm.Message.DeliveryTag, multiple, false)).ConfigureAwait(false);
                }
            }

            // report success.
            return OperationResult.Success($"Operation cmompleted succesfully. {deadlettered} messages were dead-lettered and {discarded} messages were discarded.");
        }
        catch (Exception ex)
        {
            return OperationResult.Failure(ex);
        }
    }
}
