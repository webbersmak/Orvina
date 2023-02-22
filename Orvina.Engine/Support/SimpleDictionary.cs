namespace Orvina.Engine.Support
{
    internal sealed class SimpleDictionary<T>
    {
        private int _count;
        private int currentIdx = -1;
        private T[] nodes = Array.Empty<T>();

        public int Count
        {
            get
            {
                return _count;
            }
        }

        public T this[int key]
        {
            get
            {
                return nodes[key];
            }
        }

        public int Add(T value)
        {
            _count++;
            var desiredIdx = currentIdx + 1; //on first enqueu will be 0
            if (desiredIdx > nodes.Length - 1) //need to grow
            {
                //double size
                Array.Resize(ref nodes, (desiredIdx + 1) * 2);
            }

            nodes[desiredIdx] = value;
            currentIdx = desiredIdx;
            return currentIdx;
        }

        public void Clear()
        {
            _count = 0;
            currentIdx = -1;
            nodes = Array.Empty<T>();
        }
    }
}