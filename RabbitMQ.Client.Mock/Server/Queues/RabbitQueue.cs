using RabbitMQ.Client.Mock.Server.Data;

namespace RabbitMQ.Client.Mock.Server.Queues;
internal class RabbitQueue(IRabbitServer server, string name)
{
    private readonly ConcurrentLinkedList<RabbitMessage> _queue = new();

    private IRabbitServer Server => server;

    public string Name { get; } = name;

    public bool IsDurable { get; set; }

    public bool IsExclusive { get; set; }

    public bool AutoDelete { get; set; }
    public IDictionary<string, object?>? Arguments { get; internal set; }

    public uint MessageCount => Convert.ToUInt32(_queue.Count);

    public uint ConsumerCount => Convert.ToUInt32(Server.ConsumerBindings.Where(cb => cb.Value.Queue.Name.Equals(Name)).Count());

    public ValueTask PublishMessageAsync(RabbitMessage message)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }
        message.Queue = Name.ToString();
        _queue.AddLast(message);
        return ValueTask.CompletedTask;
    }

    public ValueTask RequeueMessageAsync(RabbitMessage message)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }
        message.Queue = Name.ToString();
        _queue.AddFirst(message);
        return ValueTask.CompletedTask;
    }

    public ValueTask<uint> PurgeAsync()
    {
        var count = (uint)_queue.Count;
        _queue.Clear();
        return ValueTask.FromResult(count);
    }
}
