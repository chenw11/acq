using System.Threading;

namespace Lab.Acq
{
    public interface IFake : ICamera
    {
        int TEST_InterFrameInterval { get; set; }

        void TEST_Define_Camera(int ccdWidth, int ccdHeight, int bitsPerPixel, BinningMode[] supportedBinning);
    }

    public interface IFakeFlow : IFake, ICameraFlow { }

    public class FakeCoordinator : ICoordinator<IFakeFlow>
    {
        public string DefaultCoordinationName { get { return "Lab.Acq.Fake"; } }
    }
}
