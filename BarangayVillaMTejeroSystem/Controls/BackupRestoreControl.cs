using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using BarangayVillaMTejeroSystem.Models;
using BarangayVillaMTejeroSystem.Services;
using BarangayVillaMTejeroSystem.UI;

namespace BarangayVillaMTejeroSystem.Controls
{
    /// <summary>
    /// Full "Backup && Restore" module page (Administrator only):
    ///  • Shows the live database's location, size, and last-modified time.
    ///  • Creates on-demand backups into Data\Backups, and lists them.
    ///  • Restores the live database from a selected backup, or from any
    ///    .db file browsed from elsewhere (e.g. a USB drive).
    ///  • Exports a backup file to another location, or deletes old ones.
    /// Every backup/restore is written to the Transaction Logs audit trail.
    /// </summary>
    public class BackupRestoreControl : Panel
    {
        private static readonly Color NavyDark = Color.FromArgb(16, 37, 66);
        private static readonly Color BgLight = Color.FromArgb(244, 246, 249);
        private static readonly Color TealAccent = Color.FromArgb(27, 90, 130);
        private static readonly Color AccentRed = Color.FromArgb(200, 29, 37);
        private static readonly Color BorderGray = Color.FromArgb(232, 235, 240);
        private static readonly Color MutedText = Color.FromArgb(140, 148, 160);

        private const string ColFile = "colFile";
        private const string ColCreated = "colCreated";
        private const string ColSize = "colSize";

        private readonly UserAccount _currentUser;

        private DataGridView _grid;
        private Label _lblDbPath;
        private Label _lblDbMeta;
        private Label _lblCount;
        private FlatButton _btnRestore;
        private FlatButton _btnExport;
        private FlatButton _btnDelete;

        public BackupRestoreControl(UserAccount currentUser)
        {
            _currentUser = currentUser;
            Dock = DockStyle.Fill;
            BackColor = BgLight;
            Padding = new Padding(28, 20, 28, 24);

            BuildUi();
            RefreshAll();
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
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            Controls.Add(root);

            // ----- Database status / create-backup card -----
            var infoCard = new RoundedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                CornerRadius = 14,
                BorderColor = BorderGray,
                Padding = new Padding(18, 14, 18, 14)
            };
            infoCard.Controls.Add(new Label
            {
                Text = "Backup && Restore",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = NavyDark,
                AutoSize = true,
                Location = new Point(18, 12)
            });
            infoCard.Controls.Add(new Label
            {
                Text = "Protect barangay records with manual backup and restore tools.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(18, 38)
            });

            var dbBadge = new Panel
            {
                Size = new Size(40, 40),
                Location = new Point(18, 68),
                BackColor = Color.White
            };
            dbBadge.Paint += (s, e) =>
            {
                using var path = RoundHelper.RoundedRect(new Rectangle(0, 0, dbBadge.Width - 1, dbBadge.Height - 1), 10);
                using var brush = new SolidBrush(Color.FromArgb(35, TealAccent.R, TealAccent.G, TealAccent.B));
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.FillPath(brush, path);
            };
            dbBadge.Controls.Add(new Label
            {
                Text = "🗄",
                Font = new Font("Segoe UI Emoji", 14f),
                ForeColor = TealAccent,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            });
            infoCard.Controls.Add(dbBadge);

            _lblDbPath = new Label
            {
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = NavyDark,
                AutoSize = true,
                Location = new Point(68, 70)
            };
            infoCard.Controls.Add(_lblDbPath);

            _lblDbMeta = new Label
            {
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(68, 90)
            };
            infoCard.Controls.Add(_lblDbMeta);

            var btnCreate = new FlatButton
            {
                Text = "💾 CREATE BACKUP NOW",
                Size = new Size(210, 40),
                NormalColor = TealAccent,
                HoverColor = Color.FromArgb(22, 75, 110),
                Location = new Point(infoCard.Width - 228, 60)
            };
            btnCreate.Click += BtnCreate_Click;
            infoCard.Controls.Add(btnCreate);
            infoCard.Resize += (_, _) => btnCreate.Location = new Point(infoCard.Width - 228, 60);

