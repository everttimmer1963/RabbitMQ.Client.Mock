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

    public async ValueTask<OperationResult<TResult>?> EnqueueOperationAsync<TResult>(Operation<TResult> operation, bool noWait = false, bool throwOnTimeout = false, Func<OperationResult, Task>? callback = null, CancellationToken cancellationToken = default) where TResult : class
    {
        // we need an operation to process.
        if (operation == null) throw new ArgumentNullException(nameof(operation));

        // check if we need to await the outcome of the operation.
        if (noWait)
        {
            // nope, just enqueue the operation. the caller will be notified of the result, if a callback is specified.
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            Operations.Enqueue((operation, callback));
            _waitHandle.Set();
            return null;
        }

        // prepare a default result (when a time-out occurs, this will be returned.)
        OperationResult<TResult> opResult = null!;
        using AsyncAutoResetEvent operationDone = new AsyncAutoResetEvent(false, TimeSpan.FromSeconds(DefaultTimeoutInSeconds));
        Operations.Enqueue((operation, async (result) =>
        {
            // ok, the operation is done. release the wait handle.
            await Task.Run(() => Console.WriteLine($"Operation: {operation.OperationId.ToString()} - {result.Message}"));
            opResult = (OperationResult<TResult>)result;
            operationDone.Set();
        }
        ));

        // now wait for the operation to complete, be cancelled or time-out.
        var timedOut = await operationDone.WaitOneAsync(cancellationToken);
        if (timedOut)
        {
            if (throwOnTimeout)
            {
                throw new TimeoutException($"Operation: {operation.OperationId.ToString()} - The operation timed-out after {DefaultTimeoutInSeconds} seconds.");
            }
            return OperationResult.TimedOut<TResult>($"Operation: {operation.OperationId.ToString()} - The operation timed-out after {DefaultTimeoutInSeconds} seconds.");
        }

        // if an exceltion has been returned with the result, we throw it.
        if (opResult is { IsFailure: true, Exception: not null })
        {
            throw opResult.Exception;
        }

        // return the result of the operation.
        return opResult;
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
