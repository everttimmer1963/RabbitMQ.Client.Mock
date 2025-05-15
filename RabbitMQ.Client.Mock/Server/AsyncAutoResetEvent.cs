namespace RabbitMQ.Client.Mock.Server;

internal sealed class AsyncAutoResetEvent : IDisposable
{
    private readonly AutoResetEvent _event;
    private volatile bool _disposed;
    private readonly TimeSpan _timeOut;

    public AsyncAutoResetEvent(bool initialState = false, TimeSpan? timeOut = null)
    {
        _event = new AutoResetEvent(initialState);
        _timeOut = (timeOut == null) 
            ? TimeSpan.FromSeconds(30) 
            : timeOut.Value;
    }

    public void Set()
    {
        _event.Set();
    }

    public void Reset()
    {
        _event.Reset();
    }

    public async Task<bool> WaitOneAsync(CancellationToken cancellationToken = default)
    {
        // Fast path: try to wait synchronously first
        if (_event.WaitOne(0))
            return true;

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        RegisteredWaitHandle? registration = null;
        CancellationTokenRegistration ctr = default;

        try
        {
            registration = ThreadPool.RegisterWaitForSingleObject(
                _event,
                (state, timedOut) => ((TaskCompletionSource<bool>)state!).TrySetResult(!timedOut),
                tcs,
                (int)_timeOut.TotalMilliseconds,
                executeOnlyOnce: true);

            if (cancellationToken.CanBeCanceled)
            {
                ctr = cancellationToken.Register(() =>
                {
                    tcs.TrySetCanceled(cancellationToken);
                });
            }

            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            registration?.Unregister(null);
            ctr.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _event.Dispose();
    }
}
