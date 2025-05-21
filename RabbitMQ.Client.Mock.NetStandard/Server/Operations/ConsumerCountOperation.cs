using System;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Operations
{
    internal class ConsumerCountOperation : Operation
    {
        private readonly string _queue;

        public ConsumerCountOperation(IRabbitServer server, string queue)
            : base(server)
        {
            this._queue = queue ?? throw new ArgumentNullException(nameof(queue));
        }

        public override bool IsValid => !(Server is null || string.IsNullOrEmpty(_queue));

        public override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!IsValid)
                {
                    return new ValueTask<OperationResult>(OperationResult.Failure(new ArgumentException("Either Server or queue is null or empty.")));
                }

                // count the number of consumers for the queue.
                ulong count = Convert.ToUInt64(Server.Queues.TryGetValue(_queue, out var q) ? q.ConsumerCount : 0);

                return new ValueTask<OperationResult>(OperationResult.Success(result: count));
            }
            catch (Exception ex)
            {
                return new ValueTask<OperationResult>(OperationResult.Failure(ex));
            }
        }
    }
}