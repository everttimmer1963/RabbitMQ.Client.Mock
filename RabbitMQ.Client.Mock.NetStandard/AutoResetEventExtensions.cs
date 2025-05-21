using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace RabbitMQ.Client.Mock.NetStandard
{
    internal static class AutoResetEventExtensions
    {
        public static async Task WaitOneAsync(this AutoResetEvent autoResetEvent, CancellationToken cancellationToken)
        {
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var tcs = new TaskCompletionSource<bool>();

                // Register cancellation
                using (linkedCts.Token.Register(() => tcs.TrySetCanceled()))
                {
                    // Wait for the AutoResetEvent in a separate task
                    var waitTask = Task.Run(() =>
                    {
                        autoResetEvent.WaitOne();
                        tcs.TrySetResult(true);
                    });

                    // Wait for either the AutoResetEvent or cancellation
                    await Task.WhenAny(tcs.Task, waitTask);
                }
            }
        }

        public static async Task WaitOneAsync(this AutoResetEvent autoResetEvent, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var tcs = new TaskCompletionSource<bool>();

                // Register cancellation
                using (linkedCts.Token.Register(() => tcs.TrySetCanceled()))
                {
                    // Wait for the AutoResetEvent in a separate task
                    var waitTask = Task.Run(() =>
                    {
                        autoResetEvent.WaitOne(timeout);
                        tcs.TrySetResult(true);
                    });

                    // Wait for either the AutoResetEvent or cancellation
                    await Task.WhenAny(tcs.Task, waitTask);
                }
            }
        }

        public static async Task WaitOneAsync(this AutoResetEvent autoResetEvent, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var tcs = new TaskCompletionSource<bool>();

                // Register cancellation
                using (linkedCts.Token.Register(() => tcs.TrySetCanceled()))
                {
                    // Wait for the AutoResetEvent in a separate task
                    var waitTask = Task.Run(() =>
                    {
                        autoResetEvent.WaitOne(millisecondsTimeout);
                        tcs.TrySetResult(true);
                    });

                    // Wait for either the AutoResetEvent or cancellation
                    await Task.WhenAny(tcs.Task, waitTask);
                }
            }
        }
    }
}
