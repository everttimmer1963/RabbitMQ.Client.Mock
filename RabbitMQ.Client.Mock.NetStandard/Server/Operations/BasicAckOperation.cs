using RabbitMQ.Client.Events;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Operations
{
    internal class BasicAckOperation : Operation
    {
        private readonly FakeChannel _channel;
        private readonly ulong _deliveryTag;
        private readonly bool _multiple;

        public BasicAckOperation(IRabbitServer server, FakeChannel channel, ulong deliveryTag, bool multiple)
            : base(server)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _deliveryTag = deliveryTag;
            _multiple = multiple;
        }

        public override bool IsValid => (Server != null);

        public override async ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!IsValid)
                {
                    return OperationResult.Failure(new InvalidOperationException("Server is required."));
                }

                // get the pending message(s)
                // if multiple is specified we need to ack all messages up to, and including, the delivery tag
                var messagesToAck = _multiple
                    ? Server.PendingConfirms.Where(pc => pc.Value.ChannelNumber == _channel.ChannelNumber && pc.Value.DeliveryTag <= _deliveryTag).Select(pc => pc.Key).ToArray()
                    : Server.PendingConfirms.Where(pc => pc.Value.ChannelNumber == _channel.ChannelNumber && pc.Value.DeliveryTag == _deliveryTag).Select(pc => pc.Key).ToArray();

                // since an acknowledment means that the message was processed successfully, there is no need to
                // kee it on the server. Since it has already been removed from the original queue, deleting the
                // pending confirm is the only thing we need to do.
                foreach (var key in messagesToAck)
                {
                    var pc = Server.PendingConfirms[key];
                    await _channel.HandleBasicAckAsync(new BasicAckEventArgs(pc.DeliveryTag, _multiple));
                    Server.PendingConfirms.Remove(key);
                }

                // return success.
                return OperationResult.Success($"Acknowledged {messagesToAck.Length} message(s) on channel '{_channel.ChannelNumber}'.");
            }
            catch (Exception ex)
            {
                return OperationResult.Failure(ex);
            }
        }
    }
}
