using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Data
{
    internal class PendingConfirm
    {
        private DateTime _createdAt;

        public PendingConfirm(int channelNumber, ulong deliveryTag, RabbitMessage message, TimeSpan timeOut)
        {
            _createdAt = DateTime.UtcNow;
            ChannelNumber = channelNumber;
            DeliveryTag = deliveryTag;
            Message = message;
            TimeOut = timeOut;
        }

        public int ChannelNumber { get; }
        public ulong DeliveryTag { get; }
        public RabbitMessage Message { get; }
        public TimeSpan TimeOut { get; }
        public bool HasExpired => DateTime.UtcNow - _createdAt > TimeOut;
    }
}
