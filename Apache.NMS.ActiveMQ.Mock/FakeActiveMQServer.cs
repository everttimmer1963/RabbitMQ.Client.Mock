using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Apache.NMS.ActiveMQ.Mock;

internal class FakeActiveMQServer
{
    private static readonly Lazy<FakeActiveMQServer> _instance = new(() => new FakeActiveMQServer());
    public static FakeActiveMQServer Instance => _instance.Value;

    // In-memory storage for queues and topics
    public ConcurrentDictionary<string, Queue<FakeMessage>> Queues { get; } = new();
    public ConcurrentDictionary<string, List<FakeMessage>> Topics { get; } = new();
    public ConcurrentDictionary<string, Queue<FakeMessage>> TemporaryQueues { get; } = new();
    public ConcurrentDictionary<string, List<FakeMessage>> TemporaryTopics { get; } = new();
    public ConcurrentDictionary<string, List<FakeMessage>> DeadLetterQueues { get; } = new();

    // Consumer management
    private readonly ConcurrentDictionary<string, ConsumerRegistration> consumers = new(); // consumerTag -> registration
    private readonly ConcurrentDictionary<string, ConsumerRegistration> durableConsumers = new(); // durableName -> registration
    private readonly ConcurrentDictionary<string, List<ConsumerRegistration>> sharedConsumers = new(); // sharedName -> registrations
    private int _consumerTagCounter = 0;

    // Transaction management
    private readonly ConcurrentDictionary<string, List<FakeMessage>> transactions = new(); // txId -> messages

    private FakeActiveMQServer() { }

    // Queue operations
    public void CreateQueue(string name) => Queues.TryAdd(name, new Queue<FakeMessage>());
    public void DeleteQueue(string name) => Queues.TryRemove(name, out _);
    public void CreateTemporaryQueue(string name) => TemporaryQueues.TryAdd(name, new Queue<FakeMessage>());
    public void DeleteTemporaryQueue(string name) => TemporaryQueues.TryRemove(name, out _);
    public void CreateDeadLetterQueue(string name) => DeadLetterQueues.TryAdd(name, new List<FakeMessage>());
    public void AddToDeadLetterQueue(string queue, FakeMessage message)
    {
        if (!DeadLetterQueues.ContainsKey(queue))
            CreateDeadLetterQueue(queue);
        DeadLetterQueues[queue].Add(message);
    }

    public int GetQueueMessageCount(string queueName)
    {
        if (Queues.TryGetValue(queueName, out var q))
            return q.Count;
        return 0;
    }
    public int GetQueueConsumerCount(string queueName)
    {
        return consumers.Values.Count(c => c.Destination == queueName);
    }

    // Topic operations
    public void CreateTopic(string name) => Topics.TryAdd(name, new List<FakeMessage>());
    public void DeleteTopic(string name) => Topics.TryRemove(name, out _);
    public void CreateTemporaryTopic(string name) => TemporaryTopics.TryAdd(name, new List<FakeMessage>());
    public void DeleteTemporaryTopic(string name) => TemporaryTopics.TryRemove(name, out _);

    // Consumer registration
    public string Subscribe(string destination, Func<FakeMessage, bool> selector, Action<FakeMessage> onMessage, bool durable = false, string durableName = null, bool shared = false, string sharedName = null, bool exclusive = false)
    {
        var tag = $"consumer-{Interlocked.Increment(ref _consumerTagCounter)}";
        var reg = new ConsumerRegistration(destination, selector, onMessage, durable, durableName, shared, sharedName, exclusive);
        consumers[tag] = reg;
        if (durable && durableName != null)
            durableConsumers[durableName] = reg;
        if (shared && sharedName != null)
        {
            if (!sharedConsumers.ContainsKey(sharedName))
                sharedConsumers[sharedName] = new List<ConsumerRegistration>();
            sharedConsumers[sharedName].Add(reg);
        }
        return tag;
    }
    public void Unsubscribe(string consumerTag)
    {
        if (consumers.TryRemove(consumerTag, out var reg))
        {
            if (reg.Durable && reg.DurableName != null)
                durableConsumers.TryRemove(reg.DurableName, out _);
            if (reg.Shared && reg.SharedName != null && sharedConsumers.ContainsKey(reg.SharedName))
                sharedConsumers[reg.SharedName].Remove(reg);
        }
    }

