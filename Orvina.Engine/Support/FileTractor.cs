using Orvina.Engine.Support;

namespace Orvina.Engine
{
    internal sealed class FileTractor
    {
        private readonly Dictionary<int, AsyncContext> asyncReads = new(32);
        private readonly SimpleQueue<CompleteFile> dataQ = new(32);

        private int callId;

        public bool TryEnqueue(string file)
        {
            try
            {
                var context = new AsyncContext() { fileName = Path.GetFileName(file) };

                var fs = new FileStream(file, FileMode.Open, FileAccess.Read,FileShare.Read, 4096, true);
                var data = new byte[fs.Length];

                context.stream = fs;
                context.data = data;

                lock (asyncReads)
                {
                    asyncReads.Add(++callId, context);
                    fs.BeginRead(data, 0, data.Length, OnFileCallback, callId);
                }
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// this method is why its called a tractor. Be safe.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public bool TryGetFile(out CompleteFile file)
        {
            while (true)
            {
                lock (dataQ)
                {
                    if (dataQ.TryDequeue(out file))
                    {
                        return true;
                    }
                }
            }
        }

        private void OnFileCallback(IAsyncResult ar)
        {
            AsyncContext context;
            var callbackId = (int)ar.AsyncState;

            lock (asyncReads)
            {
                context = asyncReads[callbackId];
                asyncReads.Remove(callbackId);
            }

            lock (dataQ)
            {
                dataQ.Enqueue(new CompleteFile { data = context.data, fileName = context.fileName });
            }
            context.stream.Dispose();
        }

        public struct CompleteFile
        {
            public byte[] data;
            public string fileName;
        }

        private struct AsyncContext
        {
            public byte[] data;
            public string fileName;
            public Stream stream;
        }
    }
}