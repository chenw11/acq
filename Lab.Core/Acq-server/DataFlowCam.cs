using ProtoBuf.Meta;

namespace Lab.Acq
{
    public class DataFlowCam : DataFlow2<VideoFrame, ICameraHAL>, ICameraFlow
    {
        public DataFlowCam(ICameraHAL hal, int ringBufferCapacity, TypeModel customSerializer)
            : base(hal, ringBufferCapacity, customSerializer) { }

        public CameraAttributes CameraAttributes { get { return hal.CameraAttributes; } }

        public VideoSettingsStatic SettingsStatic
        {
            get { return hal.SettingsStatic; }
            set { hal.SettingsStatic = value; }
        }

        public VideoSettingsDynamic SettingsDynamic
        {
            get { return hal.SettingsDynamic; }
            set { hal.SettingsDynamic = value; }
        }
    }
}
