using Lab;
using Lab.Acq;
using System;
using System.Windows;
using System.Windows.Forms;

namespace eas_lab.acq.CcdCam
{
    /// <summary>
    /// Defines a single instance of a CCD camera
    /// </summary>
    public class CcdCam : Disposable
    {
        readonly CameraReader<ICameraFlow, RemoteClientCameraFlow> cameraReader;

        public CcdCam(string pipeName)
        {
            cameraReader = new CameraReader<ICameraFlow, RemoteClientCameraFlow>(pipeName, new AcqProtoSerializer());
        }

        public RectSize Size
        {
            get
            {
                var ca = cameraReader.Flow.CameraAttributes;
                return new RectSize(ca.FullWidth, ca.FullHeight);
            }
        }

        protected override void RunOnceDisposer()
        {
            cameraReader.Dispose();
        }
    }

}
