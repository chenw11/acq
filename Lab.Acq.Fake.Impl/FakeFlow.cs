using ProtoBuf.Meta;

namespace Lab.Acq
{
    class FakeFlow : DataFlowCam, IFakeFlow
    {
        public FakeFlow(int ringBufferCapacity, TypeModel customSerializer)
            : base(new FakeHAL(), ringBufferCapacity, customSerializer)
        {
        }

        public int TEST_InterFrameInterval
        {
            get { return ((FakeHAL)hal).TEST_InterFrameInterval; }
            set { ((FakeHAL)hal).TEST_InterFrameInterval = value; }
        }


        public void TEST_Define_Camera(int ccdWidth, int ccdHeight, int bitsPerPixel, BinningMode[] supportedBinning)
        {
            ((FakeHAL)hal).TEST_Define_Camera(ccdWidth, ccdHeight, bitsPerPixel, supportedBinning);
        }
    }
}
