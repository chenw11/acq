using System;
using System.IO.Pipes;

namespace Lab.Acq
{
    public abstract class RemoteClientBase : Disposable
    {
        private IRpcClient rpcClient;

        public IRpcClient RpcClient
        {
            get
            {
                if (rpcClient == null)
                    throw new InvalidOperationException("Set the RpcClient property before using this object.");
                return rpcClient;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                if (rpcClient == null)
                    rpcClient = value;
                else
                    throw new InvalidOperationException("Can only set this property once!");
            }
        }

        protected RemoteClientBase() { }

        protected override void RunOnceDisposer()
        {
            rpcClient.TryDispose();
        }
    }

    public class RemoteClientStartStop : RemoteClientBase, IStartStop
    {
        public void Start() { RpcClient.RemoteCallVoid("Start"); }
        public void Stop() { RpcClient.RemoteCallVoid("Stop"); }
        public bool IsRunning { get { return RpcClient.RemoteCall<bool>("get_IsRunning"); } }
    }

    public class RemoteClientFlowBase : RemoteClientStartStop, IOutputFileAndPipe
    {
        public void SetOutputFile(string filePath)
        {
            RpcClient.RemoteCallVoid("SetOutputFile", filePath);
        }

        void IOutputFileAndPipe.SetOutputPipe(string pipeName)
        {
            RpcClient.RemoteCallVoid("SetOutputPipe", pipeName);
        }

        /// <summary>
        /// Returns an object that will stream data from the remote source.
        /// The stream returned has already been connected.
        /// </summary>
        public NamedPipeClientStream CreateDataPipe(string pipeName)
        {
            const string PipePrefix = @"\\.\pipe\";
            ((IOutputFileAndPipe)this).SetOutputPipe(pipeName);
            NamedPipeClientStream s = new NamedPipeClientStream(".", PipePrefix + pipeName, PipeDirection.In);
            s.Connect();
            return s;
        }
    }

    public class RemoteClientCameraFlow : RemoteClientFlowBase, ICameraFlow
    {
        public CameraAttributes CameraAttributes
        {
            get { return RpcClient.RemoteCall<CameraAttributes>("get_CameraAttributes"); }
        }

        public VideoSettingsStatic SettingsStatic
        {
            get { return RpcClient.RemoteCall<VideoSettingsStatic>("get_SettingsStatic"); }
            set { RpcClient.RemoteCallVoid("set_SettingsStatic", value); }
        }

        public VideoSettingsDynamic SettingsDynamic
        {
            get { return RpcClient.RemoteCall<VideoSettingsDynamic>("get_SettingsDynamic"); }
            set { RpcClient.RemoteCallVoid("set_SettingsDynamic", value); }
        }


    }
}
