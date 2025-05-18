using System.Net.Sockets;

namespace RabbitMQ.Client.Mock.Server.Operations;

internal abstract class Operation(IRabbitServer server)
{
    protected IRabbitServer Server { get; init; } = server;

    public Guid OperationId { get; } = Guid.NewGuid();

    public abstract bool IsValid { get; }

    public abstract ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken);
}
