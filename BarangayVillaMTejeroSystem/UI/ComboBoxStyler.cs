using System.Drawing;
using System.Windows.Forms;

namespace BarangayVillaMTejeroSystem.UI
{
    /// <summary>
    /// WinForms silently ignores any Height you set on a DropDownList
    /// ComboBox — it always renders at a thin, font-driven height no matter
    /// what Size you give it. That's why filter dropdowns end up looking
    /// "payat" next to a taller search box beside them.
    ///
    /// This switches the combo into owner-draw mode so it can actually be
    /// made taller (matching neighboring fields), while keeping the same
    /// flat look: white background, given text color, light highlight on
    /// the active/hovered item.
    /// </summary>
    public static class ComboBoxStyler
    {
        public static void MakeTaller(ComboBox combo, int height, Color textColor, Color? highlightColor = null)
        {
            Color highlight = highlightColor ?? Color.FromArgb(232, 240, 245);

            combo.DrawMode = DrawMode.OwnerDrawFixed;
            combo.ItemHeight = height;
            combo.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;

                bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                using (var bg = new SolidBrush(selected ? highlight : Color.White))
                    e.Graphics.FillRectangle(bg, e.Bounds);

                string text = combo.Items[e.Index]?.ToString() ?? "";
                var textRect = new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 14, e.Bounds.Height);
                TextRenderer.DrawText(e.Graphics, text, combo.Font, textRect, textColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);

                e.DrawFocusRectangle();
            };
        }
    }
}
