using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BarangayVillaMTejeroSystem.UI
{
    /// <summary>
    /// A flat, rounded-corner button with a hover color transition.
    /// Used for primary actions like "Log In".
    /// </summary>
    public class FlatButton : Button
    {
        public int CornerRadius { get; set; } = 8;
        public Color NormalColor { get; set; } = Color.FromArgb(200, 29, 37);
        public Color HoverColor { get; set; } = Color.FromArgb(170, 22, 29);

        // Tracks hover state directly instead of stashing it in Control.BackColor.
        // (The old code set BackColor = NormalColor here in the constructor, but
        // object-initializer syntax like `new FlatButton { NormalColor = green }`
        // assigns NormalColor *after* the constructor runs — so BackColor was
        // locked to the default red above until the button was hovered once.)
        private bool _isHovering;

        public FlatButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            Cursor = Cursors.Hand;
            MouseEnter += (_, _) => { _isHovering = true; Invalidate(); };
            MouseLeave += (_, _) => { _isHovering = false; Invalidate(); };
        }

        protected override void OnResize(System.EventArgs e)
        {
            base.OnResize(e);
            RoundHelper.ApplyRoundedRegion(this, CornerRadius);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundHelper.RoundedRect(rect, CornerRadius);
            using var bg = new SolidBrush(_isHovering ? HoverColor : NormalColor);
            pevent.Graphics.FillPath(bg, path);

            TextRenderer.DrawText(pevent.Graphics, Text, Font, ClientRectangle, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}
