using System.Collections.Generic;
using System.Threading;

namespace Utils
{
    public class BlockQueue<T>
        where T:class
    {
        private readonly Queue<T> queue;

        public BlockQueue(int capacity)
        {
            queue = new Queue<T>(capacity);
        }

        public BlockQueue()
        {
            queue = new Queue<T>();
        }


        public T Dequeue()
        {

            lock (queue)
            {
                while (queue.Count <= 0)
                    Monitor.Wait(queue);

                return queue.Dequeue();
            }

        }

        public void Enqueue(T obj)
        {
            lock (queue)
            {
                queue.Enqueue(obj);
                Monitor.Pulse(queue);
            }
        }

        public int Count
        {
            get => queue.Count;
        }

    }
}
