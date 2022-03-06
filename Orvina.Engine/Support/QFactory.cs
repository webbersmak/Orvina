namespace Orvina.Engine.Support
{
    /// <summary>
    /// like an ArrayPool, but for Queues
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal static class QFactory<T>
    {
        private static readonly SimpleQueue<SimpleQueue<T>> Closed = new(32);
        private static readonly Dictionary<int, SimpleQueue<T>> InUse = new(32);

        private static int maxId;
        private static SpinLock qLock = new();

        //public static SimpleQueue<T> All(int id)
        //{
        //    return InUse[id];
        //}

        public static int Count(int id)
        {
            return InUse[id].Count;
        }

        public static bool Any(int id)
        {
            return InUse[id].Any;
        }

        public static T Dequeue(int id)
        {
            return InUse[id].Dequeue();
        }

        public static void Enqueue(int id, T value)
        {
            InUse[id].Enqueue(value);
        }

        public static int GetQ()
        {
            SimpleQueue<T> queue;

            var locked = false;
            try
            {
                qLock.Enter(ref locked);
                //////////////////////
                if (Closed.TryDequeue(out SimpleQueue<T> old))
                {
                    queue = old;
                }
                else
                {
                    queue = new SimpleQueue<T>(32);
                }

                //return InUse.Add(queue);
                InUse.Add(maxId, queue);
                return maxId++;
                ///////////////////////
            }
            finally
            {
                if (locked)
                {
                    qLock.Exit(false);
                }
            }
        }

        public static void ReturnQ(int id)
        {
            var locked = false;
            try
            {
                qLock.Enter(ref locked);
                //////////////////////
                Closed.Enqueue(InUse[id]);
                InUse.Remove(id);
                ///////////////////////
            }
            finally
            {
                if (locked)
                {
                    qLock.Exit(false);
                }
            }
        }

        public static bool TryDequeue(int id, out T value)
        {
            return InUse[id].TryDequeue(out value);
        }
    }
}