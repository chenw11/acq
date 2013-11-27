using System.Windows.Media.Imaging;

namespace eas_lab.acq.DmdCam
{
    public class DisplayDeviceViewModel : Rectangular, IDisplayDevice
    {
        public DisplayDeviceViewModel(int dim_x, int dim_y)
            : base(dim_x, dim_y) { }

        BitmapSource imageSource;

        public BitmapSource ImageSource
        {
            get { return imageSource; }
            set { base.SetAndNotify_RefEquality(ref imageSource, value, "ImageSource"); }
        }
    }
}
