using System;
using System.Drawing;
using System.Windows.Forms;
using BarangayVillaMTejeroSystem.Models;
using BarangayVillaMTejeroSystem.UI;

namespace BarangayVillaMTejeroSystem.Forms
{
    /// <summary>
    /// Read-only dialog for the "View Complete Resident Profile and History"
    /// feature. Shows the full profile plus a Document History card. The
    /// history list is a "Coming Soon" placeholder for now — once the
    /// Barangay Documents module exists, wire it to pull that resident's
    /// issued documents by ResidentId here instead of showing the placeholder.
    /// </summary>
    public class ResidentProfileDialog : Form
    {
        private static readonly Color NavyDark = Color.FromArgb(16, 37, 66);
        private static readonly Color MutedText = Color.FromArgb(140, 148, 160);
        private static readonly Color BorderGray = Color.FromArgb(230, 233, 238);
        private static readonly Color TealAccent = Color.FromArgb(27, 90, 130);
        private static readonly Color SuccessGreen = Color.FromArgb(60, 130, 90);
        private static readonly Color SuccessGreenBg = Color.FromArgb(232, 244, 236);
        private static readonly Color InactiveGrayBg = Color.FromArgb(240, 242, 245);
        private static readonly Color InactiveGrayText = Color.FromArgb(130, 138, 150);

        public ResidentProfileDialog(Resident resident)
        {
            Text = "Resident Profile";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);

            int maxVisibleHeight = Math.Max(420, Screen.PrimaryScreen.WorkingArea.Height - 120);
            ClientSize = new Size(460, Math.Min(680, maxVisibleHeight));

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.White
            };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64f));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            Controls.Add(root);

            var scrollArea = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(0, 0, 0, 16)
            };
            root.Controls.Add(scrollArea, 0, 0);

            int y = 20;
            int fieldWidth = 404;

            // ----- Header: avatar + name + status pill -----
            var avatar = new RoundedPanel
            {
                Size = new Size(48, 48),
                CornerRadius = 24,
                BackColor = TealAccent,
                BorderThickness = 0,
                Location = new Point(28, y)
            };
            var avatarLabel = new Label
            {
                Text = resident.Initial,
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            avatar.Controls.Add(avatarLabel);
            scrollArea.Controls.Add(avatar);

            var lblName = new Label
            {
                Text = resident.FullName,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = NavyDark,
                AutoSize = true,
                Location = new Point(86, y + 2)
            };
            scrollArea.Controls.Add(lblName);

            var lblStatus = new Label
            {
                Text = resident.IsActive ? "● Active" : "● Inactive",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = resident.IsActive ? SuccessGreen : InactiveGrayText,
                BackColor = resident.IsActive ? SuccessGreenBg : InactiveGrayBg,
                AutoSize = true,
                Padding = new Padding(8, 3, 8, 3),
                Location = new Point(86, y + 26)
            };
            scrollArea.Controls.Add(lblStatus);
            y += 66;

            var divider1 = new Panel { Location = new Point(28, y), Size = new Size(fieldWidth, 1), BackColor = BorderGray };
            scrollArea.Controls.Add(divider1);
            y += 16;

            // ----- Profile fields -----
            AddRow(scrollArea, "Age / Gender", $"{resident.Age} / {resident.GenderLabel}", ref y, fieldWidth);
            AddRow(scrollArea, "Birth Date", resident.BirthDate.ToString("MMMM d, yyyy"), ref y, fieldWidth);
            AddRow(scrollArea, "Civil Status", resident.CivilStatusLabel, ref y, fieldWidth);
            AddRow(scrollArea, "Purok / Address", resident.Purok, ref y, fieldWidth);
            AddRow(scrollArea, "Contact No.", string.IsNullOrWhiteSpace(resident.ContactNo) ? "—" : resident.ContactNo, ref y, fieldWidth);
            AddRow(scrollArea, "Occupation", string.IsNullOrWhiteSpace(resident.Occupation) ? "—" : resident.Occupation, ref y, fieldWidth);
            AddRow(scrollArea, "Household Composition", resident.HouseholdMembersDisplay, ref y, fieldWidth, wrap: true);
            AddRow(scrollArea, "Date Registered", resident.DateRegistered.ToString("MMMM d, yyyy"), ref y, fieldWidth);
            if (!string.IsNullOrWhiteSpace(resident.Remarks))
                AddRow(scrollArea, "Remarks", resident.Remarks, ref y, fieldWidth, wrap: true);

            y += 8;
            var divider2 = new Panel { Location = new Point(28, y), Size = new Size(fieldWidth, 1), BackColor = BorderGray };
            scrollArea.Controls.Add(divider2);
            y += 16;

            // ----- Document History card (placeholder — hook for the Documents module) -----
            var lblHistoryTitle = new Label
            {
                Text = "Document History",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = NavyDark,
                AutoSize = true,
                Location = new Point(28, y)
            };
            scrollArea.Controls.Add(lblHistoryTitle);
            y += 30;

            var historyCard = new RoundedPanel
            {
                Location = new Point(28, y),
                Size = new Size(fieldWidth, 90),
                BackColor = Color.FromArgb(250, 251, 252),
                CornerRadius = 12,
                BorderColor = BorderGray
            };
            historyCard.Controls.Add(new Label
            {
                Text = "No documents issued yet.",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(16, 16)
            });
            historyCard.Controls.Add(new Label
            {
                Text = "This will list every Certificate of Residency, Indigency, Clearance,\netc. issued to this resident once the Barangay Documents module is connected.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = MutedText,
                AutoSize = false,
                Size = new Size(fieldWidth - 32, 44),
                Location = new Point(16, 38)
            });
            scrollArea.Controls.Add(historyCard);
            y += 90 + 20;

            scrollArea.Controls.Add(new Panel { Location = new Point(0, y), Size = new Size(1, 1) });

            // ===== Fixed footer =====
            var footer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            var footerLine = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = BorderGray };
            footer.Controls.Add(footerLine);

            var btnClose = new FlatButton
            {
                Text = "CLOSE",
                Size = new Size(fieldWidth, 42),
                Location = new Point(28, 12),
                DialogResult = DialogResult.OK
            };
            footer.Controls.Add(btnClose);

            root.Controls.Add(footer, 0, 1);
            AcceptButton = btnClose;
            CancelButton = btnClose;
        }

        private void AddRow(Panel host, string label, string value, ref int y, int width, bool wrap = false)
        {
            var lbl = new Label
            {
                Text = label.ToUpper(),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(28, y)
            };
            host.Controls.Add(lbl);
            y += 16;

            var valueHeight = wrap ? 36 : 20;
            var valLabel = new Label
            {
                Text = string.IsNullOrWhiteSpace(value) ? "—" : value,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = NavyDark,
                AutoSize = false,
                Size = new Size(width, valueHeight),
                Location = new Point(28, y)
            };
            host.Controls.Add(valLabel);
            y += valueHeight + 10;
        }
    }
}
