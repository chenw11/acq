using System.Windows;
using System.Windows.Forms;

namespace eas_lab.acq.DmdCam
{
    /// <summary>
    /// Defines a single instance of a DMD-camera
    /// Using a single DMD/DLP chip and a single optical detector
    /// </summary>
    public class DmdCam : Rectangular
    {
        readonly DisplayDeviceViewModel viewModel;
        readonly PreviewDisplayView previewDisplayView;
        readonly Window previewWindow;
        readonly Window outputWindow;
        readonly Screen outputScreen;

        public DmdCam(int dim_x, int dim_y, Screen outputScreen)
            : base(dim_x, dim_y)
        {
            this.viewModel = new DisplayDeviceViewModel(dim_x, dim_y);
            previewDisplayView = new PreviewDisplayView();
            previewDisplayView.DataContext = this.viewModel;
            previewWindow = new Window();
            previewWindow.Content = previewDisplayView;

            this.outputScreen = outputScreen;
            if (outputScreen != null)
                outputWindow = new Window();
        }

        public void ShowOutputScreen()
        {
            if (outputScreen != null)
            {
                outputWindow.Content = previewDisplayView;
                outputWindow.WindowState = WindowState.Normal;
                outputWindow.WindowStyle = WindowStyle.None;
                outputWindow.Show();

                outputWindow.Left = outputScreen.Bounds.Left;
                outputWindow.Top = outputScreen.Bounds.Top;
                outputWindow.Width = outputScreen.Bounds.Width;
                outputWindow.Height = outputScreen.Bounds.Height;

                outputWindow.Topmost = true;
                outputWindow.WindowState = WindowState.Maximized;
            }
        }

        public void SetPreviewVisibility(bool visibility, bool modal)
        {
            if (visibility)
            {
                if (modal)
                    previewWindow.ShowDialog();
                else
                    previewWindow.Show();
            }
            else
                previewWindow.Hide();
        }


        protected override void RunOnceDisposer()
        {
            if (previewWindow != null)
                previewWindow.Close();
            if (outputWindow != null)
                outputWindow.Close();
        }
    }

}
