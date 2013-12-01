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

        Screen validateScreen(int screenId)
        {
            if (screenId == 0)
                throw new ArgumentException("Screen 0 is reserved for Igor");
            if (screenId >= Screen.AllScreens.Length)
                throw new ArgumentException("Screen ID too high-- not that many displays are connected");
            if (screenId < 0)
                throw new ArgumentException("Screen ID must be a positive #");
            return Screen.AllScreens[screenId];
        }

        public void DmdCam_GetSize(int screenId, out RectSize size)
        {
            Screen s = validateScreen(screenId);
            size = new RectSize(s.Bounds.Width, s.Bounds.Height);
        }

        /// <summary>
        /// Setup a DmdCamera device
        /// </summary>
        /// <param name="screenId">Screen id.  Primary monitor=0, next monitor=1, etc</param>
        /// <param name="expectedSize">Dimensions of screen we expect.  
        /// Set to (0,0) to ignore; otherwise a dimension mismatch will cause an error</param>
        public void DmdCam_Create(int screenId)
        {
            Screen s = validateScreen(screenId);
            bool added = cams.TryAdd(screenId, new DmdCam(s.Bounds.Width, s.Bounds.Height, s));
            if (!added)
                throw new InvalidOperationException("Another DmdCam was already created for this screen!");
        }

        public void DmdCam_Preview(int screenId, bool visibility)
        {
            cams[screenId].SetPreviewVisibility(visibility, modal: false);
        }

        /// <summary>
        /// Set levels for image.  Stored as 2D array.
        /// 0 = black (mirror "off"), 1 = white (mirror "on")
        /// </summary>
        /// <param name="whiteLevels">2D array of pixel values</param>
        public void DmdCam_SetImage(int screenId, double[,] whiteLevels)
        {
            throw new NotImplementedException();
            Screen s = validateScreen(screenId);

            int dimX = whiteLevels.GetLength(0);
            int dimY = whiteLevels.GetLength(1);
            bool ok = (dimX == s.Bounds.Width) && (dimY == s.Bounds.Height);
            if (!ok)
                throw new ArgumentException(string.Format(
                    " Screen {0} has dimensions {1}x{2}, but you passed in a wave with dimensions ={3}x{4}",
                    screenId, s.Bounds.Width, s.Bounds.Height, dimX, dimY));
        }

        protected override void RunOnceDisposer() 
        {
            foreach (var kvp in cams)
                kvp.Value.Dispose();
            cams.Clear();
        }
    }



   
}
