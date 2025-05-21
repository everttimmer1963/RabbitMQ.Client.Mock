using RabbitMQ.Client.Events;
using RabbitMQ.Client.Mock.Server.Bindings;
using RabbitMQ.Client.Mock.Server.Data;
using RabbitMQ.Client.Mock.Server.Exchanges;
using RabbitMQ.Client.Mock.Server.Operations;
using RabbitMQ.Client.Mock.Server.Queues;

namespace RabbitMQ.Client.Mock.Server;

internal interface IRabbitServer
{
    #region Properties
    IDictionary<string, RabbitExchange> Exchanges { get; }
    IDictionary<string, RabbitQueue> Queues { get; }
    IDictionary<string, ExchangeBinding> ExchangeBindings { get; }
    IDictionary<string, QueueBinding> QueueBindings { get; }
    IDictionary<string, ConsumerBinding> ConsumerBindings { get; }
    IDictionary<(int Channel, ulong DeliveryTag),PendingConfirm> PendingConfirms { get; }
    IDictionary<int, IChannel> Channels { get; }
    OperationsProcessor Processor { get; }
    #endregion

    #region Server Interface
    ValueTask<ulong> GetNextPublishSequenceNumberAsync(CancellationToken cancellationToken = default(CancellationToken));

    ValueTask BasicAckAsync(FakeChannel channel, ulong deliveryTag, bool multiple, CancellationToken cancellationToken = default(CancellationToken));

    ValueTask BasicNackAsync(FakeChannel channel, ulong deliveryTag, bool multiple, bool requeue, CancellationToken cancellationToken = default(CancellationToken));

    Task BasicCancelAsync(string consumerTag, bool noWait = false, CancellationToken cancellationToken = default(CancellationToken));

    Task<string> BasicConsumeAsync(FakeChannel channel, string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object?>? arguments, IAsyncBasicConsumer consumer, CancellationToken cancellationToken = default(CancellationToken));

    Task<BasicGetResult?> BasicGetAsync(int channelNumber, string queue, bool autoAck, CancellationToken cancellationToken = default(CancellationToken));

    ValueTask BasicPublishAsync<TProperties>(FakeChannel channel, string exchange, string routingKey, bool mandatory, TProperties basicProperties, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default(CancellationToken)) where TProperties : IReadOnlyBasicProperties, IAmqpHeader;

    ValueTask BasicPublishAsync<TProperties>(FakeChannel channel, CachedString exchange, CachedString routingKey, bool mandatory, TProperties basicProperties, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default(CancellationToken)) where TProperties : IReadOnlyBasicProperties, IAmqpHeader;

    Task BasicQosAsync(uint prefetchSize, ushort prefetchCount, bool global, CancellationToken cancellationToken = default(CancellationToken));

    ValueTask BasicRejectAsync(FakeChannel channel, ulong deliveryTag, bool requeue, CancellationToken cancellationToken = default(CancellationToken));

    Task CloseAsync(ushort replyCode, string replyText, bool abort, CancellationToken cancellationToken = default(CancellationToken));

    Task CloseAsync(ShutdownEventArgs reason, bool abort, CancellationToken cancellationToken = default(CancellationToken));

    Task ExchangeDeclareAsync(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object?>? arguments = null, bool passive = false, bool noWait = false, CancellationToken cancellationToken = default(CancellationToken));

    Task ExchangeDeclarePassiveAsync(string exchange, CancellationToken cancellationToken = default(CancellationToken));

    Task ExchangeDeleteAsync(string exchange, bool ifUnused = false, bool noWait = false, CancellationToken cancellationToken = default(CancellationToken));

    Task ExchangeBindAsync(string destination, string source, string routingKey, IDictionary<string, object?>? arguments = null, bool noWait = false, CancellationToken cancellationToken = default(CancellationToken));

    Task ExchangeUnbindAsync(string destination, string source, string routingKey, IDictionary<string, object?>? arguments = null, bool noWait = false, CancellationToken cancellationToken = default(CancellationToken));

    Task<QueueDeclareOk> QueueDeclareAsync(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object?>? arguments = null, bool passive = false, bool noWait = false, CancellationToken cancellationToken = default(CancellationToken));

    Task<QueueDeclareOk> QueueDeclarePassiveAsync(string queue, CancellationToken cancellationToken = default(CancellationToken));

    Task<uint> QueueDeleteAsync(string queue, bool ifUnused, bool ifEmpty, bool noWait = false, CancellationToken cancellationToken = default(CancellationToken));

    Task<uint> QueuePurgeAsync(string queue, CancellationToken cancellationToken = default(CancellationToken));

    Task QueueBindAsync(string queue, string exchange, string routingKey, IDictionary<string, object?>? arguments = null, bool noWait = false, CancellationToken cancellationToken = default(CancellationToken));

    Task QueueUnbindAsync(string queue, string exchange, string routingKey, IDictionary<string, object?>? arguments = null, CancellationToken cancellationToken = default(CancellationToken));

    Task<uint> MessageCountAsync(string queue, CancellationToken cancellationToken = default(CancellationToken));

    Task<uint> ConsumerCountAsync(string queue, CancellationToken cancellationToken = default(CancellationToken));

    ValueTask<string> GenerateUniqueConsumerTag(string queueName);

    ValueTask<ulong> GetNextDeliveryTagForChannel(int channelNumber);
    #endregion

    #region Connection & Channel Management
    int RegisterChannel(IChannel channel);

    void UnregisterChannel(int channelNumber);

    int RegisterConnection(IConnection connection);

    void UnregisterConnection(int connectionNumber);
    #endregion
}
