using System;
using System.Windows.Media.Imaging;
//using eas_lab.gui;
using Lab.gui;

namespace Lab.Acq
{
    public class DisplayDeviceViewModel : Rectangular, IDisplayDevice
    {
        readonly BitmapLookup8 bm;

        public DisplayDeviceViewModel(int dim_x, int dim_y)
            : base(dim_x, dim_y)
        {
            bm = new BitmapLookup8(dim_x, dim_y, setBasicGrayscale: true);
        }

        
        public BitmapSource ImageSource
        {
            get { return bm.Bitmap; }
        }

        public void SetImage(double[,] whiteLevels)
        {
            int dimY = whiteLevels.GetLength(0);
            int dimX = whiteLevels.GetLength(1);
            bool ok = (dimX == this.Dim_X)
                && (dimY == this.Dim_Y);
            if (!ok)
                throw new ArgumentException(string.Format(
                    "Expecting dimensions {0}x{1}, but you passed in data with dimensions {2}x{3}",
                    this.Dim_X, this.Dim_Y, dimX, dimY));

            byte[] bmGray = bm.DataBuffer;
            if (bmGray.Length != dimX * dimY)
                throw new Exception("Unexpected dimension mismatch inside DisplayDeviceViewModel.SetImage");

            for(int y=0 ; y<dimY; y++)
                for (int x = 0; x < dimX; x++)
                {
                    double d = whiteLevels[y, x];
                    byte v = (byte)(255 * d);
                    bmGray[y * dimX + x] = v;
                }
            bm.Remap();
            bm.Invalidate();
        }
    }
}
