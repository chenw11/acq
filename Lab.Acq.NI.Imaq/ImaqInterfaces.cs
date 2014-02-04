
namespace Lab.Acq
{
    public interface IImaq : ICamera { }

    public interface IImaqFlow : IImaq, ICameraFlow { }

    public class ImaqCoordinator : ICoordinator<IImaqFlow>
    {
        public string DefaultCoordinationName { get { return "Lab.Acq.NI.Imaq"; } }
    }
}
