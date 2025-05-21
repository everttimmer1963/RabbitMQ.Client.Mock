using RabbitMQ.Client.Mock.NetStandard.Server.Queues;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Operations
{
    internal class QueueDeclareOperation : Operation
    {
        readonly string _queue;
        readonly bool _passive;
        readonly bool _durable;
        readonly bool _exclusive;
        readonly bool _autoDelete;
        readonly IDictionary<string, object> _arguments;

        public QueueDeclareOperation(IRabbitServer server, string queue, bool passive, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
            : base(server)
        {
            this._queue = queue ?? throw new ArgumentNullException(nameof(queue));
            this._passive = passive;
            this._durable = durable;
            this._exclusive = exclusive;
            this._autoDelete = autoDelete;
            this._arguments = arguments;
        }

        public override bool IsValid => !(Server is null || string.IsNullOrWhiteSpace(_queue));

        public override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!IsValid)
                {
                    return new ValueTask<OperationResult>(OperationResult.Warning("Queue name is required."));
                }

                // check if the queue already exists.
                var queueInstance = Server.Queues.TryGetValue(_queue, out var q) ? q : null;
                if (queueInstance != null)
                {
                    var messageCount = queueInstance.MessageCount;
                    var consumerCount = queueInstance.ConsumerCount;
                    return new ValueTask<OperationResult>(OperationResult.Success($"Queue '{queueInstance.Name}' already exists.", new QueueDeclareOk(_queue, messageCount, consumerCount)));
                }

                // if passive is true, we should not create the queue, but return an error instead.
                if (_passive)
                {
                    return new ValueTask<OperationResult>(OperationResult.Warning($"Queue '{_queue}' not found."));
                }

                // create a new queue
                queueInstance = new RabbitQueue(Server, _queue);
                queueInstance.IsDurable = _durable;
                queueInstance.IsExclusive = _exclusive;
                queueInstance.AutoDelete = _autoDelete;
                queueInstance.Arguments = _arguments;

                // add the queue to the server. if, by a different thread, the queue was already added, tryadd will return false.
                if (Server.Queues.ContainsKey(_queue))
                {
                    return new ValueTask<OperationResult>(OperationResult.Warning($"Queue '{_queue}' already exists.", new QueueDeclareOk(_queue, 0, 0)));
                }
                Server.Queues.Add(_queue, queueInstance);
                return new ValueTask<OperationResult>(OperationResult.Success($"Queue '{_queue}' created successfully.", new QueueDeclareOk(_queue, 0, 0)));
            }
            catch (Exception ex)
            {
                return new ValueTask<OperationResult>(OperationResult.Failure(ex));
            }
        }
    }
}