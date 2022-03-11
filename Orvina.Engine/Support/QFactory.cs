namespace Orvina.Engine.Support
{
    /// <summary>
    /// like an ArrayPool, but for Queues
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal static class QFactory<T>
    {
        private static readonly Dictionary<int, SimpleQueue<T>> ActiveQueues = new();
        private static readonly SimpleQueue<SimpleQueue<T>> ClosedQueues = new();
        private static SpinLock ActiveQueueLock = new();
        private static int maxId;

        public static bool Any(int id)
        {
            return ActiveQueues[id].Any;
        }

        public static int Count(int id)
        {
            return ActiveQueues[id].Count;
        }

        public static T Dequeue(int id)
        {
            return ActiveQueues[id].Dequeue();
        }

        public static void Enqueue(int id, T value)
        {
            ActiveQueues[id].Enqueue(value);
        }

        public static int GetQ()
        {
            return Lock(() =>
            {
                ActiveQueues.Add(maxId, ClosedQueues.TryDequeue(out SimpleQueue<T> old) ? old : new SimpleQueue<T>());
                return maxId++;
            });

            //lock (ActiveQueues)
            //{
            //    if (ClosedQueues.TryDequeue(out SimpleQueue<T> old))
            //    {
            //        queue = old;
            //    }
            //    else
            //    {
            //        queue = new SimpleQueue<T>(32);
            //    }

            //    ActiveQueues.Add(maxId, queue);
            //    return maxId++;
            //}
        }

        public static void ReturnQ(int id)
        {
            Lock(() =>
            {
                ClosedQueues.Enqueue(ActiveQueues[id]);
                ActiveQueues.Remove(id);
            });

            //lock (ActiveQueues)
            //{
            //    ClosedQueues.Enqueue(ActiveQueues[id]);
            //    ActiveQueues.Remove(id);
            //}
        }

        public static bool TryDequeue(int id, out T value)
        {
            return ActiveQueues[id].TryDequeue(out value);
        }

        private static void Lock(Action atomicAction)
        {
            var locked = false;
            try
            {
                ActiveQueueLock.Enter(ref locked);
                atomicAction();
            }
            finally
            {
                if (locked)
                {
                    ActiveQueueLock.Exit(false);
                }
            }
        }

        private static T1 Lock<T1>(Func<T1> atomicAction)
        {
            var locked = false;
            try
            {
                ActiveQueueLock.Enter(ref locked);
                return atomicAction();
            }
            finally
            {
                if (locked)
                {
                    ActiveQueueLock.Exit(false);
                }
            }
        }
    }
}