using System;
using System.Collections.Concurrent;
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


    public class DmdCamXop : Disposable
    {
        readonly ConcurrentDictionary<int, DmdCam> cams = new ConcurrentDictionary<int, DmdCam>();

        public DmdCamXop() { }

        /// <summary>
        /// Setup a DmdCamera device
        /// </summary>
        /// <param name="screenId">Screen id.  Primary monitor=0, next monitor=1, etc</param>
        /// <param name="expectedSize">Dimensions of screen we expect.  
        /// Set to (0,0) to ignore; otherwise a dimension mismatch will cause an error</param>
        public void DmdCam_Create(int screenId, RectSize expectedSize)
        {
            if (screenId == 0)
                throw new ArgumentException("Screen 0 is reserved for Igor");
            if (screenId >= Screen.AllScreens.Length)
                throw new ArgumentException("Screen ID too high-- not that many displays are connected");
            if (screenId < 0)
                throw new ArgumentException("Screen ID must be a positive #");

            Screen s = Screen.AllScreens[screenId];
            if ((expectedSize.DimX != 0) || (expectedSize.DimY != 0))
            {
                bool ok = (expectedSize.DimX == s.Bounds.Width) && (expectedSize.DimY == s.Bounds.Height);
                if (!ok)
                    throw new ArgumentException("Specify the exact dimensions of the screen, or set expectedSize to (0,0) to use defaults. " +
                        string.Format(" Screen {0} has dimensions {1}x{2}, but you gave expectedSize={3}x{4}", screenId,
                        s.Bounds.Width, s.Bounds.Height, expectedSize.DimX, expectedSize.DimY));
            }

            bool added = cams.TryAdd(screenId, new DmdCam(s.Bounds.Width, s.Bounds.Height, s));
            if (!added)
                throw new InvalidOperationException("Another DmdCam was already created for this screen!");
        }

        public void DmdCam_Preview(int screenId, bool visibility)
        {
            cams[screenId].SetPreviewVisibility(visibility, modal: false);
        }

        /// <summary>
        /// Set levels for image.  Stored as flattened 2D array.
        /// 0 = black (mirror "off"), 1 = white (mirror "on")
        /// </summary>
        /// <param name="whiteLevels">Flattened 2D array of pixel values</param>
        public void DmdCam_SetImage(int screenId, double[] whiteLevels)
        {
            throw new NotImplementedException();
        }

        protected override void RunOnceDisposer() 
        {
            foreach (var kvp in cams)
                kvp.Value.Dispose();
            cams.Clear();
        }
    }



   
}
