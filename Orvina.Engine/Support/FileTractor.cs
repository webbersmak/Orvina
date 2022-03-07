using Orvina.Engine.Support;

namespace Orvina.Engine
{
    internal sealed class FileTractor
    {
        private readonly Dictionary<int, AsyncContext> asyncReads = new(32);
        private readonly SimpleQueue<CompleteFile> dataQ = new(32);

        private readonly SpinWait spinWait = new();
        private SpinLock asyncLock = new();
        private int callId;
        private SpinLock dataQLock = new();

        public bool TryEnqueue(string file)
        {
            try
            {
                var context = new AsyncContext() { fileName = file };

                var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                var data = new byte[fs.Length];

                context.stream = fs;
                context.data = data;

                LockHelper.RunLock(ref asyncLock, () =>
                {
                    asyncReads.Add(++callId, context);
                    fs.BeginRead(data, 0, data.Length, OnFileCallback, callId);
                });
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
                    if (dataQ.TryDequeue(out file1))
                    {
                        return true;
                    }

                    return false;
                }, out file))
                {
                    return true;
                }

                spinWait.SpinOnce();
            }
        }

        private void OnFileCallback(IAsyncResult ar)
        {
            var callbackId = (int)ar.AsyncState;

            var context = LockHelper.RunLock(ref asyncLock, () =>
            {
                var context = asyncReads[callbackId];
                asyncReads.Remove(callbackId);
                return context;
            });

            LockHelper.RunLock(ref dataQLock, () =>
            {
                dataQ.Enqueue(new CompleteFile { data = context.data, fileName = context.fileName });
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