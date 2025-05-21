using RabbitMQ.Client.Mock.NetStandard.Server.Bindings;
using RabbitMQ.Client.Mock.NetStandard.Server.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Exchanges
{
    internal abstract class RabbitExchange
    {
        public RabbitExchange(IRabbitServer server, string name, string type)
        {
            Server = server ?? throw new ArgumentNullException(nameof(server));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }
        #region Properties
        protected IRabbitServer Server { get; }

        public string Name { get; set; }

        public string Type { get; }

        public bool IsDurable { get; set; }

        public bool AutoDelete { get; set; }

        public IDictionary<string, object> Arguments { get; set; }

        public IReadOnlyDictionary<string, ExchangeBinding> ExchangeBindings => new ReadOnlyDictionary<string, ExchangeBinding>(Server.ExchangeBindings.Where(b => b.Value.Exchange.Name.Equals(Name)).ToDictionary(kvp => kvp.Key, KeyValuePair => KeyValuePair.Value));

        public IReadOnlyDictionary<string, QueueBinding> QueueBindings => new ReadOnlyDictionary<string, QueueBinding>(Server.QueueBindings.Where(b => b.Value.Exchange.Name.Equals(Name)).ToDictionary(kvp => kvp.Key, KeyValuePair => KeyValuePair.Value));
        #endregion

        #region Message Publishing
        public abstract ValueTask PublishMessageAsync(string routingKey, RabbitMessage message);
        #endregion
    }
}
