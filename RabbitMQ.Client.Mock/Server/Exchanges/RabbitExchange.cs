using RabbitMQ.Client.Mock.Domain;

namespace RabbitMQ.Client.Mock.Server.Exchanges;

internal abstract class RabbitExchange(IRabbitServer server, string name, string type)
{
    #region Properties
    protected IRabbitServer Server { get; } = server;

    public string Name { get; set; } = name;

    public string Type { get; set; } = type;

    public bool IsDurable { get; set; }

    public bool AutoDelete { get; set; }

    public IDictionary<string, object?> Arguments { get; } = new Dictionary<string, object?>();
    #endregion

    #region Exchange Bindings
    public abstract ValueTask BindExchangeAsync(string exchange, string routingKey, IDictionary<string, object?>? arguments = null);

    public abstract ValueTask UnbindExchangeAsync(string exchange, string routingKey);
    #endregion

    #region Queue Bindings
    public abstract ValueTask BindQueueAsync(string queue, string bindingKey, IDictionary<string, object?>? arguments = null);

    public abstract ValueTask UnbindQueueAsync(string queue, string bindingKey);
    #endregion

    #region Message Publishing
    public abstract ValueTask PublishMessageAsync(string routingKey, RabbitMessage message);
    #endregion
}
