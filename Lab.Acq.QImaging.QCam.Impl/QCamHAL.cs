using QCamManagedDriver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using xq = QCamManagedDriver.QCam;

namespace Lab.Acq
{
    /// <summary>
    /// Exposes basic operations for a QImaging camera
    /// </summary>
    class QCamHAL : Disposable, IQCam, ICameraHAL
    {
        readonly IntPtr camera = IntPtr.Zero;
        QCamM_SettingsEx settingsEx = new QCamM_SettingsEx();
        readonly ManualResetEventSlim disposeSignal = new ManualResetEventSlim();
        readonly CameraAttributes cameraAttributes = new CameraAttributes();
        readonly QCamM_Frame[] qCamFrameBuffers;

        /// <summary>
        /// Camera attributes which are constant during runtime
        /// </summary>
        public CameraAttributes CameraAttributes { get { return cameraAttributes.Duplicate(); } }

        /// <summary>
        /// Loads the QCam driver, attempts to open a camera, and initializes everything, but doesn't start acquiring
        /// </summary>
        public QCamHAL(int nFrameBuffers)
        {
            if (nFrameBuffers < 2)
                throw new ArgumentOutOfRangeException("Need at least 2 frame buffers to support full-frame streaming.");
            if (nFrameBuffers > 16)
                throw new ArgumentOutOfRangeException("Too many frame buffers.");

            // bug in ReleaseDriver, so we only call at start and end of program
            //xq.QCamM_LoadDriver().Check();

            Trace.TraceInformation("Opening QCAM camera...");
            OpenCamera(out camera);


            qCamFrameBuffers = new QCamM_Frame[nFrameBuffers];

            Trace.TraceInformation("Initializing QCAM camera...");
            Initialize();

            Trace.TraceInformation("Qimaging camera opened: " + cameraAttributes.ToString());


            copierDelegate = new Action<QCamM_Frame, VideoFrame>(CopyDataForOutput);
            queueSettingsCallbackDelegate = new QCamM_AsyncCallback(queueSettingsCallbackMethod);
            queueFrameCallbackDelegate = new QCamM_AsyncCallback(queueFrameCallbackMethod);
        }

        public static readonly int Default_NumFrameBuffers = 4;

        public QCamHAL() : this(Default_NumFrameBuffers) { }

        void OpenCamera(out IntPtr camera)
        {
            camera = IntPtr.Zero;
            var camList = new QCamM_CamListItem[10];
            uint listLen = (uint)camList.Length;

            // listLen is the length of your QCam_CamListItem array
            xq.QCamM_ListCameras(camList, ref listLen);

            // listLen is now the number of cameras available. It may be
            // larger than your QCam_CamListItem array length!

            if (listLen == 0)
                throw new QCamException("No Qimaging cameras were found on this system.  Check connections and power.");

            if (listLen > 1)
                throw new NotSupportedException("More than one Qimaging camera was found on this system.  Current software doesn't support choosing amongst them.");

            var camListItem = camList[0];
            if (camListItem.isOpen != 0)
                throw new QCamException("Qimaging camera is already open (by this or another program).");

            xq.QCamM_OpenCamera(camListItem.cameraId, ref camera).Check();
            
            cameraAttributes.Model = ((QCamM_qcCameraType)(camListItem.cameraType)).ToString().Remove(0, 8);

            string serNum = "";
            xq.QCamM_GetSerialString(camera, ref serNum);
            cameraAttributes.SerialNumber = serNum;

            cameraAttributes.FullWidth = checked((ushort)getInfo(QCamM_Info.qinfCcdWidth));
            cameraAttributes.FullHeight  = checked((ushort)getInfo(QCamM_Info.qinfCcdHeight));
            cameraAttributes.BitDepth = checked((byte)(getInfo(QCamM_Info.qinfBitDepth)));
        }

