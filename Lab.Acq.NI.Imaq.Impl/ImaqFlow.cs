using ProtoBuf.Meta;

namespace Lab.Acq
{
    class ImaqFlow : DataFlowCam, IImaqFlow
    {
        public ImaqFlow(int ringBufferCapacity, TypeModel customSerializer)
            : base(new ImaqHAL(), ringBufferCapacity, customSerializer) { }
    }
}
