using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BarangayVillaMTejeroSystem.Forms;
using BarangayVillaMTejeroSystem.Models;
using BarangayVillaMTejeroSystem.Services;
using BarangayVillaMTejeroSystem.UI;

namespace BarangayVillaMTejeroSystem.Controls
{
    /// <summary>
    /// Full Resident Records module: lists all registered residents with
    /// search and purok/status filtering, and lets Administrator and Staff
    /// accounts add, edit, view full profiles, and activate/deactivate or
    /// permanently delete resident records.
    /// </summary>
    public class ResidentManagementControl : Panel
    {
        private static readonly Color NavyDark = Color.FromArgb(16, 37, 66);
        private static readonly Color NavySidebar = Color.FromArgb(16, 37, 66);
        private static readonly Color BgLight = Color.FromArgb(244, 246, 249);
        private static readonly Color AccentRed = Color.FromArgb(200, 29, 37);
        private static readonly Color TealAccent = Color.FromArgb(27, 90, 130);
        private static readonly Color BorderGray = Color.FromArgb(232, 235, 240);
        private static readonly Color MutedText = Color.FromArgb(140, 148, 160);
        private static readonly Color SuccessGreen = Color.FromArgb(60, 130, 90);
        private static readonly Color SuccessGreenBg = Color.FromArgb(232, 244, 236);
        private static readonly Color InactiveGrayBg = Color.FromArgb(240, 242, 245);
        private static readonly Color InactiveGrayText = Color.FromArgb(130, 138, 150);

        private const string ColFullName = "colFullName";
        private const string ColAgeGender = "colAgeGender";
        private const string ColPurok = "colPurok";
        private const string ColContact = "colContact";
        private const string ColCivilStatus = "colCivilStatus";
        private const string ColStatus = "colStatus";
        private const string ColView = "colView";
        private const string ColEdit = "colEdit";
        private const string ColToggle = "colToggle";
        private const string ColDelete = "colDelete";

        private readonly UserAccount _currentUser;
        private DataGridView _grid;
        private TextBox _txtSearch;
        private ComboBox _cmbPurokFilter;
        private ComboBox _cmbStatusFilter;
        private Label _lblCount;

        public ResidentManagementControl(UserAccount currentUser)
        {
            _currentUser = currentUser;
            Dock = DockStyle.Fill;
            BackColor = BgLight;
            Padding = new Padding(28, 20, 28, 24);

            BuildUi();
            RefreshGrid();
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = BgLight
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            Controls.Add(root);

            // ===== Toolbar: search + purok/status filters (left), Add Resident button (right) =====
            var toolbar = new Panel { Dock = DockStyle.Fill, BackColor = BgLight };

            var searchBox = new Panel
            {
                Location = new Point(0, 8),
                Size = new Size(250, 38),
                BackColor = Color.White
            };
            searchBox.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderGray);
                e.Graphics.DrawRectangle(pen, 0, 0, searchBox.Width - 1, searchBox.Height - 1);
            };
            // Fixed-size icon badge (instead of AutoSize) so its footprint is
            // predictable and can never grow into the textbox that follows it.
            var lblSearchIcon = new Label
            {
                Text = "🔎",
                Font = new Font("Segoe UI Emoji", 9.5f),
                ForeColor = MutedText,
                AutoSize = false,
                Size = new Size(22, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(8, 9)
            };
            searchBox.Controls.Add(lblSearchIcon);
            _txtSearch = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(36, 9),
                Width = 204
            };
            _txtSearch.PlaceholderText = "Search by name, purok, or contact no...";
            _txtSearch.TextChanged += (_, _) => RefreshGrid();
            searchBox.Controls.Add(_txtSearch);
            toolbar.Controls.Add(searchBox);

