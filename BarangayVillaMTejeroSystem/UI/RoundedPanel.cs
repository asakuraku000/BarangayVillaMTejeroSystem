using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BarangayVillaMTejeroSystem.UI
{
    /// <summary>
    /// A plain panel painted with rounded corners. Used for login cards,
    /// dashboard stat cards, and placeholder content cards.
    /// </summary>
    public class RoundedPanel : Panel
    {
        public int CornerRadius { get; set; } = 14;
        public Color BorderColor { get; set; } = Color.FromArgb(230, 233, 238);
        public int BorderThickness { get; set; } = 1;

        public RoundedPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
            DoubleBuffered = true;
        }

        protected override void OnResize(System.EventArgs e)
        {
            base.OnResize(e);
            RoundHelper.ApplyRoundedRegion(this, CornerRadius);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundHelper.RoundedRect(rect, CornerRadius);
            using var bg = new SolidBrush(BackColor);
            e.Graphics.FillPath(bg, path);

            if (BorderThickness > 0)
            {
                using var pen = new Pen(BorderColor, BorderThickness);
                e.Graphics.DrawPath(pen, path);
            }
        }
    }
}
