using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BarangayVillaMTejeroSystem.UI
{
    /// <summary>
    /// A small eye / eye-slash icon button used to toggle password visibility
    /// inside a text field. Drawn entirely with GDI+ so no image assets are required.
    /// Click toggles <see cref="PasswordVisible"/> and redraws the icon:
    ///   - Eye-slash (crossed out)  => password is currently hidden
    ///   - Open eye (with pupil)    => password is currently shown
    /// </summary>
    public class EyeToggleButton : Control
    {
        private bool _passwordVisible;
        private bool _hovering;

        public Color IconColor { get; set; } = Color.FromArgb(150, 158, 170);
        public Color HoverColor { get; set; } = Color.FromArgb(27, 90, 130);

        /// <summary>True when the password is currently shown in plain text.</summary>
        public bool PasswordVisible
        {
            get => _passwordVisible;
            set
            {
                if (_passwordVisible == value) return;
                _passwordVisible = value;
                Invalidate();
            }
        }

        public EyeToggleButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                      ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
            // NOTE: real transparency isn't supported by plain WinForms Controls. Instead of
            // faking it (which leaves the paint buffer uncleared and shows garbage pixels),
            // this control just uses a solid BackColor that the caller sets to match whatever
            // field/panel it sits inside (see LoginForm — it's set to the password field's color).
            BackColor = Color.FromArgb(247, 248, 250);
            Cursor = Cursors.Hand;
            Size = new Size(22, 22);
            TabStop = false;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovering = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovering = false;
            Invalidate();
        }

        protected override void OnClick(EventArgs e)
        {
            // Flip state first so subscribers of Click see the up-to-date value.
            PasswordVisible = !PasswordVisible;
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Always paint a clean solid background first — otherwise leftover pixels
            // from the double-buffer can bleed through as visual garbage.
            using (var bg = new SolidBrush(BackColor))
                g.FillRectangle(bg, ClientRectangle);

            Color color = _hovering ? HoverColor : IconColor;
            var rect = new Rectangle(1, 3, Width - 2, Height - 6);

            float cx = rect.Left + rect.Width / 2f;
            float cy = rect.Top + rect.Height / 2f;
            float halfW = rect.Width * 0.46f;
            float halfH = rect.Height * 0.34f;

            using var path = new GraphicsPath();
            var left = new PointF(cx - halfW, cy);
            var right = new PointF(cx + halfW, cy);
            var topC1 = new PointF(cx - halfW * 0.5f, cy - halfH * 2.6f);
            var topC2 = new PointF(cx + halfW * 0.5f, cy - halfH * 2.6f);
            var botC1 = new PointF(cx + halfW * 0.5f, cy + halfH * 2.6f);
            var botC2 = new PointF(cx - halfW * 0.5f, cy + halfH * 2.6f);

            path.AddBezier(left, topC1, topC2, right);
            path.AddBezier(right, botC1, botC2, left);
            path.CloseFigure();

            using var pen = new Pen(color, 1.6f) { LineJoin = LineJoin.Round };
            g.DrawPath(pen, path);

            if (_passwordVisible)
            {
                // Open eye: draw the pupil/iris.
                float r = halfH * 0.95f;
                using var irisBrush = new SolidBrush(color);
                g.FillEllipse(irisBrush, cx - r / 2f, cy - r / 2f, r, r);
            }
            else
            {
                // Hidden password: eye-slash, diagonal line through the outline.
                using var slashPen = new Pen(color, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawLine(slashPen, rect.Left, rect.Top, rect.Right, rect.Bottom);
            }
        }
    }
}