            _cmbPurokFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(262, 8),
                Size = new Size(150, 38),
                FlatStyle = FlatStyle.Flat
            };
            _cmbPurokFilter.Items.Add("All Puroks");
            foreach (var purok in ResidentService.GetPuroks())
                _cmbPurokFilter.Items.Add(purok);
            _cmbPurokFilter.SelectedIndex = 0;
            _cmbPurokFilter.SelectedIndexChanged += (_, _) => RefreshGrid();
            ComboBoxStyler.MakeTaller(_cmbPurokFilter, 34, NavyDark);
            toolbar.Controls.Add(_cmbPurokFilter);

            _cmbStatusFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(424, 8),
                Size = new Size(140, 38),
                FlatStyle = FlatStyle.Flat
            };
            _cmbStatusFilter.Items.Add("All Statuses");
            _cmbStatusFilter.Items.Add("Active");
            _cmbStatusFilter.Items.Add("Inactive");
            _cmbStatusFilter.SelectedIndex = 0;
            _cmbStatusFilter.SelectedIndexChanged += (_, _) => RefreshGrid();
            ComboBoxStyler.MakeTaller(_cmbStatusFilter, 34, NavyDark);
            toolbar.Controls.Add(_cmbStatusFilter);

            _lblCount = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(576, 18)
            };
            toolbar.Controls.Add(_lblCount);

            var btnAdd = new FlatButton
            {
                Text = "＋ ADD RESIDENT",
                Size = new Size(180, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnAdd.Click += (_, _) => OpenAddDialog();
            toolbar.Controls.Add(btnAdd);
            toolbar.Resize += (_, _) =>
            {
                btnAdd.Location = new Point(toolbar.Width - btnAdd.Width, 8);
            };
            btnAdd.Location = new Point(toolbar.Width - btnAdd.Width, 8);

            root.Controls.Add(toolbar, 0, 0);

            // ===== Grid card =====
            var card = new RoundedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                CornerRadius = 14,
                BorderColor = BorderGray,
                Padding = new Padding(2)
            };
            root.Controls.Add(card, 0, 1);

            _grid = BuildGrid();
            card.Controls.Add(_grid);
        }

        private DataGridView BuildGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(240, 242, 245),
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 42,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                RowTemplate = { Height = 52 },
                Font = new Font("Segoe UI", 9.5f),
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor = NavySidebar;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 240, 245);
            grid.DefaultCellStyle.SelectionForeColor = NavyDark;
            grid.DefaultCellStyle.ForeColor = NavyDark;
            grid.DefaultCellStyle.Padding = new Padding(10, 0, 6, 0);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 252);

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColFullName,
                HeaderText = "Full Name",
                FillWeight = 190
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColAgeGender,
                HeaderText = "Age / Gender",
                FillWeight = 100
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColPurok,
                HeaderText = "Purok / Address",
                FillWeight = 130
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColContact,
                HeaderText = "Contact No.",
                FillWeight = 110
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColCivilStatus,
                HeaderText = "Civil Status",
                FillWeight = 100
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColStatus,
                HeaderText = "Status",
                FillWeight = 90
            });

            var viewCol = new DataGridViewButtonColumn
            {
                Name = ColView,
                HeaderText = "",
                Text = "View",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat,
                FillWeight = 60
            };
            viewCol.DefaultCellStyle.ForeColor = NavyDark;
            viewCol.DefaultCellStyle.BackColor = Color.White;
            viewCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            viewCol.DefaultCellStyle.SelectionBackColor = Color.White;
            viewCol.DefaultCellStyle.SelectionForeColor = NavyDark;
            grid.Columns.Add(viewCol);

            var editCol = new DataGridViewButtonColumn
            {
                Name = ColEdit,
                HeaderText = "",
                Text = "Edit",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat,
                FillWeight = 60
            };
            editCol.DefaultCellStyle.ForeColor = TealAccent;
            editCol.DefaultCellStyle.BackColor = Color.White;
            editCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            editCol.DefaultCellStyle.SelectionBackColor = Color.White;
            editCol.DefaultCellStyle.SelectionForeColor = TealAccent;
            grid.Columns.Add(editCol);

            var toggleCol = new DataGridViewButtonColumn
            {
                Name = ColToggle,
                HeaderText = "",
                UseColumnTextForButtonValue = false,
                FlatStyle = FlatStyle.Flat,
                FillWeight = 90
            };
            toggleCol.DefaultCellStyle.BackColor = Color.White;
            toggleCol.DefaultCellStyle.SelectionBackColor = Color.White;
            toggleCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.Columns.Add(toggleCol);

            var deleteCol = new DataGridViewButtonColumn
            {
                Name = ColDelete,
                HeaderText = "",
                Text = "Delete",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat,
                FillWeight = 70
            };
            deleteCol.DefaultCellStyle.ForeColor = AccentRed;
            deleteCol.DefaultCellStyle.BackColor = Color.White;
            deleteCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            deleteCol.DefaultCellStyle.SelectionBackColor = Color.White;
            deleteCol.DefaultCellStyle.SelectionForeColor = AccentRed;
            grid.Columns.Add(deleteCol);

            // Staff accounts can view/add/edit but not delete resident records.
            if (_currentUser.Role != UserRole.Administrator)
            {
                deleteCol.Visible = false;
            }

            grid.CellFormatting += Grid_CellFormatting;
            grid.CellPainting += Grid_CellPainting;
            grid.CellContentClick += Grid_CellContentClick;
            grid.CellDoubleClick += Grid_CellDoubleClick;

            return grid;
        }

        // ----- Data loading / filtering -----

        private void RefreshGrid()
        {
            string search = _txtSearch?.Text.Trim() ?? "";
            string purokFilter = _cmbPurokFilter?.SelectedItem as string ?? "All Puroks";
            string statusFilter = _cmbStatusFilter?.SelectedItem as string ?? "All Statuses";

            var residents = ResidentService.GetAllResidents()
                .Where(r =>
                    (string.IsNullOrEmpty(search) ||
                     r.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                     r.Purok.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                     r.ContactNo.Contains(search, StringComparison.OrdinalIgnoreCase)) &&
                    (purokFilter == "All Puroks" || r.Purok == purokFilter) &&
                    (statusFilter == "All Statuses" ||
                     (statusFilter == "Active" && r.IsActive) ||
                     (statusFilter == "Inactive" && !r.IsActive)))
                .ToList();

            _grid.Rows.Clear();
            foreach (var r in residents)
            {
                int rowIndex = _grid.Rows.Add(
                    r.FullName,
                    $"{r.Age} / {r.GenderLabel}",
                    string.IsNullOrWhiteSpace(r.Purok) ? "—" : r.Purok,
                    string.IsNullOrWhiteSpace(r.ContactNo) ? "—" : r.ContactNo,
                    r.CivilStatusLabel,
                    r.IsActive ? "Active" : "Inactive",
                    "View",
                    "Edit",
                    r.IsActive ? "Deactivate" : "Activate",
                    "Delete");
                _grid.Rows[rowIndex].Tag = r.ResidentId;
            }

            int total = ResidentService.GetAllResidents().Count;
            _lblCount.Text = residents.Count == total
                ? $"{total} resident(s) total"
                : $"Showing {residents.Count} of {total} resident(s)";
        }

        private Resident RowToResident(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _grid.Rows.Count) return null;
            int residentId = (int)_grid.Rows[rowIndex].Tag;
            return ResidentService.GetById(residentId);
        }

        // ----- Visuals: colored pill badges for Status column -----

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var col = _grid.Columns[e.ColumnIndex].Name;
            if (col == ColToggle)
            {
                var resident = RowToResident(e.RowIndex);
                if (resident == null) return;
                var cell = (DataGridViewButtonCell)_grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
                cell.Style.ForeColor = resident.IsActive ? AccentRed : SuccessGreen;
            }
        }

        private void Grid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var colName = _grid.Columns[e.ColumnIndex].Name;
            if (colName != ColStatus) return;

            e.PaintBackground(e.ClipBounds, true);

            string text = e.FormattedValue?.ToString() ?? "";
            bool isActive = text == "Active";
            Color pillBg = isActive ? SuccessGreenBg : InactiveGrayBg;
            Color pillText = isActive ? SuccessGreen : InactiveGrayText;

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var textSize = TextRenderer.MeasureText(text, e.CellStyle.Font);
            int pillWidth = textSize.Width + 24;
            int pillHeight = 24;
            var pillRect = new Rectangle(
                e.CellBounds.X + 10,
                e.CellBounds.Y + (e.CellBounds.Height - pillHeight) / 2,
                pillWidth,
                pillHeight);

            using var path = RoundHelper.RoundedRect(pillRect, pillHeight / 2);
            using var brush = new SolidBrush(pillBg);
            g.FillPath(brush, path);

            TextRenderer.DrawText(g, text, e.CellStyle.Font, pillRect, pillText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            e.Handled = true;
        }

        // ----- Actions -----

        private void Grid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var colName = _grid.Columns[e.ColumnIndex].Name;
            var resident = RowToResident(e.RowIndex);
            if (resident == null) return;

            switch (colName)
            {
                case ColView:
                    ShowProfile(resident);
                    break;
                case ColEdit:
                    OpenEditDialog(resident);
                    break;
                case ColToggle:
                    ToggleActive(resident);
                    break;
                case ColDelete:
                    DeleteResident(resident);
                    break;
            }
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var colName = _grid.Columns[e.ColumnIndex].Name;
            if (colName is ColView or ColEdit or ColToggle or ColDelete) return;

            var resident = RowToResident(e.RowIndex);
            if (resident != null) ShowProfile(resident);
        }

        private void OpenAddDialog()
        {
            using var dlg = ResidentFormDialog.CreateForAdd();
            if (dlg.ShowDialog(FindForm()) == DialogResult.OK && dlg.Result != null)
            {
                ResidentService.AddResident(dlg.Result);
                TransactionLogService.Log(LogType.Resident, "Registered resident",
                    _currentUser.FullName, _currentUser.UserId, $"{dlg.Result.FullName} added to Resident Records");
                RefreshGrid();
                MessageBox.Show($"\"{dlg.Result.FullName}\" was registered successfully.",
                    "Resident Added", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void OpenEditDialog(Resident resident)
        {
            using var dlg = ResidentFormDialog.CreateForEdit(resident);
            if (dlg.ShowDialog(FindForm()) == DialogResult.OK && dlg.Result != null)
            {
                ResidentService.UpdateResident(dlg.Result);
                TransactionLogService.Log(LogType.Resident, "Updated resident",
                    _currentUser.FullName, _currentUser.UserId, $"{dlg.Result.FullName} profile updated");
                RefreshGrid();
            }
        }

        private void ShowProfile(Resident resident)
        {
            using var dlg = new ResidentProfileDialog(resident);
            dlg.ShowDialog(FindForm());
        }

        private void ToggleActive(Resident resident)
        {
            bool newState = !resident.IsActive;
            string action = newState ? "reactivate" : "deactivate";

            if (newState)
            {
                var confirm = MessageBox.Show(
                    $"Are you sure you want to {action} the record for \"{resident.FullName}\"?",
                    "Confirm Action", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);

                if (confirm == DialogResult.Yes)
                {
                    ResidentService.SetActive(resident.ResidentId, true);
                    TransactionLogService.Log(LogType.Resident, "Reactivated resident",
                        _currentUser.FullName, _currentUser.UserId, $"{resident.FullName} marked active again");
                    RefreshGrid();
                }
                return;
            }

            using var promptDialog = new ReasonPromptDialog(
                "Deactivate Resident",
                $"Deactivating \"{resident.FullName}\" marks them as no longer residing here (e.g. moved out or deceased) without deleting their historical record.\n\nReason (optional):");

            if (promptDialog.ShowDialog(FindForm()) == DialogResult.OK)
            {
                ResidentService.SetActive(resident.ResidentId, false, promptDialog.ReasonText);
                TransactionLogService.Log(LogType.Resident, "Deactivated resident",
                    _currentUser.FullName, _currentUser.UserId, $"{resident.FullName} deactivated" +
                    (string.IsNullOrWhiteSpace(promptDialog.ReasonText) ? "" : $" — {promptDialog.ReasonText}"));
                RefreshGrid();
            }
        }

        private void DeleteResident(Resident resident)
        {
            var confirm = MessageBox.Show(
                $"This will permanently delete the record for \"{resident.FullName}\".\n\n" +
                "This action cannot be undone. Continue?",
                "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirm == DialogResult.Yes)
            {
                ResidentService.DeleteResident(resident.ResidentId);
                RefreshGrid();
            }
        }

        /// <summary>
        /// Small inline confirm+reason dialog, used only for deactivating a
        /// resident record (moved out / deceased) so a reason can optionally
        /// be captured without adding a whole new form file for one field.
        /// </summary>
        private class ReasonPromptDialog : Form
        {
            private readonly TextBox _txtReason;
            public string ReasonText => _txtReason.Text.Trim();

            public ReasonPromptDialog(string title, string message)
            {
                Text = title;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                StartPosition = FormStartPosition.CenterParent;
                BackColor = Color.White;
                Font = new Font("Segoe UI", 9.5f);
                ClientSize = new Size(380, 220);

                var lblMessage = new Label
                {
                    Text = message,
                    AutoSize = false,
                    Size = new Size(336, 100),
                    Location = new Point(22, 18),
                    ForeColor = NavyDark
                };
                Controls.Add(lblMessage);

                _txtReason = new TextBox
                {
                    Location = new Point(22, 122),
                    Width = 336,
                    Font = new Font("Segoe UI", 9.5f)
                };
                Controls.Add(_txtReason);

                var btnCancel = new FlatButton
                {
                    Text = "CANCEL",
                    Size = new Size(160, 38),
                    Location = new Point(22, 164),
                    NormalColor = Color.FromArgb(200, 29, 37),
                    HoverColor = Color.FromArgb(200, 29, 37),
                    ForeColor = Color.White,
                    DialogResult = DialogResult.Cancel
                };
                Controls.Add(btnCancel);

                var btnConfirm = new FlatButton
                {
                    Text = "DEACTIVATE",
                    Size = new Size(160, 38),
                    Location = new Point(198, 164),
                    DialogResult = DialogResult.OK
                };
                Controls.Add(btnConfirm);

                AcceptButton = btnConfirm;
                CancelButton = btnCancel;
            }
        }
    }
}
