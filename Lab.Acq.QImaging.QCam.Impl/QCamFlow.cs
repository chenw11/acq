using ProtoBuf.Meta;

namespace Lab.Acq
{
    class QCamFlow : DataFlowCam, IQCamFlow
    {
        public QCamFlow(int ringBufferCapacity, TypeModel customSerializer)
            : base(new QCamHAL(), ringBufferCapacity, customSerializer) { }
    }

}
