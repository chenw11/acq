using ProtoBuf.Meta;
using System;

namespace Lab.Acq
{
    public class DataFlow2<T, THal> : DataFlow<T>, IStartStop
        where THal : ISupportRingBufferOutput<T>, IStartStop
        where T : class, new()
    {
        protected readonly THal hal;

        private DataFlow2(TypeModel customSerializer, THal hal, RingBuffer<T> rb)
            : base(rb, customSerializer)
        {
            if (hal == null)
                throw new ArgumentNullException();
            this.hal = hal;
            hal.RingBufferForOutput = rb;
        }

        static T Builder() { return new T(); }

        public DataFlow2(THal hal, int ringBufferCapacity, TypeModel customSerializer)
            : this(customSerializer, hal,
            new RingBuffer<T>(ringBufferCapacity, Builder) { KeepFresh = true })
        {

        }

        protected override void RunOnceDisposer()
        {
            base.RunOnceDisposer();
            hal.TryDispose();
        }

        public void Start() { hal.Start(); }

        public void Stop() { hal.Stop(); }

        public bool IsRunning { get { return hal.IsRunning; } }
    }

}
