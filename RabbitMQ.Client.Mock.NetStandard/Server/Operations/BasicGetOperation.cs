
using RabbitMQ.Client.Mock.NetStandard.Server.Data;
using RabbitMQ.Client.Mock.NetStandard.Server.Queues;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Operations
{

    internal class BasicGetOperation : Operation
    {
        private readonly int _channelNumber;
        private readonly string _queue;
        private readonly bool _autoAck;

        public BasicGetOperation(IRabbitServer server, int channelNumber, string queue, bool autoAck)
            : base(server)
        {
            _channelNumber = channelNumber;
            _queue = queue;
            _autoAck = autoAck;
        }

        public override bool IsValid => !(Server is null || string.IsNullOrEmpty(_queue));

        public async override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                // get the queue.
                if (!Server.Queues.TryGetValue(_queue, out var rabbitQueue))
                {
                    return OperationResult.Warning($"Queue '{_queue}' not found.");
                }

                // get the message, if any. If there is no message available at this time, return success and a null value.
                var message = await rabbitQueue.ConsumeMessageAsync(_channelNumber, _autoAck);
                if (message is null)
                {
                    return OperationResult.Success();
                }

                // pprepare and return the result
                var result = PrepareResult(rabbitQueue, message);
                return OperationResult.Success("Message returned", result);
            }
            catch (Exception ex)
            {
                return OperationResult.Failure(ex);
            }
        }

        private BasicGetResult PrepareResult(RabbitQueue queue, RabbitMessage message = null)
        {
            var messageCount = queue.MessageCount;
            return new BasicGetResult(
                deliveryTag: message.DeliveryTag,
                redelivered: message.Redelivered,
                exchange: message.Exchange,
                routingKey: message.RoutingKey,
                basicProperties: message.BasicProperties,
                body: message.Body,
                messageCount: messageCount
            );
        }
    }
}