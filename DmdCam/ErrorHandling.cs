using System.Windows;

namespace Lab.Acq
{
    public enum ErrorCode : int
    {
        OK = 0,
        XopError_CustomMessage = 10001, // 1 + FIRST_XOP_ERROR
    }

    public static class ErrorReporting
    {
        public static void MsgBox(string msg)
        {
            MessageBox.Show(msg, "XOP Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static void CopyToClipboard(string msg)
        {
            Clipboard.SetText(msg);
        }
    }
}