            root.Controls.Add(infoCard, 0, 0);

            // ----- Backup list card -----
            var card = new RoundedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                CornerRadius = 14,
                BorderColor = BorderGray,
                Padding = new Padding(2)
            };
            root.Controls.Add(card, 0, 1);

            var gridHost = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.White
            };
            gridHost.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
            gridHost.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            gridHost.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
            card.Controls.Add(gridHost);

            // Toolbar
            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(14, 8, 14, 8),
                BackColor = Color.White
            };

            var btnRefresh = MakeToolbarButton("⟳ Refresh", TealAccent);
            btnRefresh.Click += (_, _) => RefreshAll();
            toolbar.Controls.Add(btnRefresh);

            _btnRestore = MakeToolbarButton("♻ Restore Selected", Color.FromArgb(60, 130, 90));
            _btnRestore.Click += BtnRestoreSelected_Click;
            toolbar.Controls.Add(_btnRestore);

            var btnImport = MakeToolbarButton("📥 Import && Restore...", Color.FromArgb(214, 158, 46));
            btnImport.Click += BtnImportAndRestore_Click;
            toolbar.Controls.Add(btnImport);

            _btnExport = MakeToolbarButton("📤 Export Copy...", TealAccent);
            _btnExport.Click += BtnExportSelected_Click;
            toolbar.Controls.Add(_btnExport);

            _btnDelete = MakeToolbarButton("🗑 Delete Selected", AccentRed);
            _btnDelete.Click += BtnDeleteSelected_Click;
            toolbar.Controls.Add(_btnDelete);

            gridHost.Controls.Add(toolbar, 0, 0);

            _grid = BuildGrid();
            gridHost.Controls.Add(_grid, 0, 1);

            _lblCount = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = MutedText,
                AutoSize = true,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };
            gridHost.Controls.Add(_lblCount, 0, 2);

            UpdateButtonStates();
        }

        private FlatButton MakeToolbarButton(string text, Color color)
        {
            return new FlatButton
            {
                Text = text,
                Size = new Size(text.Length * 9 + 30, 34),
                NormalColor = color,
                HoverColor = ControlPaint.Dark(color, 0.1f),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Margin = new Padding(0, 0, 10, 0)
            };
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
                ColumnHeadersHeight = 42,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                RowTemplate = { Height = 46 },
                Font = new Font("Segoe UI", 9.5f),
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor = NavyDark;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 240, 245);
            grid.DefaultCellStyle.SelectionForeColor = NavyDark;
            grid.DefaultCellStyle.ForeColor = NavyDark;
            grid.DefaultCellStyle.Padding = new Padding(10, 0, 6, 0);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 252);

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = ColFile, HeaderText = "Backup File", FillWeight = 220 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = ColCreated, HeaderText = "Date Created", FillWeight = 150 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = ColSize, HeaderText = "Size", FillWeight = 90 });

            grid.SelectionChanged += (_, _) => UpdateButtonStates();

            return grid;
        }

        // ----- Data loading -----

        private void RefreshAll()
        {
            var info = BackupService.GetCurrentDatabaseInfo();
            _lblDbPath.Text = info.Path;
            _lblDbMeta.Text = $"Current size: {FormatSize(info.SizeBytes)}   •   Last modified: {info.LastModified:MMM d, yyyy  h:mm tt}";

            var backups = BackupService.GetBackups();
            _grid.Rows.Clear();
            foreach (var b in backups)
            {
                int row = _grid.Rows.Add(b.FileName, b.CreatedLabel, b.SizeLabel);
                _grid.Rows[row].Tag = b.FullPath;
            }

            _lblCount.Text = backups.Count == 0
                ? "No backups yet. Click \"Create Backup Now\" to make one."
                : $"{backups.Count} backup file{(backups.Count == 1 ? "" : "s")} in Data\\Backups";

            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            bool hasSelection = _grid.SelectedRows.Count > 0;
            if (_btnRestore != null) _btnRestore.Enabled = hasSelection;
            if (_btnExport != null) _btnExport.Enabled = hasSelection;
            if (_btnDelete != null) _btnDelete.Enabled = hasSelection;
        }

        private string SelectedBackupPath =>
            _grid.SelectedRows.Count > 0 ? (string)_grid.SelectedRows[0].Tag : null;

        private static string FormatSize(long bytes) => bytes < 1024 * 1024
            ? $"{bytes / 1024.0:0.0} KB"
            : $"{bytes / (1024.0 * 1024.0):0.00} MB";

        // ----- Actions -----

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            try
            {
                string path = BackupService.CreateBackup();
                TransactionLogService.Log(LogType.System, "Created system backup", _currentUser.FullName,
                    _currentUser.UserId, $"Backup file: {Path.GetFileName(path)}");
                RefreshAll();
                MessageBox.Show($"Backup created:\n{path}", "Backup Complete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not create the backup:\n{ex.Message}", "Backup Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRestoreSelected_Click(object sender, EventArgs e)
        {
            var path = SelectedBackupPath;
            if (path == null) return;
            RestoreFrom(path);
        }

        private void BtnImportAndRestore_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Filter = "Database backup files (*.db)|*.db|All files (*.*)|*.*",
                Title = "Select a Backup File to Restore"
            };
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
            RestoreFrom(dlg.FileName);
        }

        private void RestoreFrom(string backupPath)
        {
            string fileName = Path.GetFileName(backupPath);
            var confirm = MessageBox.Show(
                $"This will replace ALL current data with the contents of:\n\n{fileName}\n\n" +
                "A snapshot of the current data will be saved automatically before restoring, " +
                "but this action cannot be undone from within the app. Continue?",
                "Confirm Restore",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes) return;

            try
            {
                string preRestoreFile = BackupService.RestoreFromFile(backupPath);
                TransactionLogService.Log(LogType.System, "Restored system backup", _currentUser.FullName,
                    _currentUser.UserId, $"Restored from '{fileName}'. Pre-restore snapshot saved as '{preRestoreFile}'.");

                var restart = MessageBox.Show(
                    "Restore complete. The application needs to restart to load the restored data.\n\nRestart now?",
                    "Restore Complete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (restart == DialogResult.Yes)
                {
                    Process.Start(Application.ExecutablePath);
                    Environment.Exit(0);
                }
                else
                {
                    RefreshAll();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not restore from that file:\n{ex.Message}", "Restore Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExportSelected_Click(object sender, EventArgs e)
        {
            var path = SelectedBackupPath;
            if (path == null) return;

            using var dlg = new SaveFileDialog
            {
                Filter = "Database backup files (*.db)|*.db",
                FileName = Path.GetFileName(path),
                Title = "Export Backup Copy"
            };
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

            try
            {
                BackupService.ExportTo(path, dlg.FileName);
                MessageBox.Show($"Backup copied to:\n{dlg.FileName}", "Export Complete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not export the file:\n{ex.Message}", "Export Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDeleteSelected_Click(object sender, EventArgs e)
        {
            var path = SelectedBackupPath;
            if (path == null) return;
            string fileName = Path.GetFileName(path);

            var confirm = MessageBox.Show(
                $"Delete backup file '{fileName}'? This cannot be undone.",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes) return;

            try
            {
                BackupService.DeleteBackup(path);
                TransactionLogService.Log(LogType.System, "Deleted system backup", _currentUser.FullName,
                    _currentUser.UserId, $"Backup file: {fileName}");
                RefreshAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not delete the file:\n{ex.Message}", "Delete Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
