using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Mock.Server.Bindings;

namespace RabbitMQ.Client.Mock.Server.Operations;

internal class BasicConsumeOperation(IRabbitServer server, IChannel channel, string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object?>? arguments, IAsyncBasicConsumer consumer) : Operation(server)
{
    public override bool IsValid => !(Server is null || string.IsNullOrEmpty(queue) || consumer is null);

    public override async ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!IsValid)
            {
                return OperationResult.Failure(new InvalidOperationException("Queue and Consumer are required."));
            }

            // get the queue for which to add a consumer.
            var queueInstance = Server.Queues.Values.FirstOrDefault(q => q.Name == queue);
            if (queueInstance == null)
            {
                return OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Queue '{queue}' not found.")));
            }

            // check if the consumer already exists.
            var exists = Server.ConsumerBindings.ContainsKey(consumerTag);
            if (exists)
            {
                return OperationResult.Warning($"A consumer with tag '{consumerTag}' already exists.");
            }

            // if exclusive is true, we need to check if there are any other consumers on the queue.
            if (exclusive && queueInstance.ConsumerCount > 0)
            {
                return OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Queue '{queue}' already has a consumer.")));
            }

            // do we need to create a server provided consumertag?
            if (string.IsNullOrWhiteSpace(consumerTag))
            {
                consumerTag = await Server.GenerateUniqueConsumerTag(queue).ConfigureAwait(false);
            }

            // now, add the new consumer binding, and notify the queue of its existence.
            var binding = new ConsumerBinding(queueInstance, consumer)
            {
                ChannelNumber = channel.ChannelNumber,
                AutoAcknowledge = autoAck,
                Arguments = arguments,
                NoLocal = noLocal,
                Exclusive = exclusive,
            };

            Server.ConsumerBindings.TryAdd(consumerTag, binding);
            await queueInstance.NotifyConsumerAdded();

            return OperationResult.Success($"Consumer '{consumerTag}' added to queue '{queue}' with autoAck={autoAck}, noLocal={noLocal}, exclusive={exclusive}.", consumerTag);
        }
        catch (Exception ex)
        {
            return OperationResult.Failure(ex);
        }
    }
}
