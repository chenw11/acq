using Lab.Acq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Lab.Acq
{
    public struct VideoSettingsStaticStruct
    {
        public BinningMode Binning;
        public int RoiX;
        public int RoiY;
        public int RoiWidth;
        public int RoiHeight;
        public TriggeringMode Trigger;

        public VideoSettingsStatic AsRefType()
        {
            return new VideoSettingsStatic()
            {
                Binning = this.Binning,
                RoiX = this.RoiX,
                RoiY = this.RoiY,
                RoiWidth = this.RoiWidth,
                RoiHeight = this.RoiHeight,
                Trigger = this.Trigger
            };
        }
    }

    public struct VideoSettingsDynamicStruct
    {
        public float AnalogGain;
        public int AnalogOffset;

        public VideoSettingsDynamic AsRefType()
        {
            return new VideoSettingsDynamic()
            {
                AnalogGain_dB = this.AnalogGain,
                AnalogOffset = this.AnalogOffset
            };
        }
    }

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
                pipeName = "Lab.Acq.NI.Imaq";
            else
                throw new System.ArgumentException("invalid device id: select 0, 1 or 2 for fake, qcam, or orcaER");

            if (cams.ContainsKey(deviceId))
                throw new ArgumentException("Device already created!");
            
            CcdCam c = new CcdCam(pipeName);
            cams.TryAdd(deviceId, c);
        }


        CcdCam getCam(int deviceId)
        {
            try { return cams[deviceId]; }
            catch (KeyNotFoundException)
            {
                throw new Exception("Device not initialized.  Call CcdCam_Create first");
            }
        }

        public void CcdCam_GetSize(int deviceId, out RectSize size)
        {
            size = getCam(deviceId).Size;
        }

        public void CcdCam_SetVideoSettingsStatic(int deviceId, VideoSettingsStaticStruct settings)
        {
            getCam(deviceId).SetVideoSettingsStatic(settings);
        }

        public void CcdCam_SetVideoSettingsDynamic(int deviceId, VideoSettingsDynamicStruct settings)
        {
            if (deviceId != 2)
                throw new ArgumentException("This is only supported on the Orca ER camera, for now");
            getCam(deviceId).SetVideoSettingsDynamic(settings);
        }

        public void CcdCam_Start(int deviceId)
        {
            getCam(deviceId).Start();
        }

        public void CcdCam_Stop(int deviceId)
        {
            getCam(deviceId).Stop();
        }

        public bool CcdCam_TryGetFrame(int deviceId, out VideoFrame frame)
        {
            return getCam(deviceId).TryGetFrame(out frame);
        }

        protected override void RunOnceDisposer()
        {
            foreach (var kvp in cams)
                kvp.Value.Dispose();
            cams.Clear();
        }
    }
}
