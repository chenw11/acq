using Lab;
using Lab.Acq;
using System;
using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Forms;

namespace Lab.Acq
{
    /// <summary>
    /// Defines a single instance of a CCD camera
    /// </summary>
    public class CcdCam : Disposable
    {
        readonly CameraReader<ICameraFlow, RemoteClientCameraFlow> cameraReader;
        readonly ConcurrentQueue<VideoFrame> buffer = new ConcurrentQueue<VideoFrame>();

        int bufferCapacity = 8;

        public int BufferCapacity
        { 
            get { return bufferCapacity; }
            set
            {
                if ((value > 0) && (value < 128))
                    bufferCapacity = value;
                else
                    throw new ArgumentOutOfRangeException();
            }
        }

        public CcdCam(string pipeName)
        {
            cameraReader = new CameraReader<ICameraFlow, RemoteClientCameraFlow>(pipeName, new AcqProtoSerializer());
            cameraReader.DataSource.NewData += DataSource_NewData;
        }

        

        void DataSource_NewData(object sender, VideoFrame e)
        {
            buffer.Enqueue(e);
            if (buffer.Count > bufferCapacity)
                buffer.TryDequeue(out e); // ok b/c not a deep copy
        }

        public RectSize Size
        {
            get
            {
                var ca = cameraReader.Flow.CameraAttributes;
                return new RectSize(ca.FullWidth, ca.FullHeight);
            }
        }

        public void SetVideoSettingsStatic(VideoSettingsStaticStruct settings)
        {
            cameraReader.Flow.SettingsStatic = settings.AsRefType();
        }

        public void Start()
        {
            cameraReader.DataSource.Start();
        }

        public void Stop()
        {
            cameraReader.DataSource.Stop();
        }

        public bool TryGetFrame(out VideoFrame frame)
        {
            return buffer.TryDequeue(out frame);
        }

        protected override void RunOnceDisposer()
        {
            cameraReader.Dispose();
        }
    }

}
