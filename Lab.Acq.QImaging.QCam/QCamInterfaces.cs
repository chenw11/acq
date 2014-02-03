
namespace Lab.Acq
{
    public interface IQCam : ICamera { }

    public interface IQCamFlow : IQCam, ICameraFlow { }

    public class QCamCoordinator : ICoordinator<IQCamFlow>
    {
        public string DefaultCoordinationName { get { return "Lab.Acq.QImaging.QCam"; } }
    }
}
