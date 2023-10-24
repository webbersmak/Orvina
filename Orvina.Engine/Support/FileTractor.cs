using Orvina.Engine.Support;

namespace Orvina.Engine
{
    internal sealed class FileTractor : IDisposable
    {
        private readonly SimpleDictionary<AsyncFile> asyncReads = new();
        private readonly SimpleQueue<AsyncFile> dataQ = new();

        private readonly ManualResetEventSlim manualReset = new();

        public void Dispose()
        {
            manualReset.Dispose();
        }

        public bool TryEnqueue(string file)
        {
            try
            {
                var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                var data = new byte[fs.Length];

                var context = new AsyncFile
                {
                    fileName = file,
                    stream = fs,
                    data = data
                };

                int callId;
                lock (asyncReads)
                {
                    callId = asyncReads.Add(context);
                }

                try
                {
                    fs.BeginRead(data, 0, data.Length, OnFileCallback, callId);
                }
                catch
                {
                    fs.Dispose();
                    //throw;
                    return false;
                }
            }
            catch
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
        public bool TryGetFile(out AsyncFile file)
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

                manualReset.Wait();
            }
        }

        private void OnFileCallback(IAsyncResult ar)
        {
            var callbackId = (int)ar.AsyncState;

            AsyncFile context;
            lock (asyncReads)//don't use spinlock here
            {
                context = asyncReads[callbackId];
            }

            lock (dataQ)
            {
                dataQ.Enqueue(context);
                manualReset.Set();
            }

            context.stream.Dispose();
        }

        public struct AsyncFile
        {
            public byte[] data;
            public string fileName;
            public Stream stream;
        }
    }
}