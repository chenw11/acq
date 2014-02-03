using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using ProtoBuf.Meta;

namespace Lab.Acq
{
    public class SynchronousDataReader<TClient, TData> : Disposable, IStartStopDataSource<TData>
        where TClient : RemoteClientFlowBase, IFlow
    {
        static long seqNumber = 0;

        //readonly PipeReader<TIFlow, TClient, TData> parent;

        readonly string dataPipeNamePrefix;
        readonly Thread dataReadThread;
        int instanceSeqNumber = 0;
        readonly AutoResetEvent startLoop = new AutoResetEvent(false);
        readonly ManualResetEvent reqDispose = new ManualResetEvent(false);

        public override string ToString() { return this.GetType().Name + " : " + dataPipeNamePrefix; }
        public event EventHandler<TData> NewData;

        readonly TClient flowClient;
        readonly TypeModel customSerializer;

        public SynchronousDataReader(string dataPipePrefix, TClient flowClient, TypeModel customSerializer)
        {
            this.flowClient = flowClient;
            this.customSerializer = customSerializer;
            this.dataPipeNamePrefix = dataPipePrefix + Interlocked.Increment(ref seqNumber);
            this.dataReadThread = new Thread(dataReadThreadProc);
            dataReadThread.IsBackground = true;
            dataReadThread.Name = "Data read thread for " + this.ToString();
            dataReadThread.Start();
        }

        readonly AutoResetEvent pipeStateChanged = new AutoResetEvent(false);
        void dataReadThreadProc()
        {
            WaitHandle[] waitHandles = new WaitHandle[] { startLoop, reqDispose };

            if (CAS(State.Idle, State.NotReady) != State.NotReady)
                throw new InvalidOperationException("This method should have only been called once per instance.");

            while (true)
            {
                // "stop"-ed
                Set(State.Idle);

                if (WaitHandle.WaitAny(waitHandles) != 0)
                    return;

                string dataPipeName = dataPipeNamePrefix + "." + Interlocked.Increment(ref instanceSeqNumber);

                // we use exactly 1 pipe per start/stop to ensure there are no residual bits in the pipe
                // that might accidentally carry over to a new start/stop
                using (var dataStream = flowClient.CreateDataPipe(dataPipeName))
                {
                    pipeStateChanged.Set();

                    // "start"-ed
                    Set(State.ReadPending);

                    while (true)
                    {
                        // try to switch from ReadPending to Reading
                        if (!TrySwitch(State.ReadPending, State.Reading))
                            break;

                        TData data = customSerializer.NullSafe_Deserialize_Int32Prefix<TData>(dataStream);

                        if (!TrySwitch(State.Reading, State.InCallback))
                            break;

                        var e = this.NewData;
                        if (e != null)
                            e(this, data);

                        if (!TrySwitch(State.InCallback, State.ReadPending))
                            break;
                    }
                }

                pipeStateChanged.Set();

                // force server to close pipe
                if (base.IsDisposed)
                    return;
                else
                    flowClient.SetOutputPipe(string.Empty);

                isRunningReadLoop = false;
            }


        }

        // returns expected == (CAS(ref state, nextState, expected))
        // with error reporting
        bool TrySwitch(State expectedState, State nextState)
        {
            if (base.IsDisposed)
                return false;

            State s = CAS(nextState, expectedState);
            if (s == expectedState)
                return true;
            if (s != State.RequestStop)
                Trace.TraceWarning("Unexpected state for pipe reader source.  Quitting.");
            return false;
        }

        enum State : int
        {
            /// <summary>
            /// Uninitialized, = 0 (default)
            /// </summary>
            NotReady = 0,

            /// <summary>
            /// Waiting for user to call Start()
            /// </summary>
            Idle = 1,

            /// <summary>
            /// About to start a read
            /// </summary>
            ReadPending = 10,

            /// <summary>
            /// Blocked, waiting for a complete message over the pipe, user hasn't called Stop()
            /// </summary>
            Reading = 11,

            /// <summary>
            /// Start() has been called, and message processing is underway
            /// </summary>
            ReadComplete = 12,


            /// <summary>
            /// Data read thread is executing the user-supplied callback
            /// </summary>
            InCallback = 20,


            /// <summary>
            /// User called Stop() but the data reader thread hasn't responded yet
            /// </summary>
            RequestStop = 100,

            /// <summary>
            /// Reader thread has quit and everything is disposed
            /// </summary>
            Disposed = 0xFF
        }

        int _curState = 0; // NotRead

        /// <summary>
        /// Compares the current state with the 'comparand' and if they're equal, replaces the current state with newValue
        /// Regardless, the old value is returned.
        /// </summary>
        /// <param name="newValue">This replaces current value if current == comparand</param>
        /// <param name="comparand">Test if current == this arg</param>
        /// <returns>Old value which may or may not have been replaced</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        State CAS(State newValue, State comparand)
        {
            return (State)(Interlocked.CompareExchange(ref _curState, (int)newValue, (int)comparand));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        State Set(State newValue)
        {
            return (State)(Interlocked.Exchange(ref _curState, (int)newValue));
        }

        bool isRunningReadLoop = false;
        readonly object startStopLock = new object();

        public void Start()
        {
            base.AssertNotDisposed();
            lock (startStopLock)
            {
                if (isRunningReadLoop)
                    throw new InvalidOperationException();
                isRunningReadLoop = true;

                pipeStateChanged.Reset();
                startLoop.Set();
                pipeStateChanged.WaitOne();

                flowClient.Start();
            }
        }

        public void Stop()
        {
            if (!isRunningReadLoop)
                return;

            pipeStateChanged.Reset();
            var prevState = Set(State.RequestStop);
            pipeStateChanged.WaitOne();
            flowClient.Stop();
        }

        public bool IsRunning
        {
            get { return isRunningReadLoop; }
            set
            {
                if (isRunningReadLoop == value)
                    return;
                if (value)
                    Start();
                else
                    Stop();
            }
        }

        protected override void RunOnceDisposer()
        {
            reqDispose.Set();
            State old = Set(State.RequestStop);
            if (old == State.Reading) // only possible deadlock
                dataReadThread.Join(500);
            else
                dataReadThread.Join();
        }
    }

}
