
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace RabbitMQ.Client.Mock.Server.Operations;
internal class QueueDeleteOperation(IRabbitServer server, string queue, bool ifUnused, bool ifEmpty) : Operation<object>(server)
{
    public override bool IsValid => !(Server is null || string.IsNullOrWhiteSpace(queue));

    public override async ValueTask<OperationResult<object>> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        { 
            if(!IsValid)
            {
                return OperationResult.Failure<object>(new InvalidOperationException("Queue name is required."));
            }

            // check if the queue exists.
            var queueInstance = Server.Queues.TryGetValue(queue, out var q) ? q : null;
            if (queueInstance is null)
            {
                // if the queue does not exist, silently report a failure.
                return OperationResult.Failure<object>($"Queue '{queue}' not found.");
            }

            // make sure that we don't delete a non-empty queue if 'ifEmpty' is specified.
            if (ifEmpty && queueInstance.MessageCount > 0)
            {
                return OperationResult.Failure<object>(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Cannot delete queue '{queue}'. The queue is not empty.")));
            }

            // make sure that we don't delete a queue with consumers if 'ifUnused' is specified.
            if (ifUnused && queueInstance.ConsumerCount > 0)
            {
                return OperationResult.Failure<object>(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Cannot delete queue '{queue}'. The queue is in use.")));
            }

            // okay, we are good to go. first, we remove all bindings in which the queue is involved.
            var queueBindings = Server.QueueBindings.Where(qb => qb.Value.BoundQueues.ContainsKey(queue));
            foreach (var item in queueBindings)
            {
                var bindingKey = item.Key;
                var operation = new QueueUnbindOperation(Server, item.Value.Exchange, queue, bindingKey);
                await Server.Processor.EnqueueOperationAsync<object>(operation);
            }

            if (Server.Queues.Remove(queue))
            {
                // sucessfully removed the queue. remove all bindings where the queue
            }
        }
        catch (Exception ex)
        {
            return OperationResult.Failure(ex);
        }
    }
}
