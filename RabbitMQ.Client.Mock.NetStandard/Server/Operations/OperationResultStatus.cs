using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Operations
{
    internal enum OperationResultStatus
    {
        Success,
        Warning,
        Failure,
        Timeout
    }
}
