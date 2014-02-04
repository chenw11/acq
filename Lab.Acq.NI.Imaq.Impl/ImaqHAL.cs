using System;
using System.Diagnostics;
using System.Threading;
using NationalInstruments.Vision.Acquisition.Imaq;

namespace Lab.Acq
{
    class AttributeWrapper<TKey, TValue>
    {
        readonly Func<TKey, TValue> getter;
        readonly Action<TKey, TValue> setter;
        public AttributeWrapper(Func<TKey, TValue> getter, Action<TKey, TValue> setter)
        {
            this.getter = getter;
            this.setter = setter;
        }

        public TValue this[TKey key]
        {
            get { return getter(key); }
            set { setter(key, value); }
        }
    }

    class ImaqHAL : Disposable, IImaq, ICameraHAL
    {
        readonly ImaqSession session;

        bool isStreaming = false;
        readonly object driverStreamingLock = new object();

        public static readonly int Default_NumFrameBuffers = 8;
        int nDriverFrameBuffers = Default_NumFrameBuffers;
        public int NumDriverFrameBuffers
        {
            get { return nDriverFrameBuffers; }
            set
            {
                if ((value > 1) && (value < 32))
                    nDriverFrameBuffers = value;
                else
                    throw new ArgumentOutOfRangeException();
            }
        }

        readonly AutoResetEvent startedStreaming = new AutoResetEvent(false);
        readonly AutoResetEvent reqStopStreaming = new AutoResetEvent(false);
        readonly ThreadStart streamingProcDelegate;
        Thread streamingThread;

        public bool IsRunning { get { return isStreaming; } }

        static string GetDefaultSessionName()
        {
            var sessions = ImaqSession.EnumerateInterfaces();
            if (sessions.Length == 0)
                throw new ImaqException("No IMAQ cameras detected.  Ensure cables are connected and power is on.");
            return sessions[0];
        }
        
        string getAttribute_string(string cameraAttributeName)
        {
            string s;
            session.Attributes.GetCameraAttribute(cameraAttributeName, out s);
            return s;
        }

        double getAttribute_num(string cameraAttributeName)
        {
            double d;
            session.Attributes.GetCameraAttribute(cameraAttributeName, out d);
            return d;
        }


        readonly AttributeWrapper<string, string> attrib_cam_string;
        readonly AttributeWrapper<string, double> attrib_cam_num;
        readonly AttributeWrapper<ImaqStandardAttribute, int> attrib_imaq_int;

        public ImaqHAL() : this(GetDefaultSessionName()) { }

        public ImaqHAL(string sessionName)
        {
            Trace.TraceInformation("Launching IMAQ camera driver...");
            this.session = new ImaqSession(sessionName);
            cameraAttributes.BitDepth = 12;
            cameraAttributes.FullHeight = 1024;
            cameraAttributes.FullWidth = 1344;
            cameraAttributes.Model = "Hamamatsu Orca ER";
            cameraAttributes.SerialNumber = session.Attributes[ImaqStandardAttribute.SerialNumber].GetValue().ToString();
            cameraAttributes.SupportedBinning = new BinningMode[] { 
                BinningMode.Binning1x1, BinningMode.Binning2x2, BinningMode.Binning4x4, BinningMode.Binning8x8 };
            binningModeStrings = Array.ConvertAll<BinningMode, string>(cameraAttributes.SupportedBinning,
                b => string.Format("{0}x{0} Binning", (int)b));
            attrib_cam_string = new AttributeWrapper<string, string>(getAttribute_string,
                session.Attributes.SetCameraAttribute);
            attrib_cam_num = new AttributeWrapper<string, double>(getAttribute_num,
                session.Attributes.SetCameraAttribute);
            attrib_imaq_int = new AttributeWrapper<ImaqStandardAttribute,int>(
                k => (int)session.Attributes[k].GetValue(), 
                (k,v) => session.Attributes[k].SetValue(v));
            SettingsDynamic = new VideoSettingsDynamic() { AnalogGain_dB = 0, AnalogOffset = 0 };
            streamingProcDelegate = new ThreadStart(AcqProc);
            frameCopierDelegate = new Action<NF, VideoFrame>(frameCopier);
            setContrastValuesDelegate = setContrastValues;
        }


        protected override void RunOnceDisposer()
        {
            Stop();
            session.TryDispose();
        }

        readonly CameraAttributes cameraAttributes = new CameraAttributes();
        public CameraAttributes CameraAttributes { get { return cameraAttributes.Duplicate(); } }
        
        public IRingBufferWrite<VideoFrame> RingBufferForOutput { get; set; }

