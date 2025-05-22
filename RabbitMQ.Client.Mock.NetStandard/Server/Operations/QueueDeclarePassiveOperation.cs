using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Operations
{
    internal class QueueDeclarePassiveOperation : Operation
    {
        readonly string _queue;

        public QueueDeclarePassiveOperation(IRabbitServer server, string queue)
            : base(server)
        { 
            this._queue = queue ?? throw new ArgumentNullException(nameof(queue));
        }

        public override bool IsValid => !(Server is null || string.IsNullOrEmpty(_queue));

        public override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                // check if all information is provided
                if (!IsValid)
                {
                    return new ValueTask<OperationResult>(OperationResult.Failure(new InvalidOperationException("Queue is required.")));
                }

                // get the exchange to bind to
                var queueInstance = Server.Queues.TryGetValue(_queue, out var x) ? x : null;
                if (queueInstance is null)
                {
                    return new ValueTask<OperationResult>(OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Queue '{_queue}' not found."))));
                }

                var messageCount = queueInstance.MessageCount;
                var consumerCount = queueInstance.ConsumerCount;
                var result = new QueueDeclareOk(_queue, messageCount, consumerCount);
                return new ValueTask<OperationResult>(OperationResult.Success($"Queue '{_queue}' exists.", result));
            }
            catch (Exception ex)
            {
                return new ValueTask<OperationResult>(OperationResult.Failure(ex));
            }
        }
    }
}