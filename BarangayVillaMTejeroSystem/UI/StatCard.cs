using System.Drawing;
using System.Windows.Forms;

namespace BarangayVillaMTejeroSystem.UI
{
    /// <summary>
    /// A small rounded "stat card" used on the dashboard to summarize
    /// system statistics (e.g. total residents, transactions). Values are
    /// placeholders until the corresponding modules are connected to data.
    /// </summary>
    public class StatCard : RoundedPanel
    {
        private readonly Label _valueLabel;
        private readonly Label _titleLabel;
        private readonly Panel _iconBadge;
        private readonly Label _iconLabel;

        public StatCard(string icon, string title, string value, Color accent)
        {
            BackColor = Color.White;
            CornerRadius = 14;
            BorderColor = Color.FromArgb(232, 235, 240);
            Padding = new Padding(18);

            _iconBadge = new Panel
            {
                Size = new Size(44, 44),
                Location = new Point(18, 18),
                BackColor = Color.White
            };
            _iconBadge.Paint += (s, e) =>
            {
                using var path = RoundHelper.RoundedRect(new Rectangle(0, 0, _iconBadge.Width - 1, _iconBadge.Height - 1), 10);
                using var brush = new SolidBrush(Color.FromArgb(35, accent.R, accent.G, accent.B));
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.FillPath(brush, path);
            };

            _iconLabel = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 16f),
                ForeColor = accent,
                AutoSize = false,
                Size = new Size(44, 44),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            _iconBadge.Controls.Add(_iconLabel);

            _valueLabel = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 37, 66),
                AutoSize = true,
                Location = new Point(18, 72)
            };

            _titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(120, 130, 145),
                AutoSize = true,
                Location = new Point(18, 110)
            };

            Controls.Add(_iconBadge);
            Controls.Add(_valueLabel);
            Controls.Add(_titleLabel);
        }
    }
}