        const string attribName_binning = "Binning Mode";
        const string attribName_scanMode = "Scan Mode";
        const string attribName_gain = "Contrast Enhance Gain";
        const string attribName_offset = "Contrast Enhance Offset";
        const string attribValue_normal = "Normal";
        const string attribValue_subarray = "Sub Array";
        const string attribName_subarrayWidth = "Sub Array Horizontal Width";
        const string attribName_subarrayHeight = "Sub Array Vertical Width";
        const string attribName_subarrayX = "Sub Array Horizontal Offset";
        const string attribName_subarrayY = "Sub Array Vertical Offset";

        readonly string[] binningModeStrings;

        public BinningMode Binning
        {
            get
            {
                string m = attrib_cam_string[attribName_binning];
                int i = Array.IndexOf(binningModeStrings, m);
                if (i >= 0)
                    return cameraAttributes.SupportedBinning[i];
                else
                    throw new ImaqException("Unexpected binning mode: " + m);
            }
            set
            {
                AssertNotDisposed();
                if (IsRunning)
                    throw new ImaqException("Can't change binning while acquiring.");
                int i = Array.IndexOf(cameraAttributes.SupportedBinning, value);
                if (i >= 0)
                {
                    string m = binningModeStrings[i];
                    attrib_cam_string[attribName_binning] = m;
                }
                else
                    throw new ImaqException("Invalid binning mode: " + value);
            }
        }

        readonly RoiConstraints acqWindowConstraints = new RoiConstraints(
            1344, 1024, 16, 16, 16, 16);

        RectSize PostBinningRoiSize
        {
            get
            {
                return new RectSize(
                    attrib_imaq_int[ImaqStandardAttribute.AcquisitionWindowWidth],
                    attrib_imaq_int[ImaqStandardAttribute.AcquisitionWindowHeight]);
            }
        }

        NaturalRect PostBinningRoi
        {
            get
            {
                return new NaturalRect(
                    attrib_imaq_int[ImaqStandardAttribute.AcquisitionWindowLeft],
                    attrib_imaq_int[ImaqStandardAttribute.AcquisitionWindowTop],
                    PostBinningRoiSize);
            }
            set
            {
                value.Validate();
                acqWindowConstraints.Validate(value);
                var b = (int)Binning;

                // first set sub-array mode for hardware
                if ((value.Width * b == cameraAttributes.FullWidth) && (value.Height * b == cameraAttributes.FullHeight)
                    && (value.Left == 0) && (value.Top == 0))
                {
                    attrib_cam_string[attribName_scanMode] = attribValue_normal;
                }
                else
                {
                    attrib_cam_string[attribName_scanMode] = attribValue_subarray;
                    attrib_cam_num[attribName_subarrayX] = value.Left * b;
                    attrib_cam_num[attribName_subarrayY] = value.Top * b;
                    attrib_cam_num[attribName_subarrayWidth] = value.Width * b;
                    attrib_cam_num[attribName_subarrayHeight] = value.Height * b;
                }

                attrib_imaq_int[ImaqStandardAttribute.AcquisitionWindowLeft] = 0;
                attrib_imaq_int[ImaqStandardAttribute.AcquisitionWindowTop] = 0;
                attrib_imaq_int[ImaqStandardAttribute.AcquisitionWindowWidth] = value.Width;
                attrib_imaq_int[ImaqStandardAttribute.AcquisitionWindowHeight] = value.Height;
                session.RegionOfInterest = new NationalInstruments.Vision.RectangleContour(0, 0, value.Width, value.Height);
            }
        }


        public VideoSettingsStatic SettingsStatic
        {
            get
            {
                var vss = new VideoSettingsStatic();
                vss.Binning = Binning;
                vss.Roi = PostBinningRoi;
                vss.Trigger = TriggeringMode.Freerun; // change later
                return vss;
            }
            set
            {
                AssertNotDisposed();
                if (IsRunning)
                    throw new ImaqException("Can't change static settings while running");

                Binning = value.Binning;
                PostBinningRoi = value.Roi;
                if (value.Trigger != TriggeringMode.Freerun)
                    throw new NotSupportedException("We don't support triggering yet on this camera");
            }
        }

        public VideoSettingsDynamic SettingsDynamic
        {
            get
            {
                var vsd = new VideoSettingsDynamic();
                vsd.AnalogGain_dB = (float)attrib_cam_num[attribName_gain];
                vsd.AnalogOffset = (int)attrib_cam_num[attribName_offset];
                return vsd;
            }
            set
            {
                int gain = (int)(value.AnalogGain_dB);
                int offset = value.AnalogOffset;
                bool ok = (0 <= gain) && (gain <= 255);
                if (!ok)
                    throw new ArgumentOutOfRangeException("gain");
                ok = (0 <= offset) && (offset <= 255);
                if (!ok)
                    throw new ArgumentOutOfRangeException("offset");
                queuedContrastValues = Tuple.Create(gain, offset);
                if (IsRunning)
                    Interlocked.Exchange(ref pauseAndExecute, setContrastValuesDelegate);
                else
                    setContrastValues();
            }
        }

