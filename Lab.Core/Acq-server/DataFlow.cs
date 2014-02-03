using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using ProtoBuf.Meta;

namespace Lab.Acq
{
    public class DataFlow<T> : Disposable, IOutputFileAndPipe
        where T:class, new()
    {
        readonly TypeModel customSerializer;
        protected readonly RingBuffer<T> SourceRingBuffer;
        readonly Thread mainRetrievalThread;
        readonly Thread pipeOutThread;
        readonly ManualResetEventSlim cancelSignal = new ManualResetEventSlim();

        readonly CancellationTokenSource cancelTokenSource;
        readonly CancellationToken cancelToken;

        public DataFlow(RingBuffer<T> sourceRingBuffer, TypeModel customSerializer)
        {
            if (sourceRingBuffer == null)
                throw new ArgumentNullException();
            this.SourceRingBuffer = sourceRingBuffer;

            this.customSerializer = customSerializer;

            cancelTokenSource = new CancellationTokenSource();
            cancelToken = cancelTokenSource.Token;

            mainReaderDelegate = new Action<T>(MainReader);
            runExtraActionsDelegate = new WaitCallback(RunExtraActions);
            memStreamCopyDelegate = new Action<MemoryStream, MemoryStream>(MemStreamCopy);
            pipeOutReaderDelegate = new Action<MemoryStream>(PipeOutReader);

            mainRetrievalThread = new Thread(new ThreadStart(mainRetrievalLoop));
            mainRetrievalThread.IsBackground = true;
            mainRetrievalThread.Name = "AcqDataFlow MainRetrieval";
            pipeOutThread = new Thread(new ThreadStart(pipeOutLoop));
            pipeOutThread.IsBackground = true;
            pipeOutThread.Name = "AcqDataFlow PipeOutThread";
            mainRetrievalThread.Start();
            pipeOutThread.Start();
        }

        RingBuffer<MemoryStream> pipeOutRingBuffer =
            new RingBuffer<MemoryStream>(4, () => new MemoryStream()) { KeepFresh = true };

        void pipeOutLoop()
        {
            while (!cancelSignal.Wait(0))
            {
                var ps = pipeStream.Value;
                if (ps != null)
                {
                    bool notCancelled = true;
                    try
                    {
                        if (ps.IsConnected)
                            notCancelled = pipeOutRingBuffer.Read(pipeOutReaderDelegate, cancelToken);
                        else
                        {
                            ps.WaitForConnection();
                        }
                    }
                    catch (IOException e)
                    {
                        if (!cancelSignal.IsSet)
                            Trace.TraceWarning("Pipe IO error on {0} : {1} ", this.ToString(), e.Message);
                        pipeStream.ReplaceWithNew(null);
                    }
                    catch (OperationCanceledException e)
                    {
                        Trace.TraceWarning("Pipe operation canceled on {0} : {1}", this.ToString(), e.Message);
                        pipeStream.ReplaceWithNew(null);
                    }
                    if (!notCancelled)
                        this.Dispose();
                }
                else
                    cancelSignal.Wait(100);
            }
            Dispose();
        }

        public override string ToString()
        {
            return this.GetType().FriendlyNameForGenerics();
        }

        readonly Action<MemoryStream> pipeOutReaderDelegate;
        void PipeOutReader(MemoryStream m)
        {
            m.Seek(0, SeekOrigin.Begin);
            var ps = pipeStream.Value;
            if (ps == null)
                return;
            if (ps.IsConnected && ps.CanWrite)
            {
                m.CopyTo(ps);
            }
        }



        void mainRetrievalLoop()
        {
            while (!cancelSignal.Wait(0))
            {
                bool ok = SourceRingBuffer.Read(mainReaderDelegate, cancelToken);
                if (!ok)
                    this.Dispose();
            }
            Dispose();
        }

        /// <summary>
        /// Add event listeners to handle data.  These listeners will be run on
        /// a separate thread, but a data buffer won't be released back to the ring
        /// until they complete
        /// </summary>
        public event Action<T> GotNewData;

        readonly Action<T> mainReaderDelegate;
        readonly MemoryStream mainMemStream = new MemoryStream();


        readonly WaitCallback runExtraActionsDelegate;
        readonly AutoResetEvent extraActionsComplete = new AutoResetEvent(false);
        void RunExtraActions(object videoFrame)
        {
            var a = GotNewData;
            if (a != null)
            {
                T f = videoFrame as T;
                if (f != null)
                    a(f);
            }
            extraActionsComplete.Set();
        }

        readonly Action<MemoryStream, MemoryStream> memStreamCopyDelegate;
        void MemStreamCopy(MemoryStream src, MemoryStream dest)
        {
            src.Seek(0, SeekOrigin.Begin);
            dest.Seek(0, SeekOrigin.Begin);
            src.CopyTo(dest);
            dest.Seek(0, SeekOrigin.Begin);
            src.Seek(0, SeekOrigin.Begin);
        }


        long lastFrameNumber = 0;

        public virtual void WarnOnFrameDrop(T newData)
        {
            VideoFrame f = newData as VideoFrame;
            if (f == null)
                return;

            long thisFrameNumber = f.FrameNumber;
            long nDropped = thisFrameNumber - lastFrameNumber - 1;
            if (nDropped > 0)
                Trace.TraceWarning("Disk save missed {0} frames!", nDropped);
            lastFrameNumber = thisFrameNumber;
        }

        void MainReader(T f)
        {
            ThreadPool.QueueUserWorkItem(runExtraActionsDelegate, f);

            mainMemStream.Seek(0, SeekOrigin.Begin);
            customSerializer.NullSafe_Serialize_Int32Prefix<T>(mainMemStream, f);
            mainMemStream.SetLength(mainMemStream.Position);

            mainMemStream.Seek(0, SeekOrigin.Begin);
            bool ok = pipeOutRingBuffer.TryCopyIn(
                memStreamCopyDelegate, mainMemStream);


            mainMemStream.Seek(0, SeekOrigin.Begin);
            var fs = fileStream.Value;
            lock (fileStream.AccessLock)
            {
                if ((fs != null) && (fs.CanWrite))
                    mainMemStream.CopyTo(fs);
            }

            WarnOnFrameDrop(f);

            extraActionsComplete.WaitOne();
        }

        readonly SyncSwapValue<NamedPipeServerStream> pipeStream =
            new SyncSwapValue<NamedPipeServerStream>(pipeName =>
                new NamedPipeServerStream(
                    PipePrefix + pipeName, PipeDirection.Out,
                    1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous));


        public void SetOutputPipe(string pipeName)
        {
            pipeStream.ReplaceWithNew(pipeName);
        }


        readonly SyncSwapValue<FileStream> fileStream =
            new SyncSwapValue<FileStream>(fileName =>
                new FileStream(fileName, FileMode.Create, FileAccess.Write,
                    FileShare.Read, bufferSize: Environment.SystemPageSize, useAsync: true));

        public void SetOutputFile(string fileName)
        {
            fileStream.ReplaceWithNew(fileName);
        }


        public void FlushOutputFile()
        {
            lock (fileStream.AccessLock)
            {
                var fs = fileStream.Value;
                if (fs != null)
                    fs.Flush();
            }
        }

        protected override void RunOnceDisposer()
        {
            cancelSignal.Set();
            cancelTokenSource.Cancel();
            pipeOutThread.Join();
            mainRetrievalThread.Join();
            Trace.TraceInformation("Finished disposing " + this.ToString());
        }

        public const string PipePrefix = @"\\.\pipe\";
    }


}
