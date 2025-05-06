
using System.Collections.Concurrent;

namespace RabbitMQ.Client.Mock.Domain;
internal class RabbitQueue(string name)
{
    private readonly ConcurrentQueue<RabbitMessage> _queue = new();
    private readonly ConcurrentDictionary<ulong, RabbitMessage> _activeMessages = new ConcurrentDictionary<ulong, RabbitMessage>();
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public string Name { get; set; } = name;

    public bool IsDurable { get; set; }

    public bool IsExclusive { get; set; }

    public bool AutoDelete { get; set; }

    internal ValueTask<bool> PublishMessageAsync(RabbitMessage message)
    {
        _queue.Enqueue(message);
        return ValueTask.FromResult(true);
    }

    internal ValueTask<RabbitMessage?> ConsumeMessageAsync(bool autoAck = true)
    {
        var message = _queue.TryDequeue(out var msg) ? msg : null;
        if (!autoAck && message is not null)
        {
            _activeMessages.TryAdd(message.DeliveryTag, message);
        }
        return ValueTask.FromResult<RabbitMessage?>(message);
    }

    internal ValueTask<RabbitMessage?> ConfirmMessageAsync(ulong deliveryTag)
    {
        if (_activeMessages.TryGetValue(deliveryTag, out var message))
        {
            _activeMessages.Remove(deliveryTag, out _);
        }
        return ValueTask.FromResult(message);
    }

    internal async ValueTask<RabbitMessage?> RejectMessageAsync(ulong deliveryTag, bool requeue)
    {
        if (_activeMessages.TryGetValue(deliveryTag, out var message))
        {
            if (requeue)
            {
                await RequeueMessageAsync(message);
            }
            _activeMessages.Remove(deliveryTag, out _);
            return message;
        }
        return null;
    }

    private async ValueTask RequeueMessageAsync(RabbitMessage message)
    {
        await _semaphore.WaitAsync();
        try
        {
            var queue = new ConcurrentQueue<RabbitMessage>();
            queue.Enqueue(message);
            while (_queue.TryDequeue(out var item))
            {
                queue.Enqueue(item);
            }
            _queue.Clear();
            while (queue.TryDequeue(out var item))
            {
                _queue.Enqueue(item);
            }
        }
        finally 
        {
            _semaphore.Release();
        }
    }
}
