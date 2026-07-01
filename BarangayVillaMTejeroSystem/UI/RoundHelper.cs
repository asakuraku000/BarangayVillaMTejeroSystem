using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BarangayVillaMTejeroSystem.UI
{
    /// <summary>
    /// Small helper for giving plain WinForms controls soft, rounded corners
    /// since native WinForms controls are square by default.
    /// </summary>
    public static class RoundHelper
    {
        public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.StartFigure();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void ApplyRoundedRegion(Control control, int radius)
        {
            if (control.Width <= 0 || control.Height <= 0) return;
            var rect = new Rectangle(0, 0, control.Width, control.Height);
            using var path = RoundedRect(rect, radius);
            control.Region = new Region(path);
        }
    }
}
