using RabbitMQ.Client.Mock.Server.Data;

namespace RabbitMQ.Client.Mock.Server.Exchanges;
internal class HeadersExchange(IRabbitServer server, string name) : RabbitExchange(server, name, ExchangeType.Headers)
{
    public override ValueTask ExchangeBindAsync(string exchange, string routingKey, IDictionary<string, object?>? arguments = null, bool noWait = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override ValueTask ExchangeUnbindAsync(string exchange, string routingKey, bool noWait = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override ValueTask PublishMessageAsync(string routingKey, RabbitMessage message)
    {
        throw new NotImplementedException();
    }

    public override ValueTask QueueBindAsync(string queue, string bindingKey, IDictionary<string, object?>? arguments = null, bool noWait = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override ValueTask QueueUnbindAsync(string queue, string bindingKey, bool noWait = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
