
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Operations
{
    internal class ExchangeDeclarePassiveOperation(IRabbitServer server, string exchange) : Operation(server)
    {
        public override bool IsValid => !(Server is null || string.IsNullOrEmpty(exchange));

        public override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                // check if all information is provided
                if (!IsValid)
                {
                    return ValueTask.FromResult(OperationResult.Failure(new InvalidOperationException("Exchange is required.")));
                }

                // get the exchange to bind to
                var xchg = Server.Exchanges.TryGetValue(exchange, out var x) ? x : null;
                if (xchg is null)
                {
                    return ValueTask.FromResult(OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Exchange '{exchange}' not found."))));
                }

                return ValueTask.FromResult(OperationResult.Success($"Exchange '{exchange}' exists."));
            }
            catch (Exception ex)
            {
                return ValueTask.FromResult(OperationResult.Failure(ex));
            }
        }
    }
}