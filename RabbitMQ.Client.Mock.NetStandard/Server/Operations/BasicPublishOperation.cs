
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Mock.NetStandard.Server.Data;
using RabbitMQ.Client.Mock.NetStandard.Server.Exchanges;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Operations
{
    internal class BasicPublishOperation<TProperties> : Operation where TProperties : IReadOnlyBasicProperties, IAmqpHeader
    {
        readonly FakeChannel _channel;
        readonly string _exchange;
        readonly string _routingKey;
        readonly bool _mandatory;
        readonly TProperties _properties;
        readonly ReadOnlyMemory<byte> _body;

        public BasicPublishOperation(IRabbitServer server, FakeChannel channel, string exchange, string routingKey, bool mandatory, TProperties properties, ReadOnlyMemory<byte> body)
            : base(server)
        {
            _channel = channel;
            _exchange = exchange;
            _routingKey = routingKey;
            _mandatory = mandatory;
            _properties = properties;
            _body = body;
        }

        public override bool IsValid => !(Server is null || _channel is null || _body.IsEmpty);

        public async override ValueTask<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!IsValid)
                {
                    return OperationResult.Warning("Either Server, exchange, routingkey or body is null or empty.");
                }

                RabbitExchange exchangeInstance = null;

                // get the exchange that we are publishing to.
                if (_exchange == string.Empty)
                {
                    // this is the default exchange. just temporarily create a new exchange instance
                    // that we can publish to.
                    exchangeInstance = new DirectExchange(Server, string.Empty);
                }
                else
                {
                    exchangeInstance = Server.Exchanges.TryGetValue(_exchange, out var ex) ? ex : default;
                    if (exchangeInstance is null)
                    {
                        // if mandatory is specified, and we cannot deliver the message, we should send the message back to the client,
                        // and return with a warning.
                        if (_mandatory)
                        {
                            await _channel.HandleBasicReturnAsync(new BasicReturnEventArgs(0, $"Exchange '{_exchange}' not found.", _exchange, _routingKey, _properties, _body));
                        }
                        return OperationResult.Warning($"Exchange '{_exchange}' not found.");
                    }
                }

                // create and publish a message to the exchange.
                var message = new RabbitMessage
                {
                    Exchange = _exchange,
                    RoutingKey = _routingKey,
                    Mandatory = _mandatory,
                    Immediate = true,
                    Redelivered = false,
                    Body = _body.ToArray(),
                    DeliveryTag = await Server.GetNextDeliveryTagForChannel(_channel.ChannelNumber),
                    BasicProperties = _properties
                };
                await exchangeInstance.PublishMessageAsync(_routingKey, message);

                return OperationResult.Success($"Message published to exchange '{_exchange}' with routing key '{_routingKey}'.");
            }
            catch (Exception ex)
            {
                return OperationResult.Failure(ex);
            }
        }
    }
}