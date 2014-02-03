using Lab.IO;
using ProtoBuf.Meta;
using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Lab.Acq
{
    public class DataServer<IFlow> : Disposable, IDataServer<IFlow>
        where IFlow : IDisposable
    {
        public DataServer(ICoordinator<IFlow> coordinator, 
            Func<int,TypeModel, IFlow> flowBuilder,
            TypeModel customSerializer)
        {
            if (coordinator == null)
                throw new ArgumentNullException();
            if (flowBuilder == null)
                throw new ArgumentNullException();
            this.coordinator = coordinator;
            this.customSerializer = customSerializer;
            this.flowBuilder = flowBuilder;
        }

        int ringBufferCapacity = 4;
        public int RingBufferCapacity
        {
            get { return ringBufferCapacity; }
            set
            {
                if (value > 1)
                    ringBufferCapacity = value;
                else
                    throw new ArgumentOutOfRangeException();
            }
        }

        readonly Func<int, TypeModel, IFlow> flowBuilder;
        protected readonly TypeModel customSerializer;
        readonly ICoordinator<IFlow> coordinator;
        
        static bool TryReleaseNamedSemaphore(string semaphoreName)
        {
            if (!string.IsNullOrWhiteSpace(semaphoreName))
            {
                Semaphore s;
                try
                {
                    s = Semaphore.OpenExisting(semaphoreName,
                        System.Security.AccessControl.SemaphoreRights.Modify);
                }
                catch (WaitHandleCannotBeOpenedException) { return false; }
                s.Release();
                Trace.TraceInformation("Released semaphore " + semaphoreName);
                return true;
            }
            return false;
        }

        readonly ManualResetEvent objectCancel = new ManualResetEvent(false);
        IFlow latestFlow;

        async Task RunServerAsync(string coordinationName, CancellationToken cancelToken, bool restartOnDisconnect)
        {
            bool terminate = false;
            int loopCount = 0;
            do
            {
                using (var controlPipe = new NamedPipeServerStream(coordinationName, PipeDirection.InOut, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                {
                    Func<AsyncCallback, object, IAsyncResult> beginConnect = controlPipe.BeginWaitForConnection;
                    Action<IAsyncResult> endConnect = controlPipe.EndWaitForConnection;

                    Task connection = Task.Factory.FromAsync(beginConnect, endConnect, null);

                    if (loopCount++ == 0)
                        TryReleaseNamedSemaphore(coordinationName);

                    await connection;

                    bool ok = controlPipe.IsConnected && LengthPrefixMessenger.ServerHandshake(controlPipe);
                    if (!ok)
                    {
                        Trace.TraceInformation("Got connection, but it was lost or handshake failed.");
                        continue;
                    }

                    Trace.TraceInformation("Setting up hardware for " + coordinationName + "...");
                    using (IFlow flow = flowBuilder(RingBufferCapacity, customSerializer))
                    {
                        latestFlow = flow;
                        using (var rpc = new RpcServer<IFlow>(controlPipe, flow, customSerializer))
                        {
                            Trace.TraceInformation("Started new " + typeof(IFlow).FriendlyNameForGenerics());
                            bool disconnect = false;
                            while (!cancelToken.WaitHandle.WaitOne(0) && !terminate && !disconnect && !objectCancel.WaitOne(0))
                                rpc.ProcessMessage(out disconnect, out terminate);
                        }
                    }
                    latestFlow = default(IFlow);
                }
            } 
            while (restartOnDisconnect && !cancelToken.WaitHandle.WaitOne(0) && !terminate
                && !objectCancel.WaitOne(0));
            Trace.TraceInformation("RunServerAsync completed ok");
        }


        /// <summary>
        /// Launches the data server asynchronously, and optionally waits until it is ready to serve requests before returning
        /// </summary>
        public Task LaunchServer(string coordName, CancellationToken cancelToken, bool blockUntilReady, bool restartOnDisconnect)
        {
            Semaphore s = blockUntilReady ? DataServer.BuildNamedSingletonSemaphore(coordName) : null;
            Task t = RunServerAsync(coordName, cancelToken, restartOnDisconnect);
            if (blockUntilReady)
                s.WaitOne();
            return t;
        }

        public Task LaunchServer(string coordName, CancellationToken cancelToken, bool blockUntilReady)
        {
            return LaunchServer(coordName, cancelToken, blockUntilReady, restartOnDisconnect: false);
        }


        public string DefaultCoordinationName { get { return coordinator.DefaultCoordinationName; } }

        public void StandaloneServerMain(string[] args)
        {
            Trace.Listeners.Add(new SimpleConsoleTraceListener());

            string coordName = DefaultCoordinationName;
            if (args.Length > 0)
                coordName = args[0];

            Trace.TraceInformation("Standalone data server using coordination name '{0}'", coordName);

            CancellationTokenSource cancel = new CancellationTokenSource();
            string suffix = "";
            string procName = Process.GetCurrentProcess().ProcessName;
            if (procName.ToLower().Contains("fake"))
                suffix = " (FAKE)";
            Console.Title = string.Format("{0}{1} standalone data server.  Press ESC to quit", coordName, suffix);
            Task serverTask = LaunchServer(coordName, cancel.Token, blockUntilReady : false, restartOnDisconnect: true);
            serverTask.ContinueWith(t =>
            {
                AggregateException e = t.Exception;
                if (e == null)
                    cancel.Cancel();
                else
                {
                    Console.WriteLine(" = = = = =  E R R O R  = = = = = ");
                    foreach (var ee in e.InnerExceptions)
                        Trace.TraceError(ee.Message);
                    if (e.InnerExceptions.Count > 0)
                        Lab.UI.Balloon.ShowBalloon("Data server error", e.InnerExceptions[0].Message);
                    Console.WriteLine("Press ESC to quit");
                }
            });
            DataServer.ConsoleWait(cancel);
            Trace.TraceInformation("Standalone server will terminate.");
            this.Dispose();
        }

        protected override void RunOnceDisposer()
        {
            objectCancel.Set();
            latestFlow.TryDispose();
        }
    }

}
