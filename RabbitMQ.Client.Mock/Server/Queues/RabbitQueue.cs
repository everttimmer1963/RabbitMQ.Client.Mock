using RabbitMQ.Client.Mock.Server.Data;
using System.Collections.Concurrent;

namespace RabbitMQ.Client.Mock.Server.Queues;
internal class RabbitQueue
{
    private ConcurrentLinkedList<RabbitMessage> _queue = new();

    public required string Name { get; internal set; }

    public bool IsDurable { get; set; }

    public bool IsExclusive { get; set; }

    public bool AutoDelete { get; set; }

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
