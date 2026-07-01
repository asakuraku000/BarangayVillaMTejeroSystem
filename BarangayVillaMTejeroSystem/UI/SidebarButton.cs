using System.Drawing;
using System.Windows.Forms;

namespace BarangayVillaMTejeroSystem.UI
{
    /// <summary>
    /// A navigation item for the dashboard sidebar: an icon glyph + label,
    /// with a hover tint and a left accent bar when active/selected.
    /// </summary>
    public class SidebarButton : Button
    {
        public string IconGlyph { get; set; } = "";
        public bool IsActive { get; private set; }

        private static readonly Color IdleColor = Color.FromArgb(16, 37, 66);
        private static readonly Color HoverColor = Color.FromArgb(27, 51, 87);
        private static readonly Color ActiveColor = Color.FromArgb(31, 58, 97);
        private static readonly Color AccentColor = Color.FromArgb(200, 29, 37);
        private static readonly Color TextColor = Color.FromArgb(220, 226, 235);
        private static readonly Color TextActiveColor = Color.White;

        public SidebarButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseOverBackColor = HoverColor;
            FlatAppearance.MouseDownBackColor = ActiveColor;
            TextAlign = ContentAlignment.MiddleLeft;
            BackColor = IdleColor;
            ForeColor = TextColor;
            Height = 46;
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 10.5f, FontStyle.Regular);
            MouseEnter += (_, _) => { if (!IsActive) { BackColor = HoverColor; Invalidate(); } };
            MouseLeave += (_, _) => { if (!IsActive) { BackColor = IdleColor; Invalidate(); } };
        }

        public void SetActive(bool active)
        {
            IsActive = active;
            BackColor = active ? ActiveColor : IdleColor;
            ForeColor = active ? TextActiveColor : TextColor;
            Font = new Font("Segoe UI", 10.5f, active ? FontStyle.Bold : FontStyle.Regular);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var g = pevent.Graphics;
            using var bg = new SolidBrush(BackColor);
            g.FillRectangle(bg, ClientRectangle);

            if (IsActive)
            {
                using var accent = new SolidBrush(AccentColor);
                g.FillRectangle(accent, 0, 0, 4, Height);
            }

            string label = $"{IconGlyph}   {Text}";
            var textRect = new Rectangle(24, 0, Width - 24, Height);
            TextRenderer.DrawText(g, label, Font, textRect, ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}
