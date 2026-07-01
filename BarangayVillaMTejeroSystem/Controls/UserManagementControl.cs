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
    /// Full User Management module: lists all registered accounts with search
    /// and role filtering, and lets an Administrator add, edit, activate/
    /// deactivate, or permanently delete Administrator and Staff accounts.
    /// </summary>
    public class UserManagementControl : Panel
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
        private const string ColUsername = "colUsername";
        private const string ColPosition = "colPosition";
        private const string ColContact = "colContact";
        private const string ColRole = "colRole";
        private const string ColStatus = "colStatus";
        private const string ColEdit = "colEdit";
        private const string ColToggle = "colToggle";
        private const string ColDelete = "colDelete";

        private readonly UserAccount _currentUser;
        private DataGridView _grid;
        private TextBox _txtSearch;
        private ComboBox _cmbRoleFilter;
        private Label _lblCount;

        public UserManagementControl(UserAccount currentUser)
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

            // ===== Toolbar: search + role filter (left), Add User button (right) =====
            var toolbar = new Panel { Dock = DockStyle.Fill, BackColor = BgLight };

            var searchBox = new Panel
            {
                Location = new Point(0, 8),
                Size = new Size(260, 38),
                BackColor = Color.White
            };
            searchBox.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderGray);
                e.Graphics.DrawRectangle(pen, 0, 0, searchBox.Width - 1, searchBox.Height - 1);
            };
            var lblSearchIcon = new Label
            {
                Text = "🔎",
                Font = new Font("Segoe UI Emoji", 9.5f),
                AutoSize = true,
                Location = new Point(10, 10)
            };
            searchBox.Controls.Add(lblSearchIcon);
            _txtSearch = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(34, 9),
                Width = 216
            };
            _txtSearch.TextChanged += (_, _) => RefreshGrid();
            searchBox.Controls.Add(_txtSearch);
            toolbar.Controls.Add(searchBox);

            _cmbRoleFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(272, 8),
                Width = 170,
                FlatStyle = FlatStyle.Flat
            };
            _cmbRoleFilter.Items.Add("All Roles");
            _cmbRoleFilter.Items.Add("Administrator");
            _cmbRoleFilter.Items.Add("Staff");
            _cmbRoleFilter.SelectedIndex = 0;
            _cmbRoleFilter.SelectedIndexChanged += (_, _) => RefreshGrid();
            toolbar.Controls.Add(_cmbRoleFilter);

            _lblCount = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(452, 18)
            };
            toolbar.Controls.Add(_lblCount);

            var btnAdd = new FlatButton
            {
                Text = "＋ ADD NEW USER",
                Size = new Size(190, 40),
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
                FillWeight = 170,
                DataPropertyName = "FullName"
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColUsername,
                HeaderText = "Username",
                FillWeight = 110
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColPosition,
                HeaderText = "Position",
                FillWeight = 160
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColContact,
                HeaderText = "Contact No.",
                FillWeight = 120
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColRole,
                HeaderText = "Role",
                FillWeight = 110
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColStatus,
                HeaderText = "Status",
                FillWeight = 90
            });

            var editCol = new DataGridViewButtonColumn
            {
                Name = ColEdit,
                HeaderText = "",
                Text = "Edit",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat,
                FillWeight = 70
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
                FillWeight = 80
            };
            deleteCol.DefaultCellStyle.ForeColor = AccentRed;
            deleteCol.DefaultCellStyle.BackColor = Color.White;
            deleteCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            deleteCol.DefaultCellStyle.SelectionBackColor = Color.White;
            deleteCol.DefaultCellStyle.SelectionForeColor = AccentRed;
            grid.Columns.Add(deleteCol);

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
            string roleFilter = _cmbRoleFilter?.SelectedItem as string ?? "All Roles";

            var accounts = UserService.GetAllAccounts()
                .Where(a =>
                    (string.IsNullOrEmpty(search) ||
                     a.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                     a.Username.Contains(search, StringComparison.OrdinalIgnoreCase)) &&
                    (roleFilter == "All Roles" ||
                     (roleFilter == "Administrator" && a.Role == UserRole.Administrator) ||
                     (roleFilter == "Staff" && a.Role == UserRole.Staff)))
                .ToList();

            _grid.Rows.Clear();
            foreach (var a in accounts)
            {
                int rowIndex = _grid.Rows.Add(
                    a.FullName,
                    a.Username,
                    string.IsNullOrWhiteSpace(a.Position) ? "—" : a.Position,
                    string.IsNullOrWhiteSpace(a.ContactNo) ? "—" : a.ContactNo,
                    a.Role == UserRole.Administrator ? "Administrator" : "Staff",
                    a.IsActive ? "Active" : "Inactive",
                    "Edit",
                    a.IsActive ? "Deactivate" : "Activate",
                    "Delete");
                _grid.Rows[rowIndex].Tag = a.UserId;
            }

            int total = UserService.GetAllAccounts().Count;
            _lblCount.Text = accounts.Count == total
                ? $"{total} account(s) total"
                : $"Showing {accounts.Count} of {total} account(s)";
        }

        private UserAccount RowToAccount(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _grid.Rows.Count) return null;
            int userId = (int)_grid.Rows[rowIndex].Tag;
            return UserService.GetById(userId);
        }

        // ----- Visuals: colored pill badges for Role / Status columns -----

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var col = _grid.Columns[e.ColumnIndex].Name;
            if (col == ColToggle)
            {
                var account = RowToAccount(e.RowIndex);
                if (account == null) return;
                var cell = (DataGridViewButtonCell)_grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
                cell.Style.ForeColor = account.IsActive ? AccentRed : SuccessGreen;
            }
        }

        private void Grid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var colName = _grid.Columns[e.ColumnIndex].Name;
            if (colName != ColRole && colName != ColStatus) return;

            e.PaintBackground(e.ClipBounds, true);

            string text = e.FormattedValue?.ToString() ?? "";
            Color pillBg, pillText;

            if (colName == ColRole)
            {
                bool isAdmin = text == "Administrator";
                pillBg = isAdmin ? Color.FromArgb(16, 37, 66) : Color.FromArgb(27, 90, 130);
                pillText = Color.White;
            }
            else
            {
                bool isActive = text == "Active";
                pillBg = isActive ? SuccessGreenBg : InactiveGrayBg;
                pillText = isActive ? SuccessGreen : InactiveGrayText;
            }

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
            var account = RowToAccount(e.RowIndex);
            if (account == null) return;

            switch (colName)
            {
                case ColEdit:
                    OpenEditDialog(account);
                    break;
                case ColToggle:
                    ToggleActive(account);
                    break;
                case ColDelete:
                    DeleteAccount(account);
                    break;
            }
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var colName = _grid.Columns[e.ColumnIndex].Name;
            // Ignore double-click on the action button columns themselves.
            if (colName is ColEdit or ColToggle or ColDelete) return;

            var account = RowToAccount(e.RowIndex);
            if (account != null) OpenEditDialog(account);
        }

        private void OpenAddDialog()
        {
            using var dlg = UserFormDialog.CreateForAdd();
            if (dlg.ShowDialog(FindForm()) == DialogResult.OK && dlg.Result != null)
            {
                UserService.AddAccount(dlg.Result);
                RefreshGrid();
                MessageBox.Show($"Account for \"{dlg.Result.FullName}\" was created successfully.",
                    "User Added", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void OpenEditDialog(UserAccount account)
        {
            using var dlg = UserFormDialog.CreateForEdit(account);
            if (dlg.ShowDialog(FindForm()) == DialogResult.OK && dlg.Result != null)
            {
                UserService.UpdateAccount(dlg.Result);
                RefreshGrid();
            }
        }

        private void ToggleActive(UserAccount account)
        {
            if (account.UserId == _currentUser.UserId)
            {
                MessageBox.Show("You cannot deactivate your own account while logged in.",
                    "Action Not Allowed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool newState = !account.IsActive;
            string action = newState ? "reactivate" : "deactivate";
            var confirm = MessageBox.Show(
                $"Are you sure you want to {action} the account \"{account.FullName}\"?",
                "Confirm Action", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (confirm == DialogResult.Yes)
            {
                UserService.SetActive(account.UserId, newState);
                RefreshGrid();
            }
        }

        private void DeleteAccount(UserAccount account)
        {
            if (account.UserId == _currentUser.UserId)
            {
                MessageBox.Show("You cannot delete your own account while logged in.",
                    "Action Not Allowed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"This will permanently delete the account \"{account.FullName}\" ({account.Username}).\n\n" +
                "This action cannot be undone. Continue?",
                "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirm == DialogResult.Yes)
            {
                UserService.DeleteAccount(account.UserId);
                RefreshGrid();
            }
        }
    }
}
