using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Operations
{
    internal class QueuePurgeOperation : Operation
    {
        readonly string queue;

        public QueuePurgeOperation(IRabbitServer server, string queue)
            : base(server)
        {
            this.queue = queue ?? throw new ArgumentNullException(nameof(queue));
        }

        public override bool IsValid => !(Server is null || string.IsNullOrEmpty(queue));

        public override async ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!IsValid)
                {
                    return OperationResult.Failure(new ArgumentException("Either Server or queue is null or empty."));
                }

                // get the queue to purge
                var queueInstance = Server.Queues.TryGetValue(queue, out var x) ? x : null;
                if (queueInstance is null)
                {
                    return OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Queue '{queue}' not found.")));
                }

                // purge the queue
                var purged = await queueInstance.PurgeAsync();
                return OperationResult.Success($"Queue '{queue}' was purged.", purged);
            }
            catch (Exception ex)
            {
                return OperationResult.Failure(ex);
            }
        }
    }
}