        void Initialize()
        {
            xq.QCamM_CreateCameraSettingsStruct(settingsEx).Check();
            var err = xq.QCamM_InitializeCameraSettings(camera, settingsEx);
            err.Check();
            xq.QCamM_ReadDefaultSettings(camera, settingsEx).Check();

            //check what type of CCD this camera has
            uint ccdTypeInt = 0;
            xq.QCamM_GetInfo(camera, QCamM_Info.qinfCcdType, ref ccdTypeInt).Check();
            QCamM_qcCcdType ccdType = (QCamM_qcCcdType)ccdTypeInt;
            if (ccdType != QCamM_qcCcdType.qcCcdMonochrome)
                throw new NotSupportedException("We only support monochrome CCDs in this version");
            xq.QCamM_SetParam(settingsEx, QCamM_Param.qprmImageFormat,
                (uint)QCamM_ImageFormat.qfmtMono16).Check();

            xq.QCamM_SendSettingsToCam(camera, settingsEx).Check();
            Trace.TraceInformation("Camera set to output 16-bit monochrome data.");

            xq.QCamM_IsSparseTable(settingsEx, QCamM_Param.qprmBinning).Check();

            uint[] table = new uint[32];
            int size = table.Length;
            xq.QCamM_GetParamSparseTable(settingsEx, QCamM_Param.qprmBinning, table, ref size).Check();

            var modes = new List<BinningMode>();
            foreach (uint b in table)
                if (b > 0)
                    modes.Add((BinningMode)b);
            cameraAttributes.SupportedBinning = modes.ToArray();
        }

        /// <summary>
        /// Video settings that may NOT be adjusted while acquiring live data
        /// </summary>
        public VideoSettingsStatic SettingsStatic
        {
            get
            {
                VideoSettingsStatic outputSettings = new VideoSettingsStatic();
                xq.QCamM_ReadSettingsFromCam(camera, settingsEx).Check();

                outputSettings.Binning = checked((BinningMode)getParam(QCamM_Param.qprmBinning));

                outputSettings.RoiX = checked((ushort)getParam(QCamM_Param.qprmRoiX));
                outputSettings.RoiY = checked((ushort)getParam(QCamM_Param.qprmRoiY));
                outputSettings.RoiWidth = checked((ushort)getParam(QCamM_Param.qprmRoiWidth));
                outputSettings.RoiHeight = checked((ushort)getParam(QCamM_Param.qprmRoiHeight));

                QCamM_qcTriggerType tt = checked((QCamM_qcTriggerType)getParam(QCamM_Param.qprmTriggerType));
                outputSettings.Trigger = Get_TriggerMode(tt);

                return outputSettings;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                if (isStreaming)
                    throw new InvalidOperationException("Cannot set while camera is running!");
                setParam(QCamM_Param.qprmBinning, (uint)value.Binning);

                setParam(QCamM_Param.qprmRoiX, checked((ushort)value.RoiX));
                setParam(QCamM_Param.qprmRoiY, checked((ushort)value.RoiY));
                setParam(QCamM_Param.qprmRoiWidth, checked((ushort)value.RoiWidth));
                setParam(QCamM_Param.qprmRoiHeight, checked((ushort)value.RoiHeight));

                QCamM_qcTriggerType tt = Get_qcTriggerType(value.Trigger);
                setParam(QCamM_Param.qprmTriggerType, checked((uint)tt));

                xq.QCamM_SendSettingsToCam(camera, settingsEx).Check();
            }
        }

        TriggeringMode Get_TriggerMode(QCamM_qcTriggerType tt)
        {
            switch (tt)
            {
                case QCamM_qcTriggerType.qcTriggerEdgeHi:
                    return TriggeringMode.HardwareEdgeHigh;
                case QCamM_qcTriggerType.qcTriggerEdgeLow:
                    return TriggeringMode.HardwareEdgeLow;
                case QCamM_qcTriggerType.qcTriggerFreerun:
                    return TriggeringMode.Freerun;
                case QCamM_qcTriggerType.qcTriggerSoftware:
                    return TriggeringMode.Software;
                default:
                    throw new QCamException("Unexpected trigger mode: " + tt.ToString());
            }
        }

