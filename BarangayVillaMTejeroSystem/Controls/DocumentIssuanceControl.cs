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
    /// Full "Barangay Documents & Clearance Issuance" module page:
    ///  • Generates the five required certificate / clearance types.
    ///  • Auto-populates resident details from the resident record (no re-encoding).
    ///  • Enforces a verification step (residency + documentary requirements) before approval.
    ///  • Marks each request Approved / Pending / Rejected with remarks.
    ///  • Saves every generated document to the database and supports printing.
    ///  • Shows a filterable History of issued documents per resident / per type.
    /// </summary>
    public class DocumentIssuanceControl : Panel
    {
        private static readonly Color NavyDark = Color.FromArgb(16, 37, 66);
        private static readonly Color BgLight = Color.FromArgb(244, 246, 249);
        private static readonly Color AccentRed = Color.FromArgb(200, 29, 37);
        private static readonly Color TealAccent = Color.FromArgb(27, 90, 130);
        private static readonly Color BorderGray = Color.FromArgb(232, 235, 240);
        private static readonly Color MutedText = Color.FromArgb(140, 148, 160);
        private static readonly Color FieldBg = Color.White;

        private const string ColControl = "colControl";
        private const string ColResident = "colResident";
        private const string ColType = "colType";
        private const string ColDate = "colDate";
        private const string ColPurpose = "colPurpose";
        private const string ColStatus = "colStatus";
        private const string ColOpen = "colOpen";
        private const string ColPrint = "colPrint";

        private readonly UserAccount _currentUser;
        private readonly string _captainName;

        // ----- Issuance form controls -----
        private ComboBox _cmbResident;
        private Panel _residentSummary;
        private ComboBox _cmbDocType;
        private TextBox _txtPurpose;
        private CheckBox _chkResidency;
        private CheckedListBox _clbRequirements;
        private TextBox _txtOrNo;
        private TextBox _txtFee;
        private RadioButton _rdoPending;
        private RadioButton _rdoApproved;
        private RadioButton _rdoRejected;
        private TextBox _txtRemarks;
        private Label _lblControlNo;
        private Label _lblFormTitle;

        private BarangayDocument _activeDoc;   // loaded/being-edited document (null = new)
        private Resident _selectedResident;

        // ----- History controls -----
        private DataGridView _grid;
        private TextBox _txtSearch;
        private ComboBox _cmbTypeFilter;
        private ComboBox _cmbStatusFilter;
        private Label _lblHistoryCount;

        public DocumentIssuanceControl(UserAccount currentUser)
        {
            _currentUser = currentUser;
            _captainName = Services.UserService.GetAllAccounts()
                .FirstOrDefault(a => a.Role == UserRole.Administrator)?.FullName ?? "Barangay Captain";

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
                ColumnCount = 2,
                RowCount = 1,
                BackColor = BgLight
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 470f));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            Controls.Add(root);

            root.Controls.Add(BuildIssuanceCard(), 0, 0);
            root.Controls.Add(BuildHistoryCard(), 1, 0);
        }

        // ===================================================================
        //  LEFT: Issuance form
        // ===================================================================

        private Control BuildIssuanceCard()
        {
            var card = new RoundedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                CornerRadius = 14,
                BorderColor = BorderGray,
                Padding = new Padding(0)
            };

            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(22, 18, 22, 18)
            };
            card.Controls.Add(scroll);

            int y = 0;
            int w = 410;

            _lblFormTitle = new Label
            {
                Text = "New Document Request",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = NavyDark,
                AutoSize = true,
                Location = new Point(0, y)
            };
            scroll.Controls.Add(_lblFormTitle);
            y += 30;

            _lblControlNo = new Label
            {
                Text = "Draft — not yet saved",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(0, y)
            };
            scroll.Controls.Add(_lblControlNo);
            y += 22;

            // ----- Resident selector -----
            scroll.Controls.Add(new FieldLabel("Resident", 0, y));
            _cmbResident = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(0, y + 16),
                Width = w,
                FlatStyle = FlatStyle.Flat
            };
            // Avoid mixing a placeholder string with Resident objects under a
            // DisplayMember (which would throw when reflecting on the string).
            _cmbResident.Format += (s, e) =>
            {
                e.Value = e.Value is Resident r ? r.FullName : (e.Value?.ToString() ?? "");
            };
            var residents = Services.ResidentService.GetAllResidents();
            _cmbResident.Items.Add("— Select resident —");
            foreach (var r in residents) _cmbResident.Items.Add(r);
            _cmbResident.SelectedIndex = 0;
            _cmbResident.SelectedIndexChanged += (_, _) => OnResidentSelected();
            scroll.Controls.Add(_cmbResident);
            y += 16 + 40;

            // ----- Auto-populated summary -----
            _residentSummary = new RoundedPanel
            {
                Location = new Point(0, y),
                Size = new Size(w, 96),
                BackColor = Color.FromArgb(248, 250, 252),
                CornerRadius = 10,
                BorderColor = BorderGray
            };
            _residentSummary.Controls.Add(new Label
            {
                Text = "Resident details auto-populate from the record above — no manual re-encoding.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = MutedText,
                AutoSize = false,
                Size = new Size(w - 24, 60),
                Location = new Point(12, 12)
            });
            scroll.Controls.Add(_residentSummary);
            y += 96 + 14;

            // ----- Document type -----
            scroll.Controls.Add(new FieldLabel("Document Type", 0, y));
            _cmbDocType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(0, y + 16),
                Width = w,
                FlatStyle = FlatStyle.Flat
            };
            foreach (DocumentType t in Enum.GetValues(typeof(DocumentType)))
                _cmbDocType.Items.Add(t.Label());
            _cmbDocType.SelectedIndex = 0;
            _cmbDocType.SelectedIndexChanged += (_, _) => PopulateRequirements();
            scroll.Controls.Add(_cmbDocType);
            y += 16 + 40;

            // ----- Purpose -----
            scroll.Controls.Add(new FieldLabel("Purpose", 0, y));
            _txtPurpose = new TextBox
            {
                Location = new Point(0, y + 16),
                Width = w,
                Height = 38,
                Multiline = true,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            scroll.Controls.Add(_txtPurpose);
            y += 16 + 44;

            // ----- Verification section -----
            var verifyCard = new RoundedPanel
            {
                Location = new Point(0, y),
                Size = new Size(w, 196),
                BackColor = Color.FromArgb(250, 251, 252),
                CornerRadius = 10,
                BorderColor = BorderGray
            };
            verifyCard.Controls.Add(new Label
            {
                Text = "VERIFICATION STEP (required before approval)",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = TealAccent,
                AutoSize = true,
                Location = new Point(12, 12)
            });
            _chkResidency = new CheckBox
            {
                Text = "Resident verified as bona fide resident of Barangay Villa M. Tejero",
                Font = new Font("Segoe UI", 9f),
                ForeColor = NavyDark,
                AutoSize = true,
                Location = new Point(12, 36)
            };
            verifyCard.Controls.Add(_chkResidency);

            verifyCard.Controls.Add(new Label
            {
                Text = "Documentary requirements checked:",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(12, 64)
            });
            _clbRequirements = new CheckedListBox
            {
                Location = new Point(12, 84),
                Width = w - 24,
                Height = 100,
                Font = new Font("Segoe UI", 9f),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(250, 251, 252),
                CheckOnClick = true
            };
            verifyCard.Controls.Add(_clbRequirements);
            scroll.Controls.Add(verifyCard);
            y += 196 + 14;

            // ----- Transaction details -----
            scroll.Controls.Add(new FieldLabel("O.R. Number", 0, y));
            _txtOrNo = new TextBox
            {
                Location = new Point(0, y + 16),
                Width = w,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            scroll.Controls.Add(_txtOrNo);
            y += 16 + 40;

            scroll.Controls.Add(new FieldLabel("Fee (₱)", 0, y));
            _txtFee = new TextBox
            {
                Location = new Point(0, y + 16),
                Width = w,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                Text = "0.00"
            };
            scroll.Controls.Add(_txtFee);
            y += 16 + 40;

            // ----- Status -----
            scroll.Controls.Add(new FieldLabel("Status", 0, y));
            var statusFlow = new FlowLayoutPanel
            {
                Location = new Point(0, y + 16),
                Width = w,
                Height = 30,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent
            };
            _rdoPending = new RadioButton { Text = "Pending", Font = new Font("Segoe UI", 9.5f), ForeColor = NavyDark, Checked = true, Margin = new Padding(0, 0, 18, 0) };
            _rdoApproved = new RadioButton { Text = "Approved", Font = new Font("Segoe UI", 9.5f), ForeColor = NavyDark, Margin = new Padding(0, 0, 18, 0) };
            _rdoRejected = new RadioButton { Text = "Rejected", Font = new Font("Segoe UI", 9.5f), ForeColor = NavyDark };
            statusFlow.Controls.AddRange(new Control[] { _rdoPending, _rdoApproved, _rdoRejected });
            scroll.Controls.Add(statusFlow);
            y += 16 + 36;

            // ----- Remarks -----
            scroll.Controls.Add(new FieldLabel("Remarks / Reason", 0, y));
            _txtRemarks = new TextBox
            {
                Location = new Point(0, y + 16),
                Width = w,
                Height = 46,
                Multiline = true,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            scroll.Controls.Add(_txtRemarks);
            y += 16 + 52;

            // ----- Buttons -----
            var btnClear = new FlatButton
            {
                Text = "CLEAR / NEW",
                Size = new Size(120, 40),
                Location = new Point(0, y),
                NormalColor = Color.FromArgb(240, 242, 245),
                HoverColor = Color.FromArgb(228, 231, 236),
                ForeColor = NavyDark
            };
            btnClear.Click += (_, _) => ResetForm();
            scroll.Controls.Add(btnClear);

            var btnSave = new FlatButton
            {
                Text = "SAVE",
                Size = new Size(120, 40),
                Location = new Point(130, y)
            };
            btnSave.Click += (_, _) => SaveDocument();
            scroll.Controls.Add(btnSave);

            var btnPrint = new FlatButton
            {
                Text = "PRINT",
                Size = new Size(140, 40),
                Location = new Point(260, y),
                NormalColor = TealAccent,
                HoverColor = Color.FromArgb(22, 75, 110)
            };
            btnPrint.Click += (_, _) => PrintCurrent();
            scroll.Controls.Add(btnPrint);

            y += 56;
            scroll.Controls.Add(new Panel { Location = new Point(0, y), Size = new Size(1, 1) });

            PopulateRequirements();
            return card;
        }

        // ===================================================================
        //  RIGHT: History
        // ===================================================================

        private Control BuildHistoryCard()
        {
            var card = new RoundedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                CornerRadius = 14,
                BorderColor = BorderGray,
                Padding = new Padding(2)
            };

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.White
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            card.Controls.Add(root);

            // Toolbar
            var toolbar = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            var lblTitle = new Label
            {
                Text = "Issued Documents — History",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = NavyDark,
                AutoSize = true,
                Location = new Point(18, 16)
            };
            toolbar.Controls.Add(lblTitle);

            _txtSearch = new TextBox
            {
                Location = new Point(18, 44),
                Width = 200,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            _txtSearch.PlaceholderText = "Search control no. / purpose...";
            _txtSearch.TextChanged += (_, _) => RefreshGrid();
            toolbar.Controls.Add(_txtSearch);

            _cmbTypeFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(228, 44),
                Width = 200,
                FlatStyle = FlatStyle.Flat
            };
            _cmbTypeFilter.Items.Add("All Types");
            foreach (DocumentType t in Enum.GetValues(typeof(DocumentType)))
                _cmbTypeFilter.Items.Add(t.Label());
            _cmbTypeFilter.SelectedIndex = 0;
            _cmbTypeFilter.SelectedIndexChanged += (_, _) => RefreshGrid();
            toolbar.Controls.Add(_cmbTypeFilter);

            _cmbStatusFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(438, 44),
                Width = 130,
                FlatStyle = FlatStyle.Flat
            };
            _cmbStatusFilter.Items.Add("All Statuses");
            _cmbStatusFilter.Items.Add("Pending");
            _cmbStatusFilter.Items.Add("Approved");
            _cmbStatusFilter.Items.Add("Rejected");
            _cmbStatusFilter.SelectedIndex = 0;
            _cmbStatusFilter.SelectedIndexChanged += (_, _) => RefreshGrid();
            toolbar.Controls.Add(_cmbStatusFilter);

            _lblHistoryCount = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(580, 50)
            };
            toolbar.Controls.Add(_lblHistoryCount);

            var btnRefresh = new FlatButton
            {
                Text = "REFRESH",
                Size = new Size(110, 32),
                Location = new Point(toolbar.Width - 130, 44),
                NormalColor = Color.FromArgb(240, 242, 245),
                HoverColor = Color.FromArgb(228, 231, 236),
                ForeColor = NavyDark,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnRefresh.Click += (_, _) => RefreshGrid();
            toolbar.Controls.Add(btnRefresh);
            toolbar.Resize += (_, _) => btnRefresh.Location = new Point(toolbar.Width - 130, 44);

            root.Controls.Add(toolbar, 0, 0);

            _grid = BuildGrid();
            root.Controls.Add(_grid, 0, 1);

            return card;
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
                RowTemplate = { Height = 48 },
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

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = ColControl, HeaderText = "Control No.", FillWeight = 110 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = ColResident, HeaderText = "Resident", FillWeight = 150 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = ColType, HeaderText = "Document Type", FillWeight = 150 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = ColDate, HeaderText = "Date Requested", FillWeight = 100 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = ColPurpose, HeaderText = "Purpose", FillWeight = 150 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = ColStatus, HeaderText = "Status", FillWeight = 90 });

            var openCol = new DataGridViewButtonColumn
            {
                Name = ColOpen, HeaderText = "", Text = "Open",
                UseColumnTextForButtonValue = true, FlatStyle = FlatStyle.Flat, FillWeight = 60
            };
            openCol.DefaultCellStyle.ForeColor = TealAccent;
            openCol.DefaultCellStyle.BackColor = Color.White;
            openCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            openCol.DefaultCellStyle.SelectionBackColor = Color.White;
            openCol.DefaultCellStyle.SelectionForeColor = TealAccent;
            grid.Columns.Add(openCol);

            var printCol = new DataGridViewButtonColumn
            {
                Name = ColPrint, HeaderText = "", Text = "Print",
                UseColumnTextForButtonValue = true, FlatStyle = FlatStyle.Flat, FillWeight = 60
            };
            printCol.DefaultCellStyle.ForeColor = NavyDark;
            printCol.DefaultCellStyle.BackColor = Color.White;
            printCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            printCol.DefaultCellStyle.SelectionBackColor = Color.White;
            printCol.DefaultCellStyle.SelectionForeColor = NavyDark;
            grid.Columns.Add(printCol);

            grid.CellPainting += Grid_CellPainting;
            grid.CellContentClick += Grid_CellContentClick;

            return grid;
        }

        // ===================================================================
        //  Behavior
        // ===================================================================

        private void PopulateRequirements()
        {
            if (_cmbDocType.SelectedIndex < 0) return;
            var type = (DocumentType)_cmbDocType.SelectedIndex;
            _clbRequirements.Items.Clear();
            foreach (var req in type.RequiredDocuments())
                _clbRequirements.Items.Add(req, false);
        }

        private void OnResidentSelected()
        {
            _selectedResident = _cmbResident.SelectedItem as Resident;
            RenderResidentSummary();
        }

        private void RenderResidentSummary()
        {
            _residentSummary.Controls.Clear();
            if (_selectedResident == null)
            {
                _residentSummary.Controls.Add(new Label
                {
                    Text = "Resident details auto-populate from the record above — no manual re-encoding.",
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                    ForeColor = MutedText,
                    AutoSize = false,
                    Size = new Size(_residentSummary.Width - 24, 60),
                    Location = new Point(12, 12)
                });
                return;
            }

            var r = _selectedResident;
            _residentSummary.Controls.Add(new Label
            {
                Text = r.FullName,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = NavyDark,
                AutoSize = true,
                Location = new Point(12, 10)
            });
            var details = $"{r.Age} / {r.GenderLabel}  •  {r.CivilStatusLabel}\n" +
                          $"Purok: {r.Purok}  •  Contact: {r.ContactNo}\n" +
                          $"Occupation: {r.Occupation}";
            _residentSummary.Controls.Add(new Label
            {
                Text = details,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(70, 80, 95),
                AutoSize = false,
                Size = new Size(_residentSummary.Width - 24, 60),
                Location = new Point(12, 34)
            });
            _residentSummary.Controls.Add(new Label
            {
                Text = "✔ auto-populated",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 130, 90),
                AutoSize = true,
                Location = new Point(_residentSummary.Width - 110, 10)
            });
        }

        private DocumentType SelectedType =>
            _cmbDocType.SelectedIndex >= 0 ? (DocumentType)_cmbDocType.SelectedIndex : DocumentType.CertificateOfResidency;

        private DocumentStatus SelectedStatus =>
            _rdoApproved.Checked ? DocumentStatus.Approved
            : _rdoRejected.Checked ? DocumentStatus.Rejected
            : DocumentStatus.Pending;

        private BarangayDocument ComposeDocument()
        {
            var doc = _activeDoc ?? new BarangayDocument();
            doc.ResidentId = _selectedResident?.ResidentId ?? 0;
            doc.DocumentType = SelectedType;
            doc.Purpose = _txtPurpose.Text.Trim();
            doc.ResidencyVerified = _chkResidency.Checked;
            doc.Requirements = _clbRequirements.CheckedItems.Cast<string>().ToList();
            doc.OrNumber = _txtOrNo.Text.Trim();
            decimal fee = 0;
            decimal.TryParse(_txtFee.Text.Trim(), out fee);
            doc.Fee = fee;
            doc.Status = SelectedStatus;
            doc.Remarks = _txtRemarks.Text.Trim();
            doc.RequestedBy = _currentUser.UserId;
            return doc;
        }

        private void SaveDocument()
        {
            if (_selectedResident == null)
            {
                MessageBox.Show("Please select a resident first.", "Resident Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var doc = ComposeDocument();

            if (doc.Status == DocumentStatus.Approved && !doc.ResidencyVerified)
            {
                MessageBox.Show("Cannot approve: the residency verification box must be checked first.",
                    "Verification Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _rdoPending.Checked = true;
                return;
            }

            doc = DocumentService.Save(doc);
            _activeDoc = doc;
            _lblControlNo.Text = $"Saved • {doc.ControlNo} • {doc.Status.Label()}";
            _lblControlNo.ForeColor = doc.Status.StatusColor();

            LogDocumentAction(doc);

            MessageBox.Show($"Document saved as {doc.ControlNo} ({doc.Status.Label()}).",
                "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);

            RefreshGrid();
        }

        private void LogDocumentAction(BarangayDocument doc)
        {
            string action = doc.DocumentId == 0
                ? $"Generated {doc.DocumentType.Label()}"
                : $"{doc.Status.Label()} {doc.DocumentType.Label()}";
            string details = $"Control {doc.ControlNo} for {_selectedResident?.FullName ?? "(unknown)"} — {doc.Status.Label()}";
            Services.TransactionLogService.Log(LogType.Document, action, _currentUser.FullName, _currentUser.UserId, details);
        }

        private void PrintCurrent()
        {
            if (_selectedResident == null)
            {
                MessageBox.Show("Please select a resident first.", "Resident Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Save (as Pending if nothing chosen) so the printed copy matches the stored record.
            var doc = ComposeDocument();
            if (doc.Status == DocumentStatus.Approved && !doc.ResidencyVerified)
            {
                var cont = MessageBox.Show(
                    "Residency is not verified yet. Print anyway as PENDING?",
                    "Verification Not Complete", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (cont != DialogResult.Yes) return;
                doc.Status = DocumentStatus.Pending;
                _rdoPending.Checked = true;
            }

            if (doc.DocumentId == 0 || _activeDoc == null)
            {
                doc = DocumentService.Save(doc);
                _activeDoc = doc;
                _lblControlNo.Text = $"Saved • {doc.ControlNo} • {doc.Status.Label()}";
                _lblControlNo.ForeColor = doc.Status.StatusColor();
                LogDocumentAction(doc);
            }

            DocumentPrinter.Print(doc, _selectedResident, _currentUser.FullName, _captainName);
            RefreshGrid();
        }

        private void ResetForm()
        {
            _activeDoc = null;
            _selectedResident = null;
            _cmbResident.SelectedIndex = 0;
            _cmbDocType.SelectedIndex = 0;
            _txtPurpose.Clear();
            _chkResidency.Checked = false;
            PopulateRequirements();
            _txtOrNo.Clear();
            _txtFee.Text = "0.00";
            _rdoPending.Checked = true;
            _txtRemarks.Clear();
            _lblFormTitle.Text = "New Document Request";
            _lblControlNo.Text = "Draft — not yet saved";
            _lblControlNo.ForeColor = MutedText;
            RenderResidentSummary();
        }

        private void LoadDocumentIntoForm(BarangayDocument doc)
        {
            _activeDoc = doc;
            _selectedResident = Services.ResidentService.GetById(doc.ResidentId);

            _cmbResident.SelectedIndex = _cmbResident.Items.Cast<object>()
                .ToList().FindIndex(i => i is Resident r && r.ResidentId == doc.ResidentId);
            RenderResidentSummary();

            _cmbDocType.SelectedIndex = (int)doc.DocumentType;
            _txtPurpose.Text = doc.Purpose;
            _chkResidency.Checked = doc.ResidencyVerified;
            PopulateRequirements();
            foreach (var req in doc.Requirements)
            {
                int idx = _clbRequirements.Items.IndexOf(req);
                if (idx >= 0) _clbRequirements.SetItemChecked(idx, true);
            }
            _txtOrNo.Text = doc.OrNumber;
            _txtFee.Text = doc.Fee.ToString("F2");
            _rdoPending.Checked = doc.Status == DocumentStatus.Pending;
            _rdoApproved.Checked = doc.Status == DocumentStatus.Approved;
            _rdoRejected.Checked = doc.Status == DocumentStatus.Rejected;
            _txtRemarks.Text = doc.Remarks;

            _lblFormTitle.Text = $"Edit — {doc.ControlNo}";
            _lblControlNo.Text = $"Loaded • {doc.ControlNo} • {doc.Status.Label()}";
            _lblControlNo.ForeColor = doc.Status.StatusColor();
        }

        // ----- History grid -----

        private void RefreshGrid()
        {
            string search = _txtSearch?.Text.Trim() ?? "";
            var typeFilter = _cmbTypeFilter?.SelectedIndex > 0
                ? (DocumentType?)(_cmbTypeFilter.SelectedIndex - 1) : null;
            var statusFilter = _cmbStatusFilter?.SelectedIndex switch
            {
                1 => (DocumentStatus?)DocumentStatus.Pending,
                2 => (DocumentStatus?)DocumentStatus.Approved,
                3 => (DocumentStatus?)DocumentStatus.Rejected,
                _ => null
            };

            var docs = DocumentService.Search(search, type: typeFilter, status: statusFilter).ToList();
            var residentsById = Services.ResidentService.GetAllResidents().ToDictionary(r => r.ResidentId);

            _grid.Rows.Clear();
            foreach (var d in docs)
            {
                string residentName = residentsById.TryGetValue(d.ResidentId, out var r)
                    ? r.FullName : "(unknown resident)";

                int row = _grid.Rows.Add(
                    d.ControlNo,
                    residentName,
                    d.DocumentType.Label(),
                    d.DateRequested.ToString("MMM d, yyyy"),
                    string.IsNullOrWhiteSpace(d.Purpose) ? "—" : d.Purpose,
                    d.Status.Label(),
                    "Open",
                    "Print");
                _grid.Rows[row].Tag = d.DocumentId;
            }

            int total = DocumentService.TotalIssued;
            _lblHistoryCount.Text = docs.Count == total
                ? $"{total} document(s) total"
                : $"Showing {docs.Count} of {total} document(s)";
        }

        private void Grid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (_grid.Columns[e.ColumnIndex].Name != ColStatus) return;

            e.PaintBackground(e.ClipBounds, true);
            string text = e.FormattedValue?.ToString() ?? "";
            var status = Enum.TryParse<DocumentStatus>(text, out var s) ? s : DocumentStatus.Pending;
            Color pillBg = status.StatusBackColor();
            Color pillText = status.StatusColor();

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var textSize = TextRenderer.MeasureText(text, e.CellStyle.Font);
            int pillWidth = textSize.Width + 24;
            int pillHeight = 22;
            var pillRect = new Rectangle(
                e.CellBounds.X + 8,
                e.CellBounds.Y + (e.CellBounds.Height - pillHeight) / 2,
                pillWidth, pillHeight);

            using var path = RoundHelper.RoundedRect(pillRect, pillHeight / 2);
            using var brush = new SolidBrush(pillBg);
            g.FillPath(brush, path);
            TextRenderer.DrawText(g, text, e.CellStyle.Font, pillRect, pillText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            e.Handled = true;
        }

        private void Grid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int docId = (int)_grid.Rows[e.RowIndex].Tag;
            var doc = DocumentService.GetById(docId);
            if (doc == null) return;
            var col = _grid.Columns[e.ColumnIndex].Name;

            if (col == ColOpen)
            {
                LoadDocumentIntoForm(doc);
            }
            else if (col == ColPrint)
            {
                var resident = Services.ResidentService.GetById(doc.ResidentId);
                DocumentPrinter.Print(doc, resident, _currentUser.FullName, _captainName);
            }
        }

        // ----- Small helper control for field labels -----

        private class FieldLabel : Label
        {
            public FieldLabel(string text, int x, int y)
            {
                Text = text.ToUpper();
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
                ForeColor = MutedText;
                AutoSize = true;
                Location = new Point(x, y);
            }
        }
    }
}
