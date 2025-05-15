using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Collections.Concurrent;

namespace RabbitMQ.Client.Mock.Server.Operations;

internal class OperationsProcessor : IDisposable
{
    private const int DefaultTimeoutInSeconds = 30; // 30 seconds

    private bool _isRunning;
    private bool _disposed;
    private Task _loopTask;
    private CancellationTokenSource _tokenSource = new();
    private readonly AsyncAutoResetEvent _waitHandle = new();
    private readonly ConcurrentQueue<(Operation Operation, Func<OperationResult, Task>? Callback)> Operations = new();

    public OperationProcessingStatus Status { get; private set; } = OperationProcessingStatus.Idle;

    public void StartProcessing()
    {
        if (_isRunning) return;
        _isRunning = true;

        Task.Run(() => ProcessingLoop(_tokenSource.Token));
    }

    public void StopProcessing()
    {
        if (!_isRunning) return;
        _isRunning = false;

        _tokenSource.Cancel();
        _loopTask.GetAwaiter().GetResult();
    }

    public async ValueTask<OperationResult> EnqueueOperationAsyncWithWait(Operation operation, bool throwOnError = false, CancellationToken cancellationToken)
    {
        // we need an operation to process.
        if (operation == null) throw new ArgumentNullException(nameof(operation));

        // prepare a default result (when a time-out occurs, this will be returned.)
        OperationResult opResult = null!;
        using AsyncAutoResetEvent operationDone = new AsyncAutoResetEvent(false, TimeSpan.FromSeconds(DefaultTimeoutInSeconds));
        Operations.Enqueue((operation, async (result) =>
        {
            // ok, the operation is done. release the wait handle.
            await Task.Run(() => Console.WriteLine($"Operation: {operation.OperationId.ToString()} - {result.Message}"));
            opResult = result;
            operationDone.Set();
        }));

        // now wait for the operation to complete, be cancelled or time-out.
        var timedOut = await operationDone.WaitOneAsync(cancellationToken);
        if (timedOut)
        {
            if (throwOnError)
            {
                throw new TimeoutException($"Operation: {operation.OperationId.ToString()} - The operation timed-out after {DefaultTimeoutInSeconds} seconds.");
            }
            return OperationResult.TimedOut($"Operation: {operation.OperationId.ToString()} - The operation timed-out after {DefaultTimeoutInSeconds} seconds.");
        }

        if (opResult.IsFailure && throwOnError)
        {
            var reason = new ShutdownEventArgs(ShutdownInitiator.Library, 0, 0, $"Operation: {operation.OperationId.ToString()} - {opResult.Message})");
            throw new OperationInterruptedException(reason);
        }

        // return the result of the operation.
        return opResult;
    }

    public ValueTask EnqueueOperationAsync(Operation operation, Func<OperationResult, Task>? callback = null)
    {
        if (operation == null) throw new ArgumentNullException(nameof(operation));
        Operations.Enqueue((operation, callback));
        _waitHandle.Set();
        return new ValueTask();
    }

    private async Task ProcessingLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await _waitHandle.WaitOneAsync(cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested)
                break;
            
            try
            {
                Status = OperationProcessingStatus.Processing;
                while (Operations.TryDequeue(out var combo))
                {
                    var operation = combo.Operation;
                    var notifyCaller = combo.Callback;
                    try
                    {
                        var result = await operation.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                        if ( notifyCaller is not null)
                        {
                            await notifyCaller(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error notifying caller: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing operation: {ex.Message}");
            }
            finally
            {
                Status = OperationProcessingStatus.Idle;
            }
        }
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;

        if (disposing)
        {
            StopProcessing();
            _waitHandle.Dispose();
            _tokenSource.Dispose();
            _loopTask.Dispose();
        }
    }

    public void Dispose()
    { 
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