    // Message production
    public void EnqueueMessage(string queue, FakeMessage message, string txId = null, bool isTemporary = false)
    {
        var dict = isTemporary ? TemporaryQueues : Queues;
        if (txId != null)
        {
            if (!transactions.ContainsKey(txId))
                transactions[txId] = new List<FakeMessage>();
            transactions[txId].Add(message);
        }
        else if (dict.TryGetValue(queue, out var q))
        {
            q.Enqueue(message);
            DeliverToConsumers(queue, message);
        }
    }
    public void PublishToTopic(string topic, FakeMessage message, string txId = null, bool isTemporary = false)
    {
        var dict = isTemporary ? TemporaryTopics : Topics;
        if (txId != null)
        {
            if (!transactions.ContainsKey(txId))
                transactions[txId] = new List<FakeMessage>();
            transactions[txId].Add(message);
        }
        else if (dict.TryGetValue(topic, out var list))
        {
            list.Add(message);
            DeliverToConsumers(topic, message);
        }
    }

    // Redelivery and DLQ
    public void RedeliverOrDeadLetter(string queue, FakeMessage message, int maxRedeliveries)
    {
        message.RedeliveryCount++;
        if (message.RedeliveryCount > maxRedeliveries)
            AddToDeadLetterQueue(queue, message);
        else
            EnqueueMessage(queue, message);
    }

    // Transaction support
    public void BeginTransaction(string txId)
    {
        transactions[txId] = new List<FakeMessage>();
    }
    public void CommitTransaction(string txId)
    {
        if (transactions.TryRemove(txId, out var msgs))
        {
            foreach (var msg in msgs)
            {
                if (Queues.ContainsKey(msg.Destination))
                    EnqueueMessage(msg.Destination, msg);
                else if (Topics.ContainsKey(msg.Destination))
                    PublishToTopic(msg.Destination, msg);
            }
        }
    }
    public void RollbackTransaction(string txId)
    {
        transactions.TryRemove(txId, out _);
    }

    // Message consumption
    public FakeMessage DequeueMessage(string queue, Func<FakeMessage, bool> selector = null, bool isTemporary = false)
    {
        var dict = isTemporary ? TemporaryQueues : Queues;
        if (dict.TryGetValue(queue, out var q))
        {
            var arr = q.ToArray();
            foreach (var msg in arr)
            {
                if (selector == null || selector(msg))
                {
                    // Remove from queue
                    var temp = new Queue<FakeMessage>(q.Where(m => !ReferenceEquals(m, msg)));
                    while (q.Count > 0) q.Dequeue();
                    foreach (var m in temp) q.Enqueue(m);
                    return msg;
                }
            }
        }
        return null;
    }

    // Acknowledge (manual/auto)
    public void Acknowledge(FakeMessage message) { message.Acknowledged = true; }

    // Internal: deliver to registered consumers
    private void DeliverToConsumers(string destination, FakeMessage message)
    {
        foreach (var reg in consumers.Values)
        {
            if (reg.Destination == destination && (reg.Selector == null || reg.Selector(message)))
            {
                if (reg.Exclusive)
                {
                    // Only deliver to the first exclusive consumer
                    reg.OnMessage?.Invoke(message);
                    break;
                }
                reg.OnMessage?.Invoke(message);
            }
        }
    }

    private class ConsumerRegistration
    {
        public string Destination { get; }
        public Func<FakeMessage, bool> Selector { get; }
        public Action<FakeMessage> OnMessage { get; }
        public bool Durable { get; }
        public string DurableName { get; }
        public bool Shared { get; }
        public string SharedName { get; }
        public bool Exclusive { get; }
        public ConsumerRegistration(string dest, Func<FakeMessage, bool> selector, Action<FakeMessage> onMessage, bool durable = false, string durableName = null, bool shared = false, string sharedName = null, bool exclusive = false)
        {
            Destination = dest;
            Selector = selector;
            OnMessage = onMessage;
            Durable = durable;
            DurableName = durableName;
            Shared = shared;
            SharedName = sharedName;
            Exclusive = exclusive;
        }
    }
}

internal class FakeMessage
{
    public string Body { get; set; }
    public string Destination { get; set; }
    public int RedeliveryCount { get; set; } = 0;
    public bool Acknowledged { get; set; } = false;
    // Add more properties as needed (headers, properties, etc.)
}
