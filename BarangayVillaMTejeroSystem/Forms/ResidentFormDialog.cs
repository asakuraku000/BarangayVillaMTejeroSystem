using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BarangayVillaMTejeroSystem.Models;
using BarangayVillaMTejeroSystem.UI;

namespace BarangayVillaMTejeroSystem.Forms
{
    /// <summary>
    /// Modal dialog for creating a new resident profile or editing an
    /// existing one. Mirrors UserFormDialog's structure: a scrollable field
    /// area with a fixed Save/Cancel footer that never scrolls away.
    /// </summary>
    public class ResidentFormDialog : Form
    {
        private static readonly Color NavyDark = Color.FromArgb(16, 37, 66);
        private static readonly Color MutedText = Color.FromArgb(140, 148, 160);
        private static readonly Color FieldBg = Color.FromArgb(247, 248, 250);
        private static readonly Color BorderGray = Color.FromArgb(230, 233, 238);
        private static readonly Color AccentRed = Color.FromArgb(200, 29, 37);

        private readonly int _editingResidentId; // 0 = adding a new resident

        private TextBox _txtLastName;
        private TextBox _txtFirstName;
        private TextBox _txtMiddleName;
        private TextBox _txtSuffix;
        private DateTimePicker _dtBirthDate;
        private ComboBox _cmbGender;
        private ComboBox _cmbCivilStatus;
        private TextBox _txtPurok;
        private TextBox _txtContactNo;
        private TextBox _txtOccupation;
        private TextBox _txtHousehold;
        private Label _lblError;

        /// <summary>The resident produced or updated by this dialog after a successful Save.</summary>
        public Resident Result { get; private set; }

        public static ResidentFormDialog CreateForAdd()
        {
            return new ResidentFormDialog(0);
        }

        public static ResidentFormDialog CreateForEdit(Resident resident)
        {
            var dlg = new ResidentFormDialog(resident.ResidentId);
            dlg._txtLastName.Text = resident.LastName;
            dlg._txtFirstName.Text = resident.FirstName;
            dlg._txtMiddleName.Text = resident.MiddleName;
            dlg._txtSuffix.Text = resident.Suffix;
            dlg._dtBirthDate.Value = resident.BirthDate == default ? DateTime.Today.AddYears(-18) : resident.BirthDate;
            dlg._cmbGender.SelectedIndex = resident.Gender == Gender.Male ? 0 : 1;
            dlg._cmbCivilStatus.SelectedIndex = (int)resident.CivilStatus;
            dlg._txtPurok.Text = resident.Purok;
            dlg._txtContactNo.Text = resident.ContactNo;
            dlg._txtOccupation.Text = resident.Occupation;
            dlg._txtHousehold.Text = string.Join(Environment.NewLine, resident.HouseholdMembers ?? new System.Collections.Generic.List<string>());
            return dlg;
        }

        private ResidentFormDialog(int editingResidentId)
        {
            _editingResidentId = editingResidentId;
            BuildUi();
        }

        private bool IsEditing => _editingResidentId != 0;

        private void BuildUi()
        {
            Text = IsEditing ? "Edit Resident Profile" : "Add New Resident";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);

