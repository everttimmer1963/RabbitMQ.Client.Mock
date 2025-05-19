
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Mock.Server.Data;

namespace RabbitMQ.Client.Mock.Server.Operations;

internal class BasicPublishOperation<TProperties>(IRabbitServer server, FakeChannel channel, string exchange, string routingKey, bool mandatory, TProperties properties, ReadOnlyMemory<byte> body) : Operation(server) where TProperties : IReadOnlyBasicProperties, IAmqpHeader
{
    public override bool IsValid => !(Server is null || channel is null || string.IsNullOrEmpty(exchange) || string.IsNullOrEmpty(routingKey) || body.IsEmpty);

    public async override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            if(!IsValid)
            {
                return OperationResult.Warning("Either Server, exchange, routingkey or body is null or empty.");
            }

            // get the exchange that we are publishing to.
            var exchangeInstance = Server.Exchanges.TryGetValue(exchange, out var ex) ? ex : default;
            if (exchangeInstance is null)
            {
                // if mandatory is specified, and we cannot deliver the message, we should send the message back to the client,
                // and return with a warning.
                if (mandatory)
                {
                    await channel.HandleBasicReturnAsync(new BasicReturnEventArgs(0, $"Exchange '{exchange}' not found.", exchange, routingKey, properties, body));
                }
                return OperationResult.Warning($"Exchange '{exchange}' not found.");
            }

            // create and publish a message to the exchange.
            var message = new RabbitMessage
            {
                Exchange = exchange,
                RoutingKey = routingKey,
                Mandatory = mandatory,
                Immediate = true,
                Redelivered = false,
                Body = body.ToArray(),
                DeliveryTag = await Server.GetNextDeliveryTagForChannel(channel.ChannelNumber),
                BasicProperties = properties
            };
            await exchangeInstance.PublishMessageAsync(routingKey, message);

            return OperationResult.Success($"Message published to exchange '{exchange}' with routing key '{routingKey}'.");
        }
        catch (Exception ex)
        {
            return OperationResult.Failure(ex);
        }
    }
}
