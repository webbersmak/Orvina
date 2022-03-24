namespace Orvina.Engine.Support
{
    /// <summary>
    /// small, efficient queue
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class SimpleQueue<T>
    {
        private int frontIdx;//[x][][]
        private int rearIdx;//[][][x]

        private T[] nodes;

        public SimpleQueue(int size = 0)
        {
            Reset(size);
        }

        public void Reset(int size = 0)
        {
            nodes = new T[size];
            frontIdx = -1;
            rearIdx = -1;
        }

        public void Clear()
        {
            frontIdx = -1;
            rearIdx = -1;
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

        public bool Any
        {
            get
            {
                return Count > 0;
            }
        }

        public int Count
        {
            get
            {
                return rearIdx < 0 ? 0 : rearIdx - frontIdx + 1;//[2][3][4]
            }
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
    }
}