        QCamM_qcTriggerType Get_qcTriggerType(TriggeringMode tm)
        {
            switch (tm)
            {
                case TriggeringMode.Software:
                    return QCamM_qcTriggerType.qcTriggerSoftware;
                case TriggeringMode.Freerun:
                    return QCamM_qcTriggerType.qcTriggerFreerun;
                case TriggeringMode.HardwareEdgeHigh:
                    return QCamM_qcTriggerType.qcTriggerEdgeHi;
                case TriggeringMode.HardwareEdgeLow:
                    return QCamM_qcTriggerType.qcTriggerEdgeLow;
                default:
                    throw new ArgumentException("Unexpected trigger mode: " + tm);
            }
        }


        readonly object driverStreamingLock = new object();
        bool isStreaming = false;
        readonly AutoResetEvent queueSettingsCallbackSync = new AutoResetEvent(false);
        readonly QCamM_AsyncCallback queueSettingsCallbackDelegate;
        QCamM_Err queueSettingsCallbackErr;
        void queueSettingsCallbackMethod(IntPtr userPtr, uint userData, QCamM_Err errcode, uint flags)
        {
            queueSettingsCallbackErr = errcode;
            queueSettingsCallbackSync.Set();
        }

        const double Mega = 1000000.0;
        //Offset range: [-292, 731]
        //Gain range: [-1940000, 30810000]

        public static readonly Range<int> RangeOffset = new Range<int>(-292, 731);
        public static readonly Range<float> RangeGain = new Range<float>(-1.940f, 30.810f);

        /// <summary>
        /// Video settings that may be adjusted while acquiring live data
        /// </summary>
        public VideoSettingsDynamic SettingsDynamic
        {
            get
            {
                xq.QCamM_ReadSettingsFromCam(camera, settingsEx).Check();
                
                int gainMicro = getParam(QCamM_ParamS32.qprmS32NormalizedGaindB);
                float gain_db = (float)(gainMicro / Mega);
                int offset = getParam(QCamM_ParamS32.qprmS32AbsoluteOffset);

                return new VideoSettingsDynamic { AnalogGain_dB = gain_db, AnalogOffset = offset };
            }
            set
            {
                if (!RangeGain.Allows(value.AnalogGain_dB))
                    throw new ArgumentOutOfRangeException("AnalogGain_db", "Gain must be within the range " + RangeGain);

                int s32gain_db = checked((int)(value.AnalogGain_dB * Mega));
                setParam(QCamM_ParamS32.qprmS32NormalizedGaindB, s32gain_db);

                if (!RangeOffset.Allows(value.AnalogOffset))
                    throw new ArgumentOutOfRangeException("AnalogOffset", "Offset must be within the range " + RangeOffset);
                setParam(QCamM_ParamS32.qprmS32AbsoluteOffset, value.AnalogOffset);

                lock (driverStreamingLock)
                {
                    if (isStreaming)
                    { // queue settings, but we still wait synchronously for settings to finish
                        xq.QCamM_QueueSettings(camera, settingsEx, queueSettingsCallbackDelegate,
                            (uint)QCamM_qcCallbackFlags.qcCallbackDone,
                            IntPtr.Zero, 0).Check();
                        if (!queueSettingsCallbackSync.WaitOne(2000))
                            throw new QCamException("QImaging: queued settings while streaming, but callback didn't complete within 2 seconds!");
                        queueSettingsCallbackErr.Check();
                    }
                    else
                        xq.QCamM_SendSettingsToCam(camera, settingsEx).Check();
                }
            }
        }

        public Range<int> GetParameterRange(QCamM_ParamS32 p)
        {
            int min = 0, max = 0;
            xq.QCamM_GetParamS32Min(settingsEx, p,  ref min).Check();
            xq.QCamM_GetParamS32Max(settingsEx, p,  ref max).Check();
            return new Range<int>(min, max);
        }

        /// <summary>
        /// Starts streaming data over firewire
        /// </summary>
        public void Start()
        {
            if (disposeSignal.Wait(0))
                throw new ObjectDisposedException("this");

            lock (driverStreamingLock)
            {
                if (isStreaming)
                    throw new InvalidOperationException("Already streaming!");

                xq.QCamM_Abort(camera).Check();

                uint expectedSize = checked((uint)(SettingsStatic.RoiWidth * SettingsStatic.RoiHeight * sizeof(ushort)));
                uint actualSize = getInfo(QCamM_Info.qinfImageSize);
                if (actualSize != expectedSize)
                    throw new QCamException(string.Format(
                        "ROI size is {0}x{1} meaning we should be getting {2} bytes of data per exposure, but QCam API says data size is {3}",
                        SettingsStatic.RoiWidth, SettingsStatic.RoiHeight, expectedSize, actualSize));

                odometer = 0;

                xq.QCamM_SetStreaming(camera, 1);
                QueueFrame(0, actualSize);
                isStreaming = true;
                for (int i = 1; i < qCamFrameBuffers.Length; i++)
                    QueueFrame(i, actualSize);
            }
        }

