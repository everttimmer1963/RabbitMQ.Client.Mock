
namespace RabbitMQ.Client.Mock.Server.Operations;
internal class BasicCancelOperation(IRabbitServer server, string consumerTag) : Operation(server)
{
    public override bool IsValid => !(Server is null || string.IsNullOrEmpty(consumerTag));

    public override async ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            // get the specified consumer binding
            var binding = Server.ConsumerBindings.TryGetValue(consumerTag, out var bnd) ? bnd : null;
            if (binding is null)
            {
                return OperationResult.Warning($"Consumer '{consumerTag}' not found.");
            }

            // notify the consumer
            await binding.Consumer.HandleBasicCancelOkAsync(consumerTag, cancellationToken).ConfigureAwait(false);

            // remove the binding from the server, which will stop message delivery for the consumer.
            if (Server.ConsumerBindings.Remove(consumerTag))
            {
                // and return success
                return OperationResult.Success($"Consumer '{consumerTag}' cancelled successfully.");
            }

            return OperationResult.Warning($"Consumer '{consumerTag}' not found.");
        }
        catch (Exception ex)
        {
            return OperationResult.Failure(ex);
        }
    }
}
