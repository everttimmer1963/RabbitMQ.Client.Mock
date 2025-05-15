using RabbitMQ.Client.Mock.Server.Exchanges;

namespace RabbitMQ.Client.Mock.Server.Operations;
internal class ExchangeDeclareOperation(IRabbitServer server, string exchange, string type, bool durable, bool autoDelete, IDictionary<string,object?>? arguments = null, bool passive = false) : Operation(server)
{
    public override bool IsValid => !(Server is null || string.IsNullOrWhiteSpace(exchange) || string.IsNullOrWhiteSpace(type));

    public override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            if(!IsValid)
            {
                return ValueTask.FromResult(OperationResult.Failure("Exchange, Type and Name are required."));
            }

            // check if the exchange already exists.
            var exchangeInstance= Server.Exchanges.TryGetValue(exchange, out var x) ? x : null;
            if ( exchangeInstance is not null )
            {
                return ValueTask.FromResult(OperationResult.Success($"Exchange '{exchangeInstance.Name}' already exists."));
            }

            // if passive is true, we should not create the exchange, but return an error instead.
            if (passive)
            {
                return ValueTask.FromResult(OperationResult.Failure($"Exchange '{exchange}' not found."));
            }

            exchangeInstance = type switch
            {
                "direct" => new DirectExchange(Server, exchange),
                "fanout" => new FanoutExchange(exchange, durable, autoDelete, arguments),
                "topic" => new TopicExchange(exchange, durable, autoDelete, arguments),
                "headers" => new HeadersExchange(exchange, durable, autoDelete, arguments),
                _ => throw new ArgumentException($"Exchange type '{type}' is not supported.")
            };
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult(OperationResult.Failure(ex));
        }
    }
}
