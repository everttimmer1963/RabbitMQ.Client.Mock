using System;
using System.Collections;
using Apache.NMS.ActiveMQ.Mock.Server;

namespace Apache.NMS.ActiveMQ.Mock;

internal class FakeQueueBrowser : IQueueBrowser
{
    private readonly string _queueName;
    private readonly Func<FakeMessage, bool> _selector;
    private readonly FakeActiveMQServer _server = FakeActiveMQServer.Instance;
    public IQueue Queue { get; }
    public string MessageSelector { get; }
    public FakeQueueBrowser(IQueue queue, string selector)
    {
        Queue = queue;
        _queueName = queue.ToString();
        MessageSelector = selector;
        _selector = string.IsNullOrEmpty(selector) ? (msg => true) : (msg => msg.Body != null && msg.Body.Contains(selector));
    }
    public IEnumerator GetEnumerator()
    {
        if (_server.Queues.TryGetValue(_queueName, out var q))
        {
            foreach (var msg in q)
            {
                if (_selector(msg))
                {
                    yield return new FakeTextMessage { Text = msg.Body };
                }
            }
        }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public void Close() { }
    public System.Threading.Tasks.Task CloseAsync() => System.Threading.Tasks.Task.CompletedTask;
    public void Dispose() { }
}
