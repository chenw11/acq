using System;
using eas_lab.acq.DmdCam;

namespace Lab.Acq
{
    public static class CcdCamXopFrame
    {
        public static CcdCamXop singleton = new CcdCamXop();

        public static CcdCamXop Default { get { return singleton; } }

        public static void Reset()
        {
            singleton.TryDispose();
            singleton = new CcdCamXop();
        }

        public static void GlobalCleanup()
        {
            singleton.TryDispose();
            singleton = null;
        }

        public static Action<string> ErrorHandler { get; set; }

        public static ErrorCode Error(string msg)
        {
            Reset();
            ErrorReporting.CopyToClipboard(msg);

            var e = ErrorHandler;
            if (e != null)
                e(msg);

            return ErrorCode.XopError_CustomMessage;
        }
    }
}