        public bool IsRunning { get { return isStreaming; } }

        /// <summary>
        /// Aborts any queued settings and frames, and stops streaming data over firewire
        /// </summary>
        public void Stop()
        {
            lock (driverStreamingLock)
            {
                xq.QCamM_Abort(camera).Check();
                xq.QCamM_SetStreaming(camera, 0);

                isStreaming = false;
            }
        }

        const byte QCam_counter_bits = 8 * sizeof(ushort); // based on fact that frame field in QCamM_Frame class is a ushort
        uint odometer = 0;
        readonly object odometerLock = new object();

        /// <summary>
        /// Returns the last error code set by a queued frame streaming callback
        /// </summary>
        /// <returns></returns>
        public int GetLastQCamCallbackErrorCode()
        {
            return Interlocked.Exchange(ref queueFrameCallbackErr, unchecked((int)(QCamM_Err.qerrSuccess)));
        }

        public IRingBufferWrite<VideoFrame> RingBufferForOutput { get; set; }


        const int QFRS_NONE = 0;
        const int QFRS_IN_CALLBACK = 1;

        Nullable<uint> lastThreadId = null;

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();
        const string callback_thread_msg = "QCam callback is re-entrant on OS thread ";
        uint CheckCurrentThread()
        {
            uint curThread = GetCurrentThreadId();
            if (lastThreadId == null)
                lastThreadId = curThread;
            else if (curThread != lastThreadId)
            {
                Trace.TraceWarning("QCam callback changed threads from {0} to {1}", lastThreadId, curThread);
                lastThreadId = curThread;
            }
            return curThread;
        }

        int queueFrameReentranceState = 0;
        readonly QCamM_AsyncCallback queueFrameCallbackDelegate;
        int queueFrameCallbackErr;
        void queueFrameCallbackMethod(IntPtr userPtr, uint userData, QCamM_Err errcode, uint flags)
        {
            int state = Interlocked.CompareExchange(ref queueFrameReentranceState,
                                                        QFRS_IN_CALLBACK, QFRS_NONE);
            if (state != QFRS_NONE)
            {
                string msg = callback_thread_msg + CheckCurrentThread() + " @start";
                if (CrashIfCallbackReEntranceDetected)
                    throw new Exception(msg);
                else
                    Trace.TraceError(msg);
            }
            else
                CheckCurrentThread();

            if (disposeSignal.Wait(0))
                return;

            queueFrameCallbackErr = unchecked((int)errcode);
            int bufferIdx = checked((int)userData);
            QCamM_Frame qCamFrame = qCamFrameBuffers[bufferIdx];


            bool ok = RingBufferForOutput.TryCopyIn<QCamM_Frame, VideoFrame>(copierDelegate, qCamFrame);
            if (!ok)
                Trace.Write("!");

            // re-insert buffer into the driver's queue
            QueueFrame(bufferIdx, qCamFrame.bufferSize);

            state = Interlocked.CompareExchange(ref queueFrameReentranceState,
                                QFRS_NONE, QFRS_IN_CALLBACK);

            if (state != QFRS_IN_CALLBACK)
            {
                string msg = callback_thread_msg + CheckCurrentThread() + " @end";
                if (CrashIfCallbackReEntranceDetected)
                    throw new Exception("QCam callback is re-entrant (end");
                else
                    Trace.TraceError(msg);
            }
        }

        public bool CrashIfCallbackReEntranceDetected { get; set; }

