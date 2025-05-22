using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Operations
{
    internal class BasicCancelOperation : Operation
    {
        private string _consumerTag;

        public BasicCancelOperation(IRabbitServer server, string consumerTag)
            : base(server)
        {
            _consumerTag = consumerTag ?? throw new ArgumentNullException(nameof(consumerTag));
        }

        public override bool IsValid => !(Server is null || string.IsNullOrEmpty(_consumerTag));

        public override async ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                // get the specified consumer binding
                var binding = Server.ConsumerBindings.TryGetValue(_consumerTag, out var bnd) ? bnd : null;
                if (binding is null)
                {
                    return OperationResult.Warning($"Consumer '{_consumerTag}' not found.");
                }

                // notify the consumer
                await binding.Consumer.HandleBasicCancelOkAsync(_consumerTag, cancellationToken).ConfigureAwait(false);

                // remove the binding from the server, which will stop message delivery for the consumer.
                if (Server.ConsumerBindings.Remove(_consumerTag))
                {
                    // and return success
                    return OperationResult.Success($"Consumer '{_consumerTag}' cancelled successfully.");
                }

                return OperationResult.Warning($"Consumer '{_consumerTag}' not found.");
            }
            catch (Exception ex)
            {
                return OperationResult.Failure(ex);
            }
        }
    }
}
