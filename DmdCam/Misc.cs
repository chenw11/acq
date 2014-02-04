using System;
using System.Windows.Media.Imaging;

namespace Lab.Acq
{
    public interface IDisplayDevice : IDisposable
    {
        int Dim_X { get; }
        int Dim_Y { get; }

        BitmapSource ImageSource { get; }
    }

    public static class Constants
    {
        public const int DLP_3000_DIM_X = 608;
        public const int DLP_3000_DIM_Y = 684;
    }

}
