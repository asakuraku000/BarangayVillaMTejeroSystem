using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BarangayVillaMTejeroSystem.Models;
using BarangayVillaMTejeroSystem.Services;
using BarangayVillaMTejeroSystem.UI;

namespace BarangayVillaMTejeroSystem.Controls
{
    /// <summary>
    /// Full "Transaction Logs" / audit-trail module page:
    ///  • Lists every system action (logins, resident & document changes, user
    ///    management) with actor, category, timestamp, and details.
    ///  • Filterable by free-text search, category, and date range.
    ///  • Exportable to CSV for record-keeping / reporting.
    /// Read-only: logs are an immutable audit trail (no edit/delete).
    /// </summary>
    public class TransactionLogsControl : Panel
    {
        private static readonly Color NavyDark = Color.FromArgb(16, 37, 66);
        private static readonly Color BgLight = Color.FromArgb(244, 246, 249);
        private static readonly Color TealAccent = Color.FromArgb(27, 90, 130);
        private static readonly Color BorderGray = Color.FromArgb(232, 235, 240);
        private static readonly Color MutedText = Color.FromArgb(140, 148, 160);
        private static readonly Color FieldBg = Color.White;

        private const string ColTime = "colTime";
        private const string ColActor = "colActor";
        private const string ColType = "colType";
        private const string ColAction = "colAction";
        private const string ColDetails = "colDetails";

        private readonly UserAccount _currentUser;

        private DataGridView _grid;
        private TextBox _txtSearch;
        private ComboBox _cmbTypeFilter;
        private CheckBox _chkDateRange;
        private DateTimePicker _dtpFrom;
        private DateTimePicker _dtpTo;
        private Label _lblCount;

        public TransactionLogsControl(UserAccount currentUser)
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
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            Controls.Add(root);

            // ----- Filter toolbar card -----
            var filterCard = new RoundedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                CornerRadius = 14,
                BorderColor = BorderGray,
                Padding = new Padding(18, 14, 18, 14)
            };
            filterCard.Controls.Add(new Label
            {
                Text = "Transaction Logs",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = NavyDark,
                AutoSize = true,
                Location = new Point(18, 12)
            });
            filterCard.Controls.Add(new Label
            {
                Text = "Complete audit trail of system activity. Read-only.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(18, 38)
            });

            // Search
            var searchBox = new Panel
            {
                Location = new Point(18, 64),
                Size = new Size(230, 36),
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
                ForeColor = MutedText,
                AutoSize = false,
                Size = new Size(22, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(8, 8)
            };
            searchBox.Controls.Add(lblSearchIcon);
            _txtSearch = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(36, 8),
                Width = searchBox.Width - 36 - 10
            };
            _txtSearch.PlaceholderText = "Search action / actor / details...";
            _txtSearch.TextChanged += (_, _) => RefreshGrid();
            searchBox.Controls.Add(_txtSearch);
            filterCard.Controls.Add(searchBox);

