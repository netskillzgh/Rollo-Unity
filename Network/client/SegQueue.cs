using System.Collections.Generic;

namespace Rollo.Client
{
    public class SegQueue<T>
    {
        private readonly Queue<T> _queue = new Queue<T>();

        public int Count
        {
            get
            {
                lock (_queue)
                {
                    return _queue.Count;
                }
            }
        }

        public void Enqueue(T item)
        {
            lock (_queue)
            {
                _queue.Enqueue(item);
            }
        }

        public bool TryDequeue(out T result)
        {
            lock (_queue)
            {
                if (_queue.Count > 0)
                {
                    result = _queue.Dequeue();
                    return true;
                }

                result = default(T);
                return false;
            }
        }

        public bool TryDequeueAll(out T[] result)
        {
            lock (_queue)
            {
                result = _queue.ToArray();
                _queue.Clear();
                return result.Length > 0;
            }
        }

        public void Clear()
        {
            lock (_queue)
            {
                _queue.Clear();
            }
        }
    }
}