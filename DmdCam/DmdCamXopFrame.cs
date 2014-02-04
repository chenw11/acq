using System;

namespace Lab.Acq
{
    public static class DmdCamXopFrame
    {
        public static DmdCamXop singleton = new DmdCamXop();

        public static DmdCamXop Default { get { return singleton; } }

        public static void Reset()
        {
            singleton.TryDispose();
            singleton = new DmdCamXop();
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
