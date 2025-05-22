using RabbitMQ.Client.Mock.Server.Bindings;
using RabbitMQ.Client.Mock.Server.Data;
using RabbitMQ.Client.Mock.Server.Operations;
using RabbitMQ.Client.Mock.Server.Queues;
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

    #region Queue Binding
    public virtual ValueTask<OperationResult> QueueBindAsync(RabbitQueue queueToBind, IDictionary<string, object?>? arguments = null, string? bindingKey = null)
    {
        string key = string.IsNullOrWhiteSpace(bindingKey) ? string.Empty : bindingKey;

        // check if we already have binding. if we don't, create a new one.
        var binding = Server.QueueBindings.TryGetValue(key, out var bnd) ? bnd : null;
        if (binding is null)
        {
            binding = new QueueBinding { Exchange = this, Arguments = arguments };
            binding.BoundQueues.Add(queueToBind.Name, queueToBind);
            Server.QueueBindings.TryAdd(key, binding);

            return ValueTask.FromResult(OperationResult.Success($"Queue '{queueToBind.Name}' bound to exchange '{this.Name}' with key '{key}'."));
        }

        // the binding exists. check if the target queue is already bound. If so, return success.
        if (!binding.BoundQueues.TryAdd(queueToBind.Name, queueToBind))
        {
            return ValueTask.FromResult(OperationResult.Success($"Queue '{queueToBind.Name}' already bound to exchange '{Name}' with key '{key}'."));
        }

        // the binding was added. return success.
        return ValueTask.FromResult(OperationResult.Success($"Queue '{queueToBind.Name}' bound to exchange '{Name}' with key '{key}'."));
    }
    #endregion
}
