using System;
using System.Windows;
using System.Windows.Forms;

namespace Lab.Acq
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
        Thorlabs.PM100D.PM100D powerMeter;

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

        const string defaultPowerMeterDevice = "USB0::0x1313::0x8072::P2002350::INSTR";

        static Thorlabs.PM100D.PM100D SetUpPowerMeter(string deviceName, double wavelength)
        {
            var pm = new Thorlabs.PM100D.PM100D(deviceName, false, true);
            if (pm == null)
                throw new Exception("Error while initializing PM100D power meter device: constructor returned null");
            int err = pm.setWavelength(wavelength);
            if (err != 0)
                throw new Exception("Error while setting wavelength: code " + err);
            return pm;
        }

        public void ConfigurePowerMeter(string deviceName, double wavelength)
        {
            powerMeter.TryDispose();
            powerMeter = null;
            powerMeter = SetUpPowerMeter(deviceName, wavelength);
        }

        public double MeasurePower()
        {
            if (powerMeter == null)
                throw new InvalidOperationException("First configure the power meter!"); 

            double power;
            int err = powerMeter.measPower(out power);
            if (err == 0)
                return power;
            else
                throw new Exception("Error while measuring power: code " + err);
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
