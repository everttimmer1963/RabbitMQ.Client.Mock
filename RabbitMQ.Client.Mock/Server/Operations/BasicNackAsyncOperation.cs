namespace RabbitMQ.Client.Mock.Server.Operations;

internal class BasicNackAsyncOperation(IRabbitServer server, int channelNumber, ulong deliveryTag, bool multiple, bool requeue) : Operation(server)
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
                ? Server.PendingConfirms.Where(pm => pm.Key.Channel == channelNumber & pm.Key.DeliveryTag <= deliveryTag).Select(pm => pm.Value).ToArray()
                : Server.PendingConfirms.Where(pm => pm.Key.Channel == channelNumber & pm.Key.DeliveryTag == deliveryTag).Select(pm => pm.Value).ToArray();

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
                    Server.PendingConfirms.Remove((channelNumber, pm.Message.DeliveryTag));
                }
                return OperationResult.Success($"{messages.Length} meesages re-queued.");
            }

            // retrieve deadletter information for delivery. if dead-letter information is available,
            // move the message to the dead-letter queue; otherwise, remove the message from pending
            // confirms, only.
            foreach (var pm in messages)
            {
                var queue = Server.Queues.TryGetValue(pm.Message.Queue, out var q) ? q : default;
                if (queue is null)
                {
                    Server.PendingConfirms.Remove((channelNumber, pm.Message.DeliveryTag));
                    continue;
                }
                if (queue.TryGetDeadLetterQueueInfoAsync(out string? exchange, out string? routingKey))
                {
                    var exchangeInstance = Server.Exchanges.TryGetValue(exchange, out var xchg) ? xchg : default;
                    if (exchangeInstance is null)
                    {
                        return OperationResult.Warning($"DeadLetter exchange '{exchange}' not found.");
                    }
                    await exchangeInstance.PublishMessageAsync(routingKey, pm.Message).ConfigureAwait(false);
                    Server.PendingConfirms.Remove((channelNumber, pm.Message.DeliveryTag));
                }
                else
                {
                    Server.PendingConfirms.Remove((channelNumber, pm.Message.DeliveryTag));
                }
                return OperationResult.Success($"{messages.Length} meesages dead-lettered.");
            }

            // this should never occur.
            return OperationResult.Failure(new InvalidOperationException($"No idea what the funk has gone wrong here."));
        }
        catch (Exception ex)
        {
            return OperationResult.Failure(ex);
        }
    }
}
