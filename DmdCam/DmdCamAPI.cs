using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace eas_lab.acq.DmdCam
{
    public interface IDisplayDevice : IDisposable
    {
        int Dim_X { get; }
        int Dim_Y { get; }

        BitmapSource ImageSource { get; set; }
    }

    public static class Constants
    {
        public const int DLP_3000_DIM_X = 608;
        public const int DLP_3000_DIM_Y = 684;
    }

    /// <summary>
    /// Defines a flat API for creating and using DmdCam objects
    /// </summary>
    public static class DmdCamAPI
    {
        static int _camIndex = -1;
        static readonly ConcurrentDictionary<int, DmdCam> cams = new ConcurrentDictionary<int, DmdCam>();

        public static int CreateDmdCam(int dim_x, int dim_y, int outputScreenId)
        {
            int id = Interlocked.Increment(ref _camIndex);
            Screen outputScreen = null;
            if ((outputScreenId > 0) && (outputScreenId < Screen.AllScreens.Length))
                outputScreen = Screen.AllScreens[outputScreenId];
            cams[id] = new DmdCam(dim_x, dim_y, outputScreen);
            return id;
        }

        public static int CreateDmdCam_DLP3000()
        {
            int outputScreenId = -1;
            if (Screen.AllScreens.Length > 1)
                outputScreenId = 1;
            return CreateDmdCam(Constants.DLP_3000_DIM_X, Constants.DLP_3000_DIM_Y, 
                outputScreenId);
        }

        public static DmdCam GetDmdCam(int i) {  return cams[i]; }

        public static void Cleanup()
        {
            foreach (var kvp in cams)
                kvp.Value.Dispose();
            cams.Clear();
        }
    }
}
