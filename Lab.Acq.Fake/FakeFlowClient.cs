using System;

namespace Lab.Acq.Clients
{
    [RpcWrapper(typeof(IFakeFlow))]
    public class FakeFlowClient : RemoteClientCameraFlow, IFakeFlow
    {
        public FakeFlowClient(IRpcClient rpcClient)
        {
            if (rpcClient == null)
                throw new ArgumentNullException();
            base.RpcClient = rpcClient;
        }

        public FakeFlowClient() { }

        public int TEST_InterFrameInterval
        {
            get { return RpcClient.RemoteCall<int>("get_InterFrameInterval"); }
            set { RpcClient.RemoteCallVoid("set_InterFrameInterval", value); }
        }


        public void TEST_Define_Camera(int ccdWidth, int ccdHeight, int bitsPerPixel, BinningMode[] supportedBinning)
        {
            RpcClient.RemoteCallVoid("TEST_Define_Camera", ccdWidth, ccdHeight, bitsPerPixel, supportedBinning);
        }
    }
}