            // Type filter
            _cmbTypeFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(258, 64),
                Width = 160,
                FlatStyle = FlatStyle.Flat
            };
            _cmbTypeFilter.Items.Add("All Categories");
            foreach (LogType t in Enum.GetValues(typeof(LogType)))
                _cmbTypeFilter.Items.Add(t.Label());
            _cmbTypeFilter.SelectedIndex = 0;
            _cmbTypeFilter.SelectedIndexChanged += (_, _) => RefreshGrid();
            ComboBoxStyler.MakeTaller(_cmbTypeFilter, 34, NavyDark);
            filterCard.Controls.Add(_cmbTypeFilter);

            // Date range toggle + pickers
            _chkDateRange = new CheckBox
            {
                Text = "Date range",
                Font = new Font("Segoe UI", 9f),
                ForeColor = NavyDark,
                AutoSize = true,
                Location = new Point(430, 68)
            };
            _chkDateRange.CheckedChanged += (_, _) =>
            {
                _dtpFrom.Enabled = _dtpTo.Enabled = _chkDateRange.Checked;
                RefreshGrid();
            };
            filterCard.Controls.Add(_chkDateRange);

            _dtpFrom = new DateTimePicker
            {
                Location = new Point(540, 64),
                Width = 130,
                Font = new Font("Segoe UI", 9.5f),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(-30),
                Enabled = false
            };
            _dtpFrom.ValueChanged += (_, _) => RefreshGrid();
            filterCard.Controls.Add(_dtpFrom);

            var lblTo = new Label
            {
                Text = "to",
                Font = new Font("Segoe UI", 9f),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(678, 70)
            };
            filterCard.Controls.Add(lblTo);

            _dtpTo = new DateTimePicker
            {
                Location = new Point(704, 64),
                Width = 130,
                Font = new Font("Segoe UI", 9.5f),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Enabled = false
            };
            _dtpTo.ValueChanged += (_, _) => RefreshGrid();
            filterCard.Controls.Add(_dtpTo);

            // Export CSV (right-aligned)
            var btnExport = new FlatButton
            {
                Text = "⬇ EXPORT CSV",
                Size = new Size(150, 36),
                NormalColor = TealAccent,
                HoverColor = Color.FromArgb(22, 75, 110),
                Location = new Point(filterCard.Width - 168, 62)
            };
            btnExport.Click += BtnExport_Click;
            filterCard.Controls.Add(btnExport);
            filterCard.Resize += (_, _) => btnExport.Location = new Point(filterCard.Width - 168, 62);

            root.Controls.Add(filterCard, 0, 0);

            // ----- Grid card -----
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
                RowCount = 2,
                BackColor = Color.White
            };
            gridHost.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            gridHost.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
            card.Controls.Add(gridHost);

            _grid = BuildGrid();
            gridHost.Controls.Add(_grid, 0, 0);

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
            gridHost.Controls.Add(_lblCount, 0, 1);
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
                RowTemplate = { Height = 50 },
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

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = ColTime, HeaderText = "Timestamp", FillWeight = 130 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = ColActor, HeaderText = "Actor", FillWeight = 130 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = ColType, HeaderText = "Category", FillWeight = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = ColAction, HeaderText = "Action", FillWeight = 160 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = ColDetails, HeaderText = "Details", FillWeight = 200 });

            grid.CellFormatting += Grid_CellFormatting;
            grid.CellPainting += Grid_CellPainting;

            return grid;
        }

        // ----- Data loading -----

        private void RefreshGrid()
        {
            string search = _txtSearch?.Text.Trim() ?? "";
            var typeFilter = _cmbTypeFilter?.SelectedIndex > 0
                ? (LogType?)(_cmbTypeFilter.SelectedIndex - 1) : null;
            DateTime? from = (_chkDateRange?.Checked == true) ? _dtpFrom.Value.Date : null;
            DateTime? to = (_chkDateRange?.Checked == true) ? _dtpTo.Value.Date : null;

            var logs = TransactionLogService.Search(search, typeFilter, from, to).ToList();

            _grid.Rows.Clear();
            foreach (var l in logs)
            {
                int row = _grid.Rows.Add(
                    l.TimestampLabel,
                    string.IsNullOrWhiteSpace(l.Actor) ? "System" : l.Actor,
                    l.Type.Label(),
                    l.Action,
                    l.Details);
                _grid.Rows[row].Tag = l.LogId;
            }

            int total = TransactionLogService.Total;
            _lblCount.Text = logs.Count == total
                ? $"{total} log entr{(total == 1 ? "y" : "ies")} total"
                : $"Showing {logs.Count} of {total} log entries";
        }

        // ----- Visuals: colored category pills -----

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_grid.Columns[e.ColumnIndex].Name != ColType) return;
            var text = e.Value?.ToString() ?? "";
            if (Enum.TryParse<LogType>(text, out var t) || (text != null && TryParseLabel(text, out t)))
            {
                e.CellStyle.ForeColor = t.Color();
                e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            }
        }

        private static bool TryParseLabel(string label, out LogType type)
        {
            foreach (LogType t in Enum.GetValues(typeof(LogType)))
            {
                if (t.Label().Equals(label, StringComparison.OrdinalIgnoreCase))
                {
                    type = t;
                    return true;
                }
            }
            type = LogType.System;
            return false;
        }

        private void Grid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (_grid.Columns[e.ColumnIndex].Name != ColType) return;

            e.PaintBackground(e.ClipBounds, true);
            string label = e.FormattedValue?.ToString() ?? "";
            if (!TryParseLabel(label, out var type)) return;

            Color pillBg = type.BackColor();
            Color pillText = type.Color();

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var textSize = TextRenderer.MeasureText(label, e.CellStyle.Font);
            int pillWidth = textSize.Width + 22;
            int pillHeight = 22;
            var pillRect = new Rectangle(
                e.CellBounds.X + 8,
                e.CellBounds.Y + (e.CellBounds.Height - pillHeight) / 2,
                pillWidth, pillHeight);

            using var path = RoundHelper.RoundedRect(pillRect, pillHeight / 2);
            using var brush = new SolidBrush(pillBg);
            g.FillPath(brush, path);
            TextRenderer.DrawText(g, label, e.CellStyle.Font, pillRect, pillText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            e.Handled = true;
        }

        // ----- Export -----

        private void BtnExport_Click(object sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = $"TransactionLogs_{DateTime.Now:yyyyMMdd}.csv",
                Title = "Export Transaction Logs"
            };

            if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

            string search = _txtSearch?.Text.Trim() ?? "";
            var typeFilter = _cmbTypeFilter?.SelectedIndex > 0
                ? (LogType?)(_cmbTypeFilter.SelectedIndex - 1) : null;
            DateTime? from = (_chkDateRange?.Checked == true) ? _dtpFrom.Value.Date : null;
            DateTime? to = (_chkDateRange?.Checked == true) ? _dtpTo.Value.Date : null;

            var logs = TransactionLogService.Search(search, typeFilter, from, to);
            try
            {
                TransactionLogService.ExportCsv(logs, dlg.FileName);
                MessageBox.Show($"Exported {logs.Count} log entr{(logs.Count == 1 ? "y" : "ies")} to:\n{dlg.FileName}",
                    "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not export the file:\n{ex.Message}", "Export Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
