using Orvina.Engine.Support;

namespace Orvina.Engine
{
    internal sealed class FileTractor
    {
        private static readonly CompleteFile EmptyFile = new();
        private readonly Dictionary<int, AsyncContext> asyncReads = new(32);
        private readonly SimpleQueue<CompleteFile> dataQ = new(32);

        private bool active;
        private int callId;

        public void Enqueue(string file)
        {
            var context = new AsyncContext() { fileName = Path.GetFileName(file) };

            lock (this)
            {
                active = true;
                callId++;

                //begin async read
                var fs = File.OpenRead(file);
                var data = new byte[fs.Length];

                context.stream = fs;
                context.data = data;

                asyncReads.Add(callId, context);
                fs.BeginRead(data, 0, data.Length, OnFileCallback, callId);
            }
        }

        public bool TryGetFile(out CompleteFile file)
        {
            lock (this)
            {
                while (active)
                {
                    lock (dataQ)
                    {
                        if (dataQ.TryDequeue(out file))
                        {
                            active = dataQ.Any;
                            return true;
                        }
                        else
                        {
                            active = asyncReads.Count > 0;
                        }
                    }
                }
            }

            file = EmptyFile;
            return false;
        }

        private void OnFileCallback(IAsyncResult ar)
        {
            lock (this)
            {
                var callId = (int)ar.AsyncState;
                var context = asyncReads[callId];
                asyncReads.Remove(callId);
                try
                {
                    context.stream.EndRead(ar);
                }
                catch { }
                context.stream.Dispose();
                lock (dataQ)
                {
                    dataQ.Enqueue(new CompleteFile { data = context.data, fileName = context.fileName });
                }
            }
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