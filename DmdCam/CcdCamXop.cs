using Lab.Acq;
using System;
using System.Collections.Concurrent;

namespace eas_lab.acq.CcdCam
{
    public class CcdCamXop : Disposable
    {
        readonly ConcurrentDictionary<int, CcdCam> cams = new ConcurrentDictionary<int, CcdCam>();

        public CcdCamXop() { }

        /// <summary>
        /// Setup a CcdCamera device
        /// </summary>
        /// <param name="deviceId">DeviceId.  0=fake, 1=QCam, 2=OrcaER</param>  
        public void CcdCam_Create(int deviceId)
        {
            string pipeName = "";
            if (deviceId == 0)
                pipeName = "Lab.Acq.Fake";
            else if (deviceId == 1)
                pipeName = "Lab.Acq.QImaging.QCam";
            else if (deviceId == 2)
                throw new System.NotImplementedException("device 2 = OrcaER is not yet implemented");
            else
                throw new System.ArgumentException("invalid device id: select 0, 1 or 2 for fake, qcam, or orcaER");

            if (cams.ContainsKey(deviceId))
                throw new ArgumentException("Device already created!");
            
            CcdCam c = new CcdCam(pipeName);
            cams.TryAdd(deviceId, c);
        }

        public void CcdCam_GetSize(int deviceId, out RectSize size)
        {
            CcdCam c = cams[deviceId];
            size = c.Size;
        }

        protected override void RunOnceDisposer()
        {
            foreach (var kvp in cams)
                kvp.Value.Dispose();
            cams.Clear();
        }
    }
}
