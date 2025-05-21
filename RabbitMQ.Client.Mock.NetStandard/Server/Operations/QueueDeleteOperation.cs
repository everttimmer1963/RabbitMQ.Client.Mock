
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Operations
{
    internal class QueueDeleteOperation(IRabbitServer server, string queue, bool ifUnused, bool ifEmpty) : Operation(server)
    {
        public override bool IsValid => !(Server is null || string.IsNullOrWhiteSpace(queue));

        public override async ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!IsValid)
                {
                    return OperationResult.Failure(new InvalidOperationException("Queue name is required."));
                }

                // check if the queue exists.
                var queueInstance = Server.Queues.TryGetValue(queue, out var q) ? q : null;
                if (queueInstance is null)
                {
                    // if the queue does not exist, silently report a failure.
                    return OperationResult.Warning($"Queue '{queue}' not found.");
                }

                // make sure that we don't delete a non-empty queue if 'ifEmpty' is specified.
                if (ifEmpty && queueInstance.MessageCount > 0)
                {
                    return OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Cannot delete queue '{queue}'. The queue is not empty.")));
                }

                // make sure that we don't delete a queue with consumers if 'ifUnused' is specified.
                if (ifUnused && queueInstance.ConsumerCount > 0)
                {
                    return OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Cannot delete queue '{queue}'. The queue is in use.")));
                }

                // okay, we are good to go. first, we remove all bindings in which the queue is involved.
                var queueBindings = Server.QueueBindings.Where(qb => qb.Value.BoundQueues.ContainsKey(queue));
                foreach (var item in queueBindings)
                {
                    var bindingKey = item.Key;
                    var operation = new QueueUnbindOperation(Server, item.Value.Exchange.Name, queue, bindingKey);
                    await Server.Processor.EnqueueOperationAsync(operation, true).ConfigureAwait(false);
                }

                // remove the queue from the server. if remove returns false, the queue may have been removed by another thread.
                // in that case, we return a success message.
                if (!Server.Queues.Remove(queue))
                {
                    return OperationResult.Warning($"Queue '{queue}' not found.");
                }

                // 8return success
                return OperationResult.Success($"Queue '{queue}' deleted successfully.");
            }
            catch (Exception ex)
            {
                return OperationResult.Failure(ex);
            }
        }
    }
}