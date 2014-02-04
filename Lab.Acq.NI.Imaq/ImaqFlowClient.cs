using System;

namespace Lab.Acq.Clients
{
    [RpcWrapper(typeof(IImaqFlow))]
    public class ImaqFlowClient : RemoteClientCameraFlow, IImaqFlow
    {
        public ImaqFlowClient(IRpcClient rpcClient)
        {
            if (rpcClient == null)
                throw new ArgumentNullException();
            base.RpcClient = rpcClient;
        }

        public ImaqFlowClient() { }
    }
}
