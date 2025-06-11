using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Apache.NMS.ActiveMQ.Mock;

internal class FakeConnection : IConnection, IDisposable
{
    public bool IsStarted { get; private set; }
    public bool IsClosed { get; private set; }
    public string ClientId { get; set; }
    public TimeSpan RequestTimeout { get; set; }
    public ConsumerTransformerDelegate ConsumerTransformer { get; set; }
    public ProducerTransformerDelegate ProducerTransformer { get; set; }
    public AcknowledgementMode AcknowledgementMode { get; set; }
    public IRedeliveryPolicy RedeliveryPolicy { get; set; }
    public IConnectionMetaData MetaData { get; } = null;
    public event ExceptionListener ExceptionListener { add { } remove { } }
    public event ConnectionInterruptedListener ConnectionInterruptedListener { add { } remove { } }
    public event ConnectionResumedListener ConnectionResumedListener { add { } remove { } }

    public void Start() => IsStarted = true;
    public void Stop() => IsStarted = false;
    public void Close() { IsClosed = true; Dispose(); }
    public ISession CreateSession() => new FakeSession();
    public ISession CreateSession(AcknowledgementMode acknowledgementMode) => new FakeSession(acknowledgementMode);
    public Task<ISession> CreateSessionAsync() => Task.FromResult<ISession>(new FakeSession());
    public Task<ISession> CreateSessionAsync(AcknowledgementMode acknowledgementMode) => Task.FromResult<ISession>(new FakeSession(acknowledgementMode));
    public void PurgeTempDestinations() { }
    public Task CloseAsync() { Close(); return Task.CompletedTask; }
    public Task StartAsync() { Start(); return Task.CompletedTask; }
    public Task StopAsync() { Stop(); return Task.CompletedTask; }
    public void Dispose() { }
}