            int maxVisibleHeight = Math.Max(420, Screen.PrimaryScreen.WorkingArea.Height - 120);
            ClientSize = new Size(440, Math.Min(720, maxVisibleHeight));

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.White
            };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));
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
            int fieldWidth = 384;

            var lblTitle = new Label
            {
                Text = IsEditing ? "Edit Resident Profile" : "Add New Resident",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = NavyDark,
                AutoSize = true,
                Location = new Point(28, y)
            };
            scrollArea.Controls.Add(lblTitle);
            y += 30;

            var lblSubtitle = new Label
            {
                Text = IsEditing
                    ? "Update this resident's profile details below."
                    : "Fill in the details to register a new resident.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = MutedText,
                AutoSize = false,
                Size = new Size(fieldWidth, 18),
                Location = new Point(28, y)
            };
            scrollArea.Controls.Add(lblSubtitle);
            y += 32;

            // ----- Name row (Last / First) -----
            _txtLastName = AddField(scrollArea, "LAST NAME", 28, 184, ref y, advanceY: false);
            _txtFirstName = AddField(scrollArea, "FIRST NAME", 228, 184, ref y);

            // ----- Name row (Middle / Suffix) -----
            _txtMiddleName = AddField(scrollArea, "MIDDLE NAME", 28, 184, ref y, advanceY: false);
            _txtSuffix = AddField(scrollArea, "SUFFIX (OPTIONAL)", 228, 184, ref y);

            // ----- Birth date -----
            var lblBirth = new Label
            {
                Text = "BIRTH DATE",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(28, y)
            };
            scrollArea.Controls.Add(lblBirth);
            y += 20;
            _dtBirthDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 10f),
                Location = new Point(28, y),
                Width = fieldWidth,
                MaxDate = DateTime.Today,
                Value = DateTime.Today.AddYears(-18)
            };
            scrollArea.Controls.Add(_dtBirthDate);
            y += 44;

            // ----- Gender / Civil status row -----
            var lblGender = new Label { Text = "GENDER", Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = MutedText, AutoSize = true, Location = new Point(28, y) };
            var lblCivil = new Label { Text = "CIVIL STATUS", Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = MutedText, AutoSize = true, Location = new Point(228, y) };
            scrollArea.Controls.Add(lblGender);
            scrollArea.Controls.Add(lblCivil);
            y += 20;

            _cmbGender = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10f), Location = new Point(28, y), Width = 184, FlatStyle = FlatStyle.Flat };
            _cmbGender.Items.Add("Male");
            _cmbGender.Items.Add("Female");
            _cmbGender.SelectedIndex = 0;
            scrollArea.Controls.Add(_cmbGender);

            _cmbCivilStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10f), Location = new Point(228, y), Width = 184, FlatStyle = FlatStyle.Flat };
            _cmbCivilStatus.Items.Add("Single");
            _cmbCivilStatus.Items.Add("Married");
            _cmbCivilStatus.Items.Add("Widowed");
            _cmbCivilStatus.Items.Add("Separated");
            _cmbCivilStatus.SelectedIndex = 0;
            scrollArea.Controls.Add(_cmbCivilStatus);
            y += 44;

            _txtPurok = AddField(scrollArea, "PUROK / ADDRESS", 28, fieldWidth, ref y);
            _txtContactNo = AddField(scrollArea, "CONTACT NUMBER", 28, fieldWidth, ref y);
            _txtOccupation = AddField(scrollArea, "OCCUPATION", 28, fieldWidth, ref y);

            // ----- Household composition -----
            var lblHousehold = new Label
            {
                Text = "HOUSEHOLD COMPOSITION (ONE MEMBER PER LINE)",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = MutedText,
                AutoSize = false,
                Size = new Size(fieldWidth, 16),
                Location = new Point(28, y)
            };
            scrollArea.Controls.Add(lblHousehold);
            y += 20;

            var householdBox = new Panel
            {
                Location = new Point(28, y),
                Size = new Size(fieldWidth, 84),
                BackColor = FieldBg
            };
            _txtHousehold = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(10, 8),
                Width = fieldWidth - 20,
                Height = 68,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = FieldBg
            };
            householdBox.Controls.Add(_txtHousehold);
            scrollArea.Controls.Add(householdBox);
            y += 96;

            var lblHouseholdHint = new Label
            {
                Text = "e.g. \"Maria Dela Cruz (spouse)\" — leave blank if this resident lives alone.",
                Font = new Font("Segoe UI", 8f),
                ForeColor = MutedText,
                AutoSize = false,
                Size = new Size(fieldWidth, 16),
                Location = new Point(28, y)
            };
            scrollArea.Controls.Add(lblHouseholdHint);
            y += 28;

            _lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = AccentRed,
                AutoSize = false,
                Size = new Size(fieldWidth, 32),
                Location = new Point(28, y)
            };
            scrollArea.Controls.Add(_lblError);
            y += 40;

            scrollArea.Controls.Add(new Panel { Location = new Point(0, y), Size = new Size(1, 1) });

            // ===== Fixed footer =====
            var footer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            var footerLine = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = BorderGray };
            footer.Controls.Add(footerLine);

            var btnCancel = new FlatButton
            {
                Text = "CANCEL",
                Size = new Size(184, 42),
                Location = new Point(28, 16),
                NormalColor = Color.FromArgb(240, 242, 245),
                HoverColor = Color.FromArgb(228, 231, 236),
                ForeColor = NavyDark,
                DialogResult = DialogResult.Cancel
            };
            footer.Controls.Add(btnCancel);

            var btnSave = new FlatButton
            {
                Text = IsEditing ? "SAVE CHANGES" : "REGISTER RESIDENT",
                Size = new Size(184, 42),
                Location = new Point(228, 16)
            };
            btnSave.Click += BtnSave_Click;
            footer.Controls.Add(btnSave);

            root.Controls.Add(footer, 0, 1);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private TextBox AddField(Panel host, string label, int x, int width, ref int y, bool advanceY = true)
        {
            int startY = y;
            var lbl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(x, startY)
            };
            host.Controls.Add(lbl);

            var box = new Panel
            {
                Location = new Point(x, startY + 20),
                Size = new Size(width, 36),
                BackColor = FieldBg
            };
            var textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10f),
                Location = new Point(10, 8),
                Width = width - 20,
                BackColor = FieldBg
            };
            box.Controls.Add(textBox);
            host.Controls.Add(box);

            if (advanceY) y = startY + 64;
            return textBox;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            _lblError.Text = "";

            string lastName = _txtLastName.Text.Trim();
            string firstName = _txtFirstName.Text.Trim();
            string middleName = _txtMiddleName.Text.Trim();
            string suffix = _txtSuffix.Text.Trim();
            string purok = _txtPurok.Text.Trim();
            string contactNo = _txtContactNo.Text.Trim();
            string occupation = _txtOccupation.Text.Trim();
            var gender = _cmbGender.SelectedIndex == 0 ? Gender.Male : Gender.Female;
            var civilStatus = (CivilStatus)_cmbCivilStatus.SelectedIndex;
            var householdMembers = _txtHousehold.Text
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            if (string.IsNullOrWhiteSpace(lastName))
            {
                ShowError("Last name is required.");
                return;
            }
            if (string.IsNullOrWhiteSpace(firstName))
            {
                ShowError("First name is required.");
                return;
            }
            if (string.IsNullOrWhiteSpace(purok))
            {
                ShowError("Purok / address is required.");
                return;
            }
            if (_dtBirthDate.Value.Date > DateTime.Today)
            {
                ShowError("Birth date cannot be in the future.");
                return;
            }

            Result = new Resident
            {
                ResidentId = _editingResidentId,
                LastName = lastName,
                FirstName = firstName,
                MiddleName = middleName,
                Suffix = suffix,
                BirthDate = _dtBirthDate.Value.Date,
                Gender = gender,
                CivilStatus = civilStatus,
                Purok = purok,
                ContactNo = contactNo,
                Occupation = occupation,
                HouseholdMembers = householdMembers
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        private void ShowError(string message)
        {
            _lblError.Text = message;
        }
    }
}
