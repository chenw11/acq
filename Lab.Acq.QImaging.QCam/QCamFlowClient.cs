using System;

namespace Lab.Acq.Clients
{
    [RpcWrapper(typeof(IQCamFlow))]
    public class QCamFlowClient : RemoteClientCameraFlow, IQCamFlow
    {
        public QCamFlowClient(IRpcClient rpcClient)
        {
            if (rpcClient == null)
                throw new ArgumentNullException();
            base.RpcClient = rpcClient;
        }

        public QCamFlowClient() { }
    }
}
