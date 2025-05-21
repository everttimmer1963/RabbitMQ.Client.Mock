using System;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Operations
{
    internal abstract class Operation
    {
        public Operation(IRabbitServer server)
        {
            Server = server ?? throw new ArgumentNullException(nameof(server));
        }
        protected IRabbitServer Server { get; }

        public Guid OperationId { get; } = Guid.NewGuid();

        public virtual bool IsValid => !(Server is null);

        public abstract ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken);
    }
}