        /// <summary>
        /// Copies data from QCam frame data structure to standardized VideoFrame structure
        /// which can be serialized over the wire
        /// </summary>
        void CopyDataForOutput(QCamM_Frame qFrame, VideoFrame outFrame)
        {
            outFrame.ErrorCode = qFrame.errorCode;
            outFrame.BitsPerPixel = qFrame.bits;
            outFrame.Width = qFrame.width;
            outFrame.Height = qFrame.height;
            lock (odometerLock)
            {
                uint newOdo = CounterUtils.UnwrapRolledCounter(QCam_counter_bits, odometer, qFrame.frameNumber);
                long nFrameDrops = newOdo - (long)odometer - 1;
                if (nFrameDrops > 0)
                    Trace.TraceWarning("QCam driver dropped {0} frames!", nFrameDrops);
                odometer = newOdo;
                outFrame.FrameNumber = newOdo;
            }
            outFrame.TimeStamp = qFrame.timeStamp;

            IntPtr pSrc = qFrame.pBuffer;
            if (pSrc == IntPtr.Zero)
                throw new ArgumentException("Got QCam frame buffer with null data pointer");

            uint nBytes = qFrame.size;
            outFrame.DataSizeBytes = nBytes;

            // if there is data available, there's a buffer of the correct size, and copy it
            if (nBytes > 0)
            {
                byte[] buffer = outFrame.Data;
                if ((buffer == null) || (buffer.Length != nBytes))
                    buffer = new byte[nBytes];

                Lab.Utilities.CopyPointerToBuffer(pSrc, buffer, 0, nBytes);

                outFrame.Data = buffer;
            }
            else
            {
                outFrame.Data = new byte[0];
                Trace.TraceWarning("Sending empty frame!");
            }
        }
        Action<QCamM_Frame, VideoFrame> copierDelegate;


        /// <summary>
        /// Preps a frame buffer and inserts it into the QCam streaming queue
        /// </summary>
        void QueueFrame(int frameNum, uint dataSize)
        {
            if ((frameNum < 0) || (dataSize < 1))
                throw new ArgumentOutOfRangeException();

            if (disposeSignal.Wait(0))
                return;

            QCamM_Frame frame = qCamFrameBuffers[frameNum];

            // check that buffer exists and is sized correctly
            if (frame == null)
            {
                frame = new QCamM_Frame();
                qCamFrameBuffers[frameNum] = frame;
            }
            if (frame.bufferSize != dataSize)
            {
                // release existing buffer, if present
                if (frame.pBuffer != IntPtr.Zero)
                    xq.QCamM_Free(frame.pBuffer);

                // update buffer size
                frame.bufferSize = (uint)dataSize;
                frame.pBuffer = xq.QCamM_Malloc((uint)dataSize);
                if (frame.pBuffer == IntPtr.Zero)
                {
                    Trace.TraceError("QCam_malloc returned a null pointer!");
                    return;
                }
            }
            var err = xq.QCamM_QueueFrame(camera, frame,
                queueFrameCallbackDelegate, (uint)QCamM_qcCallbackFlags.qcCallbackDone,
                IntPtr.Zero, (uint)frameNum);
            if (err != QCamM_Err.qerrSuccess)
                Trace.TraceWarning("QCamM_QueueFrame returned " + err.ToString());
        }


        /// <summary>
        /// Stops any acquisition in progress and releases resources.
        /// Unlike other methods on this class, we do not throw exceptions here.
        /// Instead, errors are reported to Trace, and cleanup continues
        /// </summary>
        protected override void RunOnceDisposer()
        {
            disposeSignal.Set();

            QCamM_Err err;

            // before we release any resources, STOP EVERYTHING
            err = xq.QCamM_Abort(camera);
            if (err != QCamM_Err.qerrSuccess)
                Trace.TraceError("Error while aborting camera: " + err.ToString());

            if (qCamFrameBuffers != null)
            {
                foreach (var f in qCamFrameBuffers)
                {
                    if (f != null)
                    {
                        if (f.pBuffer != IntPtr.Zero)
                            xq.QCamM_Free(f.pBuffer);
                        f.bufferSize = 0;
                    }
                }
            }

            settingsEx.TryDispose();

            if (camera != IntPtr.Zero)
                err = xq.QCamM_CloseCamera(camera);
            if (err != QCamM_Err.qerrSuccess)
                Trace.TraceError("Error closing QCam camera: " + err.ToString());

            // bug in driver: ReleaseDriver() closes standard output stream so we don't call
            //xq.QCamM_ReleaseDriver();
        }

