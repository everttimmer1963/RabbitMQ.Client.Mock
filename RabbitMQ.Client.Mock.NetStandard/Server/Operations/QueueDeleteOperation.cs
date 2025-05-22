
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Operations
{
    internal class QueueDeleteOperation : Operation
    {
        readonly string _queue;
        readonly bool _ifUnused;
        readonly bool _ifEmpty;

        public QueueDeleteOperation(IRabbitServer server, string queue, bool ifUnused, bool ifEmpty)
            : base(server)
        {
            this._queue = queue ?? throw new ArgumentNullException(nameof(queue));
            this._ifUnused = ifUnused;
            this._ifEmpty = ifEmpty;
        }

        public override bool IsValid => !(Server is null || string.IsNullOrWhiteSpace(_queue));

        public override async ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!IsValid)
                {
                    return OperationResult.Failure(new InvalidOperationException("Queue name is required."));
                }

                // check if the queue exists.
                var queueInstance = Server.Queues.TryGetValue(_queue, out var q) ? q : null;
                if (queueInstance is null)
                {
                    // if the queue does not exist, silently report a failure.
                    return OperationResult.Warning($"Queue '{_queue}' not found.");
                }

                // make sure that we don't delete a non-empty queue if 'ifEmpty' is specified.
                if (_ifEmpty && queueInstance.MessageCount > 0)
                {
                    return OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Cannot delete queue '{_queue}'. The queue is not empty.")));
                }

                // make sure that we don't delete a queue with consumers if 'ifUnused' is specified.
                if (_ifUnused && queueInstance.ConsumerCount > 0)
                {
                    return OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Cannot delete queue '{_queue}'. The queue is in use.")));
                }

                // okay, we are good to go. first, we remove all bindings in which the queue is involved.
                var queueBindings = Server.QueueBindings.Where(qb => qb.Value.BoundQueues.ContainsKey(_queue));
                foreach (var item in queueBindings)
                {
                    var bindingKey = item.Key;
                    var operation = new QueueUnbindOperation(Server, item.Value.Exchange.Name, _queue, bindingKey);
                    await Server.Processor.EnqueueOperationAsync(operation, true).ConfigureAwait(false);
                }

                // remove the queue from the server. if remove returns false, the queue may have been removed by another thread.
                // in that case, we return a success message.
                if (!Server.Queues.Remove(_queue))
                {
                    return OperationResult.Warning($"Queue '{_queue}' not found.");
                }

                // 8return success
                return OperationResult.Success($"Queue '{_queue}' deleted successfully.");
            }
            catch (Exception ex)
            {
                return OperationResult.Failure(ex);
            }
        }
    }
}