namespace Orvina.Engine.Support
{
    /// <summary>
    /// small, efficient queue
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class SimpleQueue<T>
    {
        private int frontIdx;//[x][][]
        private T[] nodes;
        private int rearIdx;//[][][x]

        public SimpleQueue()
        {
            Reset();
        }

        public bool Any
        {
            get
            {
                return Count > 0;
            }
        }

        //public T[] ToArray
        //{
        //    get
        //    {
        //        var temp = new T[currentIdx + 1];
        //        Array.Copy(nodes, temp, temp.Length);
        //        return temp;
        //    }
        //}
        public int Count
        {
            get; private set;
        }

        public void Clear()
        {
            frontIdx = -1;
            rearIdx = -1;
            UpdateCount();
        }

        public T Dequeue()
        {
            var result = nodes[frontIdx];

            var desiredIdx = frontIdx + 1;//[x][x]

            if (desiredIdx > rearIdx)
            {
                Clear();
            }
            else
            {
                frontIdx = desiredIdx;
                UpdateCount();
            }
            //if (desiredIdx < (nodes.Length - 1) / 2) //need to shrink
            //{
            //    Array.Resize(ref nodes, desiredIdx + 1);
            //}

            return result;
        }

        public void Enqueue(T value)
        {
            var desiredIdx = rearIdx + 1; //on first enqueu will be 0

            if (desiredIdx > nodes.Length - 1) //need to grow
            {
                //double size
                Array.Resize(ref nodes, (desiredIdx + 1) * 2);
            }

            nodes[desiredIdx] = value;
            rearIdx = desiredIdx;
            if (frontIdx < 0)
            {
                frontIdx = rearIdx;
            }

            UpdateCount();
        }

        public bool TryDequeue(out T value)
        {
            if (Any)
            {
                value = Dequeue();
                return true;
            }

            value = default;
            return false;
        }

        private void Reset()
        {
            nodes = Array.Empty<T>();
            frontIdx = -1;
            rearIdx = -1;
            UpdateCount();
        }

        private void UpdateCount()
        {
            Count = rearIdx < 0 ? 0 : rearIdx - frontIdx + 1;
        }
    }
}