        Tuple<int, int> queuedContrastValues;
        Action setContrastValuesDelegate;
        void setContrastValues()
        {
            Tuple<int, int> t = Interlocked.Exchange(ref queuedContrastValues, null);
            if (t != null)
            {
                attrib_cam_num[attribName_gain] = t.Item1;
                attrib_cam_num[attribName_offset] = t.Item2;
            }
        }

        ImaqBufferCollection buffers;
        RectSize bufferSize;

        void SetupOutputSignal(int rtsiLine, ImaqTriggerDriveSource status)
        {
            session.Triggers.Clear();

            session.Triggers.AddTrigger("ExternSignal",
                new ImaqSignalDescriptor(ImaqSignalType.External, 0));
            session.Triggers[0].Drive(status);

            session.Triggers.AddTrigger("RTSISignal",
                new ImaqSignalDescriptor(ImaqSignalType.Rtsi, rtsiLine));
            session.Triggers[1].Drive(status);
        }

        void SetupOutputSignal_FrameStart(int rtsiLine)
        {
            SetupOutputSignal(rtsiLine, ImaqTriggerDriveSource.FrameStart);
        }

        Action pauseAndExecute;

        void AcqProc()
        {
            //session.SignalEvents.AddSignal("Frame done",
            //    new ImaqSignalEventDescriptor(ImaqSignalStatus.FrameDone,
            //        ImaqSignalState.High));


            RectSize size = PostBinningRoiSize;
            bool recycleBuffers = (buffers != null)
                && (buffers.Count == nDriverFrameBuffers)
                && (bufferSize == size);
            if (!recycleBuffers)
            {
                Trace.TraceInformation("Allocating {0} buffers...", nDriverFrameBuffers);
                buffers = session.CreateBufferCollection(nDriverFrameBuffers,
                    ImaqBufferCollectionType.PixelValue2D);
                bufferSize = size;
                Trace.TraceInformation("Setting up buffer ring...");
                session.RingSetup(buffers, 0, false);
            }

            Trace.TraceInformation("Starting acquisition...");
            session.Start();


            int frameIndex = -1;
            startedStreaming.Set();
            while (!reqStopStreaming.WaitOne(0))
            {
                uint extractedBufferIndex;

                ImaqBuffer buf = session.Acquisition.Extract(
                    (uint)(frameIndex + 1), out extractedBufferIndex);
                
                if (extractedBufferIndex != frameIndex + 1)
                    Trace.TraceWarning("Skipped from frame {0} to frame {1}",
                        frameIndex, extractedBufferIndex);

                frameIndex = (int)extractedBufferIndex;

                RingBufferForOutput.TryCopyIn<NF, VideoFrame>(frameCopierDelegate,
                    new NF(frameIndex, buf));


                Action a = Interlocked.Exchange(ref pauseAndExecute, null);
                if (a != null)
                {
                    Trace.TraceInformation("Pausing acquisition to execute an interrupt....");
                    session.Stop();
                    Trace.TraceInformation("Executing...");
                    a();
                    Trace.TraceInformation("Restarting acquisition...");
                    session.Start();
                }
            }

            session.Stop();
        }

        struct NF
        {
            public readonly int frameIndex;
            public readonly ImaqBuffer buffer;
            public NF(int frameIndex, ImaqBuffer buf)
            {
                this.frameIndex = frameIndex;
                this.buffer = buf;
            }
        }
        readonly Action<NF, VideoFrame> frameCopierDelegate;
        void frameCopier(NF input, VideoFrame f)
        {
            var uar = input.buffer.ToPixelArray().I16;
            f.BitsPerPixel = (uint)cameraAttributes.BitDepth;

            int nFrameBytes = input.buffer.Size;
            f.DataSizeBytes = (uint)nFrameBytes;
            
            f.FrameNumber = (uint)input.frameIndex;
            f.Height = (uint)uar.GetLength(1);
            f.Width = (uint)uar.GetLength(0);

            if ((f.Data == null) || (f.Data.Length != nFrameBytes))
                f.Data = new byte[nFrameBytes];
            Buffer.BlockCopy(uar, 0, f.Data, 0, nFrameBytes);
        }

        public void Start()
        {
            AssertNotDisposed();
            if (IsRunning)
                throw new ImaqException("Already started.");

            streamingThread = new Thread(streamingProcDelegate);
            Trace.TraceInformation("Starting streaming thread...");
            streamingThread.Start();
            startedStreaming.WaitOne();
            isStreaming = true;
        }

        public void Stop()
        {
            if (!IsRunning)
                throw new ImaqException("Not currently running.");
            var st = streamingThread;
            if (st == null)
                throw new ImaqException("Somehow streaming thread is null");
            Trace.TraceInformation("Requesting streaming thread to quit....");
            reqStopStreaming.Set();
            streamingThread.Join();
            isStreaming = false;
        }



    }
}
