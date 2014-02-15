using Lab;
using Lab.Acq;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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
        readonly Process externalDataServer;

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
            if (!DataServer.IsRunning(pipeName))
                this.externalDataServer = DataServer.LaunchExternal(pipeName, waitUntilReady: true);

            cameraReader = new CameraReader<ICameraFlow, RemoteClientCameraFlow>(pipeName, new AcqProtoSerializer());

            // ensure we stop the server, if we're the ones who started it
            cameraReader.TerminateServerOnStop = (this.externalDataServer != null);

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

        public void SetVideoSettingsDynamic(VideoSettingsDynamicStruct settings)
        {
            cameraReader.Flow.SettingsDynamic = settings.AsRefType();
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
            cameraReader.TryDispose();
            externalDataServer.TryDispose();
        }


        public static void test()
        {
            var c = new CcdCam("Lab.Acq.Fake");
            var size = c.Size;
            VideoSettingsStaticStruct s;
            s.Binning = BinningMode.Binning1x1;
            s.RoiX = 16;
            s.RoiY = 32;
            s.RoiWidth = 256;
            s.RoiHeight = 128;
            s.Trigger = 0;
            c.SetVideoSettingsStatic(s);

            c.Start();
            VideoFrame f;
            bool ok = c.TryGetFrame(out f);
            Console.WriteLine(ok);
            if (ok)
                Console.WriteLine(f.FrameNumber);
            c.Stop();
            c.Dispose();
        }
    }

}
