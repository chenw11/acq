using System;

namespace eas_lab.acq.DmdCamManualTests
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            int camId = DmdCam.DmdCamAPI.CreateDmdCam_DLP3000();
            var cam = DmdCam.DmdCamAPI.GetDmdCam(camId);

            cam.ShowOutputScreen();
            cam.ShowPreview(true);

            
        }
    }
}