        /*  Camera settings code that hasn't yet been translated
         *             // Prepare the UI control based on camera capabilities
            if ( QCam.QCamM_IsParamSupported( mhCamera, QCamM_Param.qprmGain ) == QCamM_Err.qerrSuccess )
            {
                gbGain.Enabled = true;
                uint val = 0;
                QCam.QCamM_GetParamMin( mSettings, QCamM_Param.qprmGain, ref val );
                tbGain.Minimum = (int)val;
                QCam.QCamM_GetParamMax( mSettings, QCamM_Param.qprmGain, ref val );
                tbGain.Maximum = (int)val;
                QCam.QCamM_GetParam( mSettings, QCamM_Param.qprmGain, ref val );
                tbGain.Value = (int)val;
            }
            else
            {
                gbGain.Enabled = false;
            }
            // Get the current exposure value set in the camera
            if ( QCam.QCamM_IsParamSupported( mhCamera, QCamM_Param.qprmExposure ) == QCamM_Err.qerrSuccess )
            {
                gbExposure.Enabled = true;
                uint val = 0;
                QCam.QCamM_GetParam( mSettings, QCamM_Param.qprmExposure, ref val );
                tbExposure.Value = (int)( val / 1000 ); // Convert from us to ms
            }
         * */


        #region Helper functions

        /// <summary>
        /// Returns a uint camera information value for the given key
        /// </summary>
        uint getInfo(QCamM_Info camInfo)
        {
            uint value = 0;
            xq.QCamM_GetInfo(camera, camInfo, ref value).Check();
            return value;
        }

        /// <summary>
        /// Sets a uint parameter to the local settingsEx field.
        /// THIS DOESN'T STORE TO THE CAMERA.  CALL SendSettingsToCam() AFTER THIS.
        /// </summary>
        void setParam(QCamM_Param param, uint value)
        {
            xq.QCamM_SetParam(settingsEx, param, value).Check();
        }

        /// <summary>
        /// Sets an int parameter to the local settingsEx field.
        /// THIS DOESN'T STORE TO THE CAMERA.  CALL SendSettingsToCam() AFTER THIS.
        /// </summary>
        void setParam(QCamM_ParamS32 param, int value)
        {
            xq.QCamM_SetParamS32(settingsEx, param, value).Check();
        }

        /// <summary>
        /// Sets a ulong parameter to the local settingsEx field.
        /// THIS DOESN'T STORE TO THE CAMERA.  CALL SendSettingsToCam() AFTER THIS.
        /// </summary>
        void setParam(QCamM_Param64 param, ulong value)
        {
            xq.QCamM_SetParam64(settingsEx, param, value).Check();
        }

        /// <summary>
        /// Returns a uint parameter value from the local settingsEx field.
        /// THIS DOESN'T READ DIRECTLY FROM THE CAMERA! CALL ReadSettingsFromCam() PRIOR TO THIS
        /// </summary>
        uint getParam(QCamM_Param param)
        {
            uint value = 0;
            xq.QCamM_GetParam(settingsEx, param, ref value).Check();
            return value;
        }

        
        /// <summary>
        /// Returns a int parameter value from the local settingsEx field.
        /// THIS DOESN'T READ DIRECTLY FROM THE CAMERA! CALL ReadSettingsFromCam() PRIOR TO THIS
        /// </summary>
        int getParam(QCamM_ParamS32 param)
        {
            int value = 0;
            xq.QCamM_GetParamS32(settingsEx, param, ref value).Check();
            return value;
        }

        /// <summary>
        /// Returns a ulong parameter value from the local settingsEx field.
        /// THIS DOESN'T READ DIRECTLY FROM THE CAMERA! CALL ReadSettingsFromCam() PRIOR TO THIS
        /// </summary>
        ulong getParam(QCamM_Param64 param)
        {
            ulong value = 0;
            xq.QCamM_GetParam64(settingsEx, param, ref value).Check();
            return value;
        }

        #endregion

    }
}
