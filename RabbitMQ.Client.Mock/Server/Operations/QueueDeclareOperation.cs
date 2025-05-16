using RabbitMQ.Client.Mock.Server.Queues;

namespace RabbitMQ.Client.Mock.Server.Operations;

internal class QueueDeclareOperation(IRabbitServer server, string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object?>? arguments, bool passive = false) : Operation<QueueDeclareOk>(server)
{
    public override bool IsValid => !(Server is null || string.IsNullOrWhiteSpace(queue));

    public override ValueTask<OperationResult<QueueDeclareOk>> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!IsValid)
            {
                return ValueTask.FromResult(OperationResult.Failure<QueueDeclareOk>("Queue name is required."));
            }

            // check if the queue already exists.
            var queueInstance = Server.Queues.TryGetValue(queue, out var q) ? q : null;
            if (queueInstance is not null)
            {
                var messageCount = queueInstance.MessageCount;
                var consumerCount = queueInstance.ConsumerCount;
                return ValueTask.FromResult(OperationResult.Success<QueueDeclareOk>($"Queue '{queueInstance.Name}' already exists.", new QueueDeclareOk(queue, messageCount, consumerCount)));
            }

            // if passive is true, we should not create the queue, but return an error instead.
            if (passive)
            {
                return ValueTask.FromResult(OperationResult.Failure<QueueDeclareOk>($"Queue '{queue}' not found."));
            }

            // create a new queue
            queueInstance = new RabbitQueue(Server, queue);
            queueInstance.IsDurable = durable;
            queueInstance.IsExclusive = exclusive;
            queueInstance.AutoDelete = autoDelete;
            queueInstance.Arguments = arguments;

            // add the queue to the server. if, by a different thread, the queue was already added, tryadd will return false.
            if (!Server.Queues.TryAdd(queue, queueInstance))
            {
                return ValueTask.FromResult(OperationResult.Success($"Queue '{queue}' already exists.", new QueueDeclareOk(queue, 0, 0)));
            }
            return ValueTask.FromResult(OperationResult.Success($"Queue '{queue}' created successfully.", new QueueDeclareOk(queue, 0, 0)));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult(OperationResult.Failure<QueueDeclareOk>(ex));
        }
    }
}
