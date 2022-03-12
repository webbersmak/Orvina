using Orvina.Engine.Support;

namespace Orvina.Engine
{
    internal sealed class FileTractor : IDisposable
    {
        private readonly SimpleDictionary<AsyncContext> asyncReads = new();
        private readonly SimpleQueue<CompleteFile> dataQ = new();

        private readonly ManualResetEventSlim manualReset = new();
        private SpinLock dataQLock = new();

        public void Dispose()
        {
            manualReset.Dispose();
        }

        public bool TryEnqueue(string file)
        {
            try
            {
                var context = new AsyncContext() { fileName = file };

                var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                var data = new byte[fs.Length];

                context.stream = fs;
                context.data = data;

                lock (asyncReads)
                {
                    var callId = asyncReads.Add(context);
                    try
                    {
                        fs.BeginRead(data, 0, data.Length, OnFileCallback, callId);
                    }
                    catch
                    {
                        asyncReads.Remove(callId);
                        fs.Dispose();
                        throw;
                    }
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
                if (LockHelper.RunLock(ref dataQLock, (out CompleteFile file1) =>
                {
                    return dataQ.TryDequeue(out file1);
                }, out file))
                {
                    return true;
                }
                else
                {
                    manualReset.Wait();
                }
            }
        }
        private void OnFileCallback(IAsyncResult ar)
        {
            var callbackId = (int)ar.AsyncState;

            AsyncContext context;
            lock (asyncReads)//don't use spinlock here
            {
                context = asyncReads[callbackId];
                asyncReads.Remove(callbackId);
            }

            LockHelper.RunLock(ref dataQLock, () =>
            {
                dataQ.Enqueue(new CompleteFile { data = context.data, fileName = context.fileName });
                manualReset.Set();
            });

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