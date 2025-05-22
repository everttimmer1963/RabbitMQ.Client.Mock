using RabbitMQ.Client.Mock.NetStandard.Server.Exchanges;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Operations
{
    internal class ExchangeDeclareOperation : Operation
    {
        private readonly string _exchange;
        private readonly string _type;
        private readonly bool _durable;
        private readonly bool _autoDelete;
        private readonly IDictionary<string, object> _arguments;
        private readonly bool _passive;

        public ExchangeDeclareOperation(IRabbitServer server, string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments = null, bool passive = false)
            : base(server)
        {
            _exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
            _type = type ?? throw new ArgumentNullException(nameof(type));
            _durable = durable;
            _autoDelete = autoDelete;
            _arguments = arguments ?? new Dictionary<string, object>();
            _passive = passive;
        }

        public override bool IsValid => !(Server is null || string.IsNullOrWhiteSpace(_exchange) || string.IsNullOrWhiteSpace(_type));

        public override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!IsValid)
                {
                    return new ValueTask<OperationResult>(OperationResult.Failure(new InvalidOperationException("Exchange, Type and Name are required.")));
                }

                // check if the exchange already exists.
                var exchangeInstance = Server.Exchanges.TryGetValue(_exchange, out var x) ? x : null;
                if (exchangeInstance != null)
                {
                    return new ValueTask<OperationResult>(OperationResult.Warning($"Exchange '{exchangeInstance.Name}' already exists."));
                }

                // if passive is true, we should not create the exchange, but return an error instead.
                if (_passive)
                {
                    return new ValueTask<OperationResult>(OperationResult.Failure(new InvalidOperationException($"Exchange '{_exchange}' not found.")));
                }

                // create a new exchange
                switch (_type)
                {
                    case ExchangeType.Direct: exchangeInstance = new DirectExchange(Server, _exchange); break;
                    case ExchangeType.Fanout: exchangeInstance = new FanoutExchange(Server, _exchange); break;
                    case ExchangeType.Topic: exchangeInstance = new TopicExchange(Server, _exchange); break;
                    case ExchangeType.Headers: exchangeInstance = new HeadersExchange(Server, _exchange); break;
                    default:
                        return new ValueTask<OperationResult>(OperationResult.Failure(new InvalidOperationException($"Exchange type '{_type}' not supported.")));
                }
                exchangeInstance.IsDurable = _durable;
                exchangeInstance.AutoDelete = _autoDelete;
                exchangeInstance.Arguments = _arguments;

                // add the exchange to the server.
                if (Server.Exchanges.ContainsKey(_exchange))
                {
                    return new ValueTask<OperationResult>(OperationResult.Warning($"Exchange '{_exchange}' already exists."));
                }
                Server.Exchanges.Add(_exchange, exchangeInstance);

                return new ValueTask<OperationResult>(OperationResult.Success($"Exchange '{_exchange}' created successfully."));

            }
            catch (Exception ex)
            {
                return new ValueTask<OperationResult>(OperationResult.Failure(ex));
            }
        }
    }
}