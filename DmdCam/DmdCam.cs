using System;
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
        readonly Window outputWindow;
        readonly Screen outputScreen;

        public DmdCam(int dim_x, int dim_y, Screen outputScreen)
            : base(dim_x, dim_y)
        {
            if (outputScreen == null)
                throw new ArgumentNullException();
            bool ok = (outputScreen.Bounds.Width == dim_x)
                && (outputScreen.Bounds.Height == dim_y);
            if (!ok)
                throw new ArgumentException("Dimension mismatch");
            this.viewModel = new DisplayDeviceViewModel(dim_x, dim_y);
            previewDisplayView = new PreviewDisplayView();
            previewDisplayView.DataContext = this.viewModel;

            this.outputScreen = outputScreen;
            outputWindow = new Window();
            PreventDisposeOnUserClose(outputWindow);
            ShowOutputScreen();
        }

        void PreventDisposeOnUserClose(Window w)
        {
            if (w != null)
                w.Closing += (object sender, System.ComponentModel.CancelEventArgs e) =>
                {
                    if (!this.IsDisposed)
                    {
                        w.Visibility = Visibility.Hidden;
                        e.Cancel = true;
                    }
                };
        }

        void ShowOutputScreen()
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

        public void SetImage(double[,] whiteLevels)
        {
            viewModel.SetImage(whiteLevels);
        }

        protected override void RunOnceDisposer()
        {
            if (outputWindow != null)
                outputWindow.Close();
        }
    }

}
