using RabbitMQ.Client.Mock.Server.Bindings;
using RabbitMQ.Client.Mock.Server.Data;
using System.Collections.ObjectModel;

namespace RabbitMQ.Client.Mock.Server.Exchanges;

internal abstract class RabbitExchange(IRabbitServer server, string name, string type)
{
    #region Properties
    protected IRabbitServer Server { get; } = server;

    public string Name { get; set; } = name;

    public string Type { get; } = type;

    public bool IsDurable { get; set; }

    public bool AutoDelete { get; set; }

    public IDictionary<string, object?>? Arguments { get; set; }

    public IReadOnlyDictionary<string, ExchangeBinding> ExchangeBindings => new ReadOnlyDictionary<string, ExchangeBinding>(Server.ExchangeBindings.Where(b => b.Value.Exchange.Name.Equals(Name)).ToDictionary(kvp => kvp.Key, KeyValuePair => KeyValuePair.Value));

    public IReadOnlyDictionary<string, QueueBinding> QueueBindings => new ReadOnlyDictionary<string, QueueBinding>(Server.QueueBindings.Where(b => b.Value.Exchange.Name.Equals(Name)).ToDictionary(kvp => kvp.Key, KeyValuePair => KeyValuePair.Value));
    #endregion

    #region Message Publishing
    public abstract ValueTask PublishMessageAsync(string routingKey, RabbitMessage message);
    #endregion
}
