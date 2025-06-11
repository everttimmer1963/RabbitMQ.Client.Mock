using System;

namespace Apache.NMS.ActiveMQ.Mock;

internal class FakeQueue : IQueue
{
    public string QueueName { get; }
    public DestinationType DestinationType => DestinationType.Queue;
    public bool IsTopic => false;
    public bool IsQueue => true;
    public bool IsTemporary => false;
    public void Dispose() { }
    public FakeQueue(string name) { QueueName = name; }
    public override string ToString() => QueueName;
}

internal class FakeTopic : ITopic
{
    public string TopicName { get; }
    public DestinationType DestinationType => DestinationType.Topic;
    public bool IsTopic => true;
    public bool IsQueue => false;
    public bool IsTemporary => false;
    public void Dispose() { }
    public FakeTopic(string name) { TopicName = name; }
    public override string ToString() => TopicName;
}

internal class FakeTemporaryQueue : ITemporaryQueue
{
    public string QueueName { get; }
    public DestinationType DestinationType => DestinationType.Queue;
    public bool IsTopic => false;
    public bool IsQueue => true;
    public bool IsTemporary => true;
    public void Dispose() { FakeActiveMQServer.Instance.DeleteTemporaryQueue(QueueName); }
    public void Delete() => Dispose();
    public System.Threading.Tasks.Task DeleteAsync() { Dispose(); return System.Threading.Tasks.Task.CompletedTask; }
    public FakeTemporaryQueue(string name) { QueueName = name; FakeActiveMQServer.Instance.CreateTemporaryQueue(name); }
    public override string ToString() => QueueName;
}

internal class FakeTemporaryTopic : ITemporaryTopic
{
    public string TopicName { get; }
    public DestinationType DestinationType => DestinationType.Topic;
    public bool IsTopic => true;
    public bool IsQueue => false;
    public bool IsTemporary => true;
    public void Dispose() { FakeActiveMQServer.Instance.DeleteTemporaryTopic(TopicName); }
    public void Delete() => Dispose();
    public System.Threading.Tasks.Task DeleteAsync() { Dispose(); return System.Threading.Tasks.Task.CompletedTask; }
    public FakeTemporaryTopic(string name) { TopicName = name; FakeActiveMQServer.Instance.CreateTemporaryTopic(name); }
    public override string ToString() => TopicName;
}
