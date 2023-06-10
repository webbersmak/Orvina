namespace Orvina.Engine.Support
{
    internal sealed class SimpleDictionary<T>
    {
        private int currentIdx = -1;
        private T[] nodes = Array.Empty<T>();

        public int Count
        {
            get; private set;
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
            Count++;
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
            Count = 0;
            currentIdx = -1;
            nodes = Array.Empty<T>();
        }
    }
}