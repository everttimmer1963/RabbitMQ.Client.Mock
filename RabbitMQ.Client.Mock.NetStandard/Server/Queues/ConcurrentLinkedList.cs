using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Mock.NetStandard.Server.Queues
{
    internal class ConcurrentLinkedList<T> : IDisposable, IAsyncDisposable
    {
        private bool _disposed;
        private LinkedList<T> _list = new LinkedList<T>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        /// <summary>
        /// Gets the number of nodes actually contained in the <see cref="ConcurrentLinkedList{T}"/>.
        /// </summary>
        /// <returns>The number of nodes actually contained in the <see cref="ConcurrentLinkedList{T}"/>.</returns>
        public int Count
        {
            get
            {
                _semaphore.Wait();
                try
                {
                    return _list.Count;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        /// <summary>
        /// Gets the first node of the <see cref="ConcurrentLinkedList{T}"/>.
        /// </summary>
        /// <returns>The first <see cref="LinkedListNode{T}"/> of the <see cref="ConcurrentLinkedList{T}"/>.</returns>
        public LinkedListNode<T> First
        {
            get
            {
                _semaphore.Wait();
                try
                {
                    return _list.First;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        /// <summary>
        /// Gets the last node of the <see cref="ConcurrentLinkedList{T}"/>.
        /// </summary>
        /// <returns>The last <see cref="LinkedListNode{T}"/> of the <see cref="ConcurrentLinkedList{T}"/>.</returns>
        public LinkedListNode<T> Last
        {
            get
            {
                _semaphore.Wait();
                try
                {
                    return _list.Last;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        /// <summary>
        /// Removes the node at the start of the <see cref="ConcurrentLinkedList{T}"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public void RemoveFirst()
        {
            _semaphore.Wait();
            try
            {
                _list.RemoveFirst();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Removes the node at the end of the <see cref="ConcurrentLinkedList{T}"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public void RemoveLast()
        {
            _semaphore.Wait();
            try
            {
                _list.RemoveLast();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Removes the specified node from the <see cref="ConcurrentLinkedList{T}"/>.
        /// </summary>
        /// <param name="node">The <see cref="LinkedListNode{T}"/> to remove from the <see cref="ConcurrentLinkedList{T}"/>.</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="InvalidOperationException"/>
        public void Remove(LinkedListNode<T> node)
        {
            _semaphore.Wait();
            try
            {
                _list.Remove(node);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Removes the first occurrence of the specified value from the <see cref="ConcurrentLinkedList{T}"/>.
        /// </summary>
        /// <param name="node">The <see cref="LinkedListNode{T}"/> to remove from the <see cref="ConcurrentLinkedList{T}"/>.</param>
        /// <returns><see langword="true"/> if the element containing <paramref name="value"/> is succesfully removed; otherwise, <see langword="false"/>. This method also returns <see langword="false"/> if <paramref name="value"/> was not found in the original <see cref="ConcurrentLinkedList{T}"/>.</returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="InvalidOperationException"/>
        public bool Remove(T value)
        {
            _semaphore.Wait();
            try
            {
                return _list.Remove(value);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Adds a new node containing the specified value at the start of the <see cref="ConcurrentLinkedList{T}"/>.
        /// </summary>
        /// <param name="value">The value to add at the start of the <see cref="ConcurrentLinkedList{T}"/></param>
        /// <returns>The new <see cref="LinkedListNode{T}"/> containing <paramref name="value"/>.</returns>
        public LinkedListNode<T> AddFirst(T value)
        {
            _semaphore.Wait();
            try
            {
                return _list.AddFirst(value);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Adds a new node containing the specified value at the end of the <see cref="ConcurrentLinkedList{T}"/>.
        /// </summary>
        /// <param name="value">The value to add at the end of the <see cref="ConcurrentLinkedList{T}"/></param>
        /// <returns>The new <see cref="LinkedListNode{T}"/> containing <paramref name="value"/>.</returns>
        public LinkedListNode<T> AddLast(T value)
        {
            _semaphore.Wait();
            try
            {
                return _list.AddLast(value);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Removes the first node from the <see cref="ConcurrentLinkedList{T}"/> and returns its value.
        /// </summary>
        /// <param name="value">The node that was removed.</param>
        /// <returns><see langword="True"/> if the first node was removed succesfully; otherwise <see langword="False"/>. <see cref="TryRemoveFirst(out T?)"/> also returns fals if the list contains no nodes.</returns>
        public bool TryRemoveFirst(out T value)
        {
            _semaphore.Wait();
            try
            {
                if (_list.First is null)
                {
                    value = default;
                    return false;
                }
                value = _list.First.Value;
                _list.RemoveFirst();
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Removes all nodes from the <see cref="ConcurrentLinkedList{T}"/>."/>
        /// </summary>
        public void Clear()
        {
            _semaphore.Wait();
            try
            {
                _list.Clear();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            if (disposing)
            {
                _semaphore.Dispose();
                _list.Clear();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public ValueTask DisposeAsync()
        {
            Dispose(false);
            return new ValueTask(Task.CompletedTask);
        }
    }
}
