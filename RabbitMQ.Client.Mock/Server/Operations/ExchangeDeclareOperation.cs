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
                return ValueTask.FromResult(OperationResult.Failure(new InvalidOperationException("Exchange, Type and Name are required.")));
            }

            // check if the exchange already exists.
            var exchangeInstance= Server.Exchanges.TryGetValue(exchange, out var x) ? x : null;
            if ( exchangeInstance is not null )
            {
                return ValueTask.FromResult(OperationResult.Warning($"Exchange '{exchangeInstance.Name}' already exists."));
            }

            // if passive is true, we should not create the exchange, but return an error instead.
            if (passive)
            {
                return ValueTask.FromResult(OperationResult.Failure(new InvalidOperationException($"Exchange '{exchange}' not found.")));
            }

            // create a new exchange
            exchangeInstance = type switch
            {
                "direct" => new DirectExchange(Server, exchange),
                "fanout" => new FanoutExchange(Server, exchange),
                "topic" => new TopicExchange(Server, exchange),
                "headers" => new HeadersExchange(Server, exchange),
                _ => throw new ArgumentException($"Exchange type '{type}' is not supported.")
            };
            exchangeInstance.IsDurable = durable;
            exchangeInstance.AutoDelete = autoDelete;
            exchangeInstance.Arguments = arguments;

            // add the exchange to the server. if, by a different thread, the exchange was already added, tryadd will return false.
            if (!Server.Exchanges.TryAdd(exchange, exchangeInstance))
            {
                return ValueTask.FromResult(OperationResult.Warning($"Exchange '{exchange}' already exists."));
            }

            return ValueTask.FromResult(OperationResult.Success($"Exchange '{exchange}' created successfully."));

        }
        catch (Exception ex)
        {
            return ValueTask.FromResult(OperationResult.Failure(ex));
        }
    }
}
