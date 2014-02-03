using System;
using System.IO.Pipes;
using ProtoBuf.Meta;
using System.Collections.Generic;
using System.IO;

namespace Lab.Acq
{
    public interface IPipeReader<out TIFlow, TData> : IDisposable
        where TIFlow : class, IFlow
    {
        TIFlow Flow { get; }
        IStartStopDataSource<TData> DataSource { get; }
        bool TerminateServerOnStop { get; set; }
        string CoordName { get; }
    }

    public class PipeReader<TIFlow, TClient, TData> : Disposable, IPipeReader<TIFlow, TData>
        where TIFlow : class, IFlow
        where TClient : RemoteClientFlowBase, TIFlow, new()
    {
        readonly TypeModel customSerializer;
        readonly string coordName;

        public string CoordName { get { return coordName; } }

        const string PipePrefix = @"\\.\pipe\";

        readonly NamedPipeClientStream clientStream;
        readonly RpcClient rpcClient;
        readonly TClient flowClient;
        public TIFlow Flow { get { return flowClient; } }


        public PipeReader(string coordName, TypeModel customSerializer)
        {
            if (string.IsNullOrWhiteSpace(coordName))
                throw new ArgumentException("coordName");

            this.coordName = coordName;
            this.customSerializer = customSerializer;

            
            clientStream = new NamedPipeClientStream(coordName);
            rpcClient = new RpcClient(clientStream, typeof(TIFlow), customSerializer);
            try
            {
                clientStream.Connect(100);
                rpcClient.SendInitialHandshake();
            }
            catch (IOException)
            {
                throw new ApplicationException("Error connecting to remote service. " +
                    " Check that the server application is running, and that any hardware it depends on is powered on.");                   
            }

            //this.flowClient = client;
            this.flowClient = new TClient();
            flowClient.RpcClient = rpcClient;
            reader = new SynchronousDataReader<TClient, TData>(coordName + ".data", flowClient, customSerializer);
        }

        readonly SynchronousDataReader<TClient, TData> reader;

        public bool IsRunning { get { return reader.IsRunning; } }

        public IStartStopDataSource<TData> DataSource { get { return reader; } }

        public bool TerminateServerOnStop
        {
            get { return flowClient.RpcClient.RequestRemoteTerminationOnDispose; }
            set { flowClient.RpcClient.RequestRemoteTerminationOnDispose = value; }
        }

        protected override void RunOnceDisposer()
        {
            reader.TryDispose();
            flowClient.TryDispose();
        }
    }

    public class CameraReader<TIFlow, TClient> : PipeReader<TIFlow, TClient, VideoFrame>
        where TIFlow : class, ICameraFlow
        where TClient : RemoteClientFlowBase, TIFlow, new()
    {
        public CameraReader(string coordName, TypeModel customSerializer)
            : base(coordName, customSerializer)
        {

        }

        public IPipeReader<ICameraFlow, VideoFrame> Upcast { get { return this; } }
    }

}
