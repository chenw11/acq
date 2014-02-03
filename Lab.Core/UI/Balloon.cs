using System.Drawing;
using System.Windows.Forms;

namespace Lab.UI
{
    public static class Balloon
    {
        public static void ShowBalloon(string title, string text)
        {
            Icon i = (Icon)Lab.Properties.Resources.warning.Clone();
            NotifyIcon ni = new NotifyIcon();
            ni.Icon = i;
            ni.Visible = true;
            ni.Text = title;
            ni.ShowBalloonTip(60000, title, text, ToolTipIcon.Error);
            ni.BalloonTipClosed += (s, e) => ni.Visible = false;
        }
    }
}
