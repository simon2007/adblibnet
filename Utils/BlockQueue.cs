using System.Collections.Generic;
using System.Threading;

namespace Utils
{
    public class BlockQueue<T>
        where T:class
    {
        private Queue<T> queue = new Queue<T>();

        public T Dequeue()
        {
            try
            {
                Monitor.Enter(queue);
                while (queue.Count <= 0)
                    Monitor.Wait(queue);

                return queue.Dequeue();
            }
            finally
            {
                Monitor.Exit(queue);
            }

        }

        public void Enqueue(T obj)
        {
            try
            {
                Monitor.Enter(queue);
                queue.Enqueue(obj);
            }
            finally
            {
                Monitor.Exit(queue);
            }
            Monitor.Pulse(queue);
        }

        public int Count
        {
            get => queue.Count;
        }

    }
}
