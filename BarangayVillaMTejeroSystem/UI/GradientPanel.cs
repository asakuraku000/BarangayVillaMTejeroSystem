using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BarangayVillaMTejeroSystem.UI
{
    /// <summary>
    /// A panel that paints a smooth diagonal gradient background.
    /// Used for the accent side of the login screen.
    /// </summary>
    public class GradientPanel : Panel
    {
        public Color ColorTop { get; set; } = Color.FromArgb(16, 37, 66);
        public Color ColorBottom { get; set; } = Color.FromArgb(27, 107, 147);

        public GradientPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var rect = new Rectangle(0, 0, Width, Height);
            using var brush = new LinearGradientBrush(rect, ColorTop, ColorBottom, 35f);
            e.Graphics.FillRectangle(brush, rect);
            base.OnPaint(e);
        }
    }
}
