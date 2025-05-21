
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Operations
{
    internal class ExchangeDeclarePassiveOperation : Operation
    {
        readonly string _exchange;

        public ExchangeDeclarePassiveOperation(IRabbitServer server, string exchange)
            : base(server)
        {
            this._exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
        }
        public override bool IsValid => !(Server is null || string.IsNullOrEmpty(_exchange));

        public override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                // check if all information is provided
                if (!IsValid)
                {
                    return new ValueTask<OperationResult>(OperationResult.Failure(new InvalidOperationException("Exchange is required.")));
                }

                // get the exchange to bind to
                var xchg = Server.Exchanges.TryGetValue(_exchange, out var x) ? x : null;
                if (xchg is null)
                {
                    return new ValueTask<OperationResult>(OperationResult.Failure(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Library, 0, $"Exchange '{_exchange}' not found."))));
                }

                return new ValueTask<OperationResult>(OperationResult.Success($"Exchange '{_exchange}' exists."));
            }
            catch (Exception ex)
            {
                return new ValueTask<OperationResult>(OperationResult.Failure(ex));
            }
        }
    }
}