using System.Linq;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Lab.Acq
{
    class FakeHAL : Disposable, IFake, ICameraHAL
    {
        public FakeHAL()
        {
            BitsPerPixel = 14;
            TEST_InterFrameInterval = 100;
            CCDWidth = 512;
            CCDHeight = 512;
            ImageWidth = CCDWidth;
            ImageHeight = CCDHeight;
            SupportedBinning = new BinningMode[] { BinningMode.Binning1x1 };
            BinningMode = BinningMode.Binning1x1;
        }

        int CCDWidth { get; set; }
        int CCDHeight { get; set; }
        int BitsPerPixel { get; set; }
        int ImageWidth { get; set; }
        int ImageHeight { get; set; }
        BinningMode BinningMode { get; set; }
        BinningMode[] SupportedBinning { get; set; }

        public void TEST_Define_Camera(int ccdWidth, int ccdHeight, int bitsPerPixel, BinningMode[] supportedBinning)
        {
            AssertNotDisposed();
            if ((ccdWidth < 16) || (ccdHeight < 16) || (bitsPerPixel < 1) || (bitsPerPixel > 16))
                throw new ArgumentOutOfRangeException();
            if ((supportedBinning == null) || (!supportedBinning.Contains(Acq.BinningMode.Binning1x1)))
                throw new ArgumentException("supportedBinning");
            this.CCDWidth = ccdWidth;
            this.CCDHeight = ccdHeight;
            this.ImageWidth = CCDWidth;
            this.ImageHeight = CCDHeight;

            this.BitsPerPixel = bitsPerPixel;
            this.SupportedBinning = supportedBinning;
            Trace.TraceInformation("Redefined fake camera to {0} x {1} @ {2}", CCDWidth, CCDHeight, BitsPerPixel);
        }
        

        void FillBuffer_Fake(byte[] outBuffer, double t)
        {
            //   Sin[(x - xm/2)/xm*2 Pi Cos[t]]/8 + 
            //   Sin[(y - ym/2)/ym*2 Pi Sin[t]]/8 + 0.25 + 
            //   0.5*Exp[-(x - xm/2)^2/100 - (y - ym/2)^2/100]
            int xm = ImageWidth;
            int ym = ImageHeight;
            int bits = BitsPerPixel;
            if (bits > 16)
                throw new NotSupportedException();
            double dynamicRange = (1 << bits) - 1;
            if (SingleThreaded)
            {
                for (int x = 0; x < xm; x++)
                    FillRow(outBuffer, t, xm, ym, bits, dynamicRange, x);
            }
            else
                Parallel.For(0, xm, x => FillRow(outBuffer, t, xm, ym, bits, dynamicRange, x));
        }

        public bool SingleThreaded { get; set; }

        private static void FillRow(byte[] outBuffer, double t, int xm, int ym, int bits, double dynamicRange, int x)
        {
            double dx = x - xm / 2;
            double vx = Math.Sin(dx / xm * 2 * Math.PI * Math.Cos(t)) / 8;
            for (int y = 0; y < ym; y++)
            {
                double dy = y - ym / 2;
                double vy = Math.Sin(dy / ym * 2 * Math.PI * Math.Sin(t)) / 8;

                // normalized value
                double v = vx + vy + 0.25 + 0.5 * Math.Exp(-dx * dx / 100 - dy * dy / 100);

                // scale with saturation
                v *= dynamicRange;
                if (!(v <= dynamicRange))
                    v = dynamicRange;

                int vInt = (int)v;
                if (vInt > dynamicRange)
                    throw new Exception("Gotcha");
                byte[] bytes = BitConverter.GetBytes(vInt);
                int flatIndex = y * xm + x;
                if (bits <= 8)
                    outBuffer[flatIndex] = bytes[0]; // Need to verify this is the right byte order
                else if (bits <= 16)
                {
                    outBuffer[flatIndex * 2] = bytes[0];
                    outBuffer[flatIndex * 2 + 1] = bytes[1];
                }
            }
        }

        int counter = -1;
        const int cycleLength = 256;

        void CopyInNewData(int myId, VideoFrame outFrame)
        {
            int b = (int)BinningMode;
            // prep
            int width = ImageWidth;
            outFrame.Width = (uint)width;

            int height = ImageHeight;
            outFrame.Height = (uint)height;

            int bits = BitsPerPixel;
            outFrame.BitsPerPixel = (uint)bits;

            outFrame.FrameNumber = (uint)myId;

            uint bytesPerPixel = checked((uint)Utilities.Divide_RoundUp(bits, 8 /* bits per byte */));
            uint nBytes = checked((uint)(width * height * bytesPerPixel));

            outFrame.DataSizeBytes = nBytes;
            if ((outFrame.Data == null) || (outFrame.Data.Length != nBytes))
                outFrame.Data = new byte[nBytes];

            double t = Math.Sin((myId % cycleLength) * Math.PI * 2 / (double)cycleLength);
            FillBuffer_Fake(outFrame.Data, t);
        }

        void QueueFrame()
        {
            int myId = Interlocked.Increment(ref counter);

            RingBufferForOutput.TryCopyIn<int, VideoFrame>(CopyInNewData, myId);
        }

        int interFrameInterval = 30;
        public int TEST_InterFrameInterval
        {
            get { return interFrameInterval; }
            set
            {
                if (value >= 0) 
                    interFrameInterval = value;
                else
                    throw new ArgumentOutOfRangeException();
            }
        }

        Thread loopThread;
        readonly object lock_thread = new object();
        ManualResetEventSlim quitLoop = new ManualResetEventSlim(false);

        void dataLoop()
        {
            Stopwatch delay = new Stopwatch();
            int toWait = 0;
            while (!quitLoop.Wait(toWait))
            {
                delay.Restart();
                QueueFrame();
                toWait = (int)Math.Max(0, TEST_InterFrameInterval - delay.ElapsedMilliseconds);
            }
        }

        public void Start()
        {
            AssertNotDisposed();
            lock (lock_thread)
            {
                if (IsRunning)
                    throw new InvalidOperationException("Already started!");
                loopThread = new Thread(new ThreadStart(dataLoop));
                quitLoop.Reset();
                loopThread.Start();
            }
        }

        public bool IsRunning { get { return (loopThread != null) && (loopThread.IsAlive); } }

        public void Stop()
        {
            lock (lock_thread)
            {
                quitLoop.Set();
                Thread t = loopThread;
                if (t != null)
                    t.Join();
            }
        }

        protected override void RunOnceDisposer()
        {
            quitLoop.Set();

        }


        public IRingBufferWrite<VideoFrame> RingBufferForOutput { get; set; }


        public CameraAttributes CameraAttributes
        {
            get
            {
                AssertNotDisposed();
                return new CameraAttributes
                {
                    BitDepth = BitsPerPixel,
                    FullHeight = CCDHeight,
                    FullWidth = CCDWidth,
                    SupportedBinning = SupportedBinning,
                    Model = "Fake source",
                    SerialNumber = "000000"
                };
            }
        }

        public VideoSettingsDynamic SettingsDynamic { get; set; }

        public VideoSettingsStatic SettingsStatic
        {
            get
            {
                AssertNotDisposed();
                return new VideoSettingsStatic
                {
                    Binning = BinningMode,
                    //Trigger = TriggerMode.Freerun,
                    RoiX = 0,
                    RoiY = 0,
                    RoiWidth = ImageWidth,
                    RoiHeight = ImageHeight
                };
            }
            set
            {
                AssertNotDisposed();
                bool ok = SupportedBinning.Contains(value.Binning);
                int b = (int)(value.Binning);
                if (b < 1)
                    throw new ArgumentException("binning");
                //ok &= value.Trigger == TriggerMode.Freerun;
                ok &= value.RoiX >= 0;
                ok &= value.RoiY >= 0;
                ok &= value.RoiWidth > 4 && value.RoiWidth + value.RoiX <= CCDWidth/b;
                ok &= value.RoiHeight > 4 && value.RoiHeight + value.RoiY <= CCDHeight/b;
                if (!ok)
                    throw new ArgumentOutOfRangeException();

                this.ImageWidth = value.RoiWidth;
                this.ImageHeight = value.RoiHeight;
            }
        }
    }
}
