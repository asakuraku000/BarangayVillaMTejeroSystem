using System;
using System.Drawing;
using System.Windows.Forms;
using BarangayVillaMTejeroSystem.Models;
using BarangayVillaMTejeroSystem.Services;
using BarangayVillaMTejeroSystem.UI;

namespace BarangayVillaMTejeroSystem.Forms
{
    /// <summary>
    /// Modal dialog for creating a new user account or editing an existing one.
    /// When editing, the password field is optional — leaving it blank keeps
    /// the account's current password.
    /// </summary>
    public class UserFormDialog : Form
    {
        private static readonly Color NavyDark = Color.FromArgb(16, 37, 66);
        private static readonly Color MutedText = Color.FromArgb(140, 148, 160);
        private static readonly Color FieldBg = Color.FromArgb(247, 248, 250);
        private static readonly Color BorderGray = Color.FromArgb(230, 233, 238);
        private static readonly Color AccentRed = Color.FromArgb(200, 29, 37);

        private readonly int _editingUserId; // 0 = adding a new account
        private readonly bool _isOwnAccount;  // true when the logged-in user is editing their own account
        private UserRole? _originalRole;      // locked-in role when _isOwnAccount, regardless of combo state

        private TextBox _txtFullName;
        private TextBox _txtUsername;
        private TextBox _txtPosition;
        private TextBox _txtContactNo;
        private ComboBox _cmbRole;
        private TextBox _txtPassword;
        private TextBox _txtConfirmPassword;
        private Label _lblPasswordCaption;
        private Label _lblError;

        /// <summary>The account produced or updated by this dialog after a successful Save.</summary>
        public UserAccount Result { get; private set; }

        public static UserFormDialog CreateForAdd()
        {
            return new UserFormDialog(0, isOwnAccount: false);
        }

        /// <summary>
        /// isOwnAccount: pass true when the account being edited belongs to the
        /// currently logged-in user. This locks the Role field so an admin
        /// can't change their own access level out from under themselves.
        /// </summary>
        public static UserFormDialog CreateForEdit(UserAccount account, bool isOwnAccount = false)
        {
            var dlg = new UserFormDialog(account.UserId, isOwnAccount);
            dlg._txtFullName.Text = account.FullName;
            dlg._txtUsername.Text = account.Username;
            dlg._txtPosition.Text = account.Position;
            dlg._txtContactNo.Text = account.ContactNo;
            dlg._cmbRole.SelectedIndex = account.Role == UserRole.Administrator ? 0 : 1;
            dlg._originalRole = account.Role;
            return dlg;
        }

        private UserFormDialog(int editingUserId, bool isOwnAccount)
        {
            _editingUserId = editingUserId;
            _isOwnAccount = isOwnAccount;
            BuildUi();
        }

        private bool IsEditing => _editingUserId != 0;

        private void BuildUi()
        {
            Text = IsEditing ? "Edit User Account" : "Add New User Account";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);

            // Cap the visible height to something that always fits on screen;
            // if the fields don't all fit, the content area scrolls — but the
            // Save/Cancel footer below is in its own fixed row and is ALWAYS
            // visible, regardless of how tall the field list grows.
            int maxVisibleHeight = Math.Max(420, Screen.PrimaryScreen.WorkingArea.Height - 120);
            ClientSize = new Size(420, Math.Min(620, maxVisibleHeight));

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

            // ===== Scrollable field area =====
            var scrollArea = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(0, 0, 0, 16)
            };
            root.Controls.Add(scrollArea, 0, 0);

            int y = 20;

            var lblTitle = new Label
            {
                Text = IsEditing ? "Edit User Account" : "Add New User Account",
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
                    ? "Update this account's details below."
                    : "Fill in the details to register a new Administrator or Staff account.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = MutedText,
                AutoSize = false,
                Size = new Size(364, 18),
                Location = new Point(28, y)
            };
            scrollArea.Controls.Add(lblSubtitle);
            y += 32;

            _txtFullName = AddField(scrollArea, "FULL NAME", ref y);
            _txtUsername = AddField(scrollArea, "USERNAME", ref y);
            _txtPosition = AddField(scrollArea, "POSITION / DESIGNATION", ref y);
            _txtContactNo = AddField(scrollArea, "CONTACT NUMBER", ref y);

            // ----- Role -----
            var lblRole = new Label
            {
                Text = "ROLE",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(28, y)
            };
            scrollArea.Controls.Add(lblRole);
            y += 20;

            _cmbRole = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10f),
                Location = new Point(28, y),
                Width = 364,
                FlatStyle = FlatStyle.Flat
            };
            _cmbRole.Items.Add("Administrator");
            _cmbRole.Items.Add("Staff");
            _cmbRole.SelectedIndex = 1;
            scrollArea.Controls.Add(_cmbRole);
            y += 34;

            if (_isOwnAccount)
            {
                _cmbRole.Enabled = false;
                var lblRoleNote = new Label
                {
                    Text = "You can't change your own role while logged in.",
                    Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                    ForeColor = MutedText,
                    AutoSize = false,
                    Size = new Size(364, 16),
                    Location = new Point(28, y)
                };
                scrollArea.Controls.Add(lblRoleNote);
                y += 22;
            }
            else
            {
                y += 6;
            }

            // ----- Password -----
            _lblPasswordCaption = new Label
            {
                Text = IsEditing ? "NEW PASSWORD (leave blank to keep current)" : "PASSWORD",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = MutedText,
                AutoSize = false,
                Size = new Size(364, 16),
                Location = new Point(28, y)
            };
            scrollArea.Controls.Add(_lblPasswordCaption);
            y += 20;

            _txtPassword = AddPasswordBox(scrollArea, ref y);
            var lblConfirm = new Label
            {
                Text = "CONFIRM PASSWORD",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(28, y)
            };
            scrollArea.Controls.Add(lblConfirm);
            y += 20;
            _txtConfirmPassword = AddPasswordBox(scrollArea, ref y);

            _lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = AccentRed,
                AutoSize = false,
                Size = new Size(364, 32),
                Location = new Point(28, y)
            };
            scrollArea.Controls.Add(_lblError);
            y += 40;

            // Growing a spacer panel to this final height is what makes the
            // AutoScroll area's scrollbar range match the actual content —
            // otherwise Panel.AutoScroll can't tell how tall the content is.
            scrollArea.Controls.Add(new Panel { Location = new Point(0, y), Size = new Size(1, 1) });

            // ===== Fixed footer — always visible, never scrolls away =====
            var footer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            var footerLine = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = BorderGray };
            footer.Controls.Add(footerLine);

            var btnCancel = new FlatButton
            {
                Text = "CANCEL",
                Size = new Size(174, 42),
                Location = new Point(28, 16),
                NormalColor = Color.FromArgb(240, 242, 245),
                HoverColor = Color.FromArgb(228, 231, 236),
                ForeColor = NavyDark,
                DialogResult = DialogResult.Cancel
            };
            footer.Controls.Add(btnCancel);

            var btnSave = new FlatButton
            {
                Text = IsEditing ? "SAVE CHANGES" : "CREATE ACCOUNT",
                Size = new Size(174, 42),
                Location = new Point(218, 16)
            };
            btnSave.Click += BtnSave_Click;
            footer.Controls.Add(btnSave);

            root.Controls.Add(footer, 0, 1);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private TextBox AddField(Panel host, string label, ref int y)
        {
            var lbl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(28, y)
            };
            host.Controls.Add(lbl);
            y += 20;

            var box = new Panel
            {
                Location = new Point(28, y),
                Size = new Size(364, 36),
                BackColor = FieldBg
            };
            var textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10f),
                Location = new Point(10, 8),
                Width = 344,
                BackColor = FieldBg
            };
            box.Controls.Add(textBox);
            host.Controls.Add(box);
            y += 44;

            return textBox;
        }

        private TextBox AddPasswordBox(Panel host, ref int y)
        {
            var box = new Panel
            {
                Location = new Point(28, y),
                Size = new Size(364, 36),
                BackColor = FieldBg
            };
            var textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10f),
                Location = new Point(10, 8),
                Width = 344,
                BackColor = FieldBg,
                UseSystemPasswordChar = true
            };
            box.Controls.Add(textBox);
            host.Controls.Add(box);
            y += 44;

            return textBox;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            _lblError.Text = "";

            string fullName = _txtFullName.Text.Trim();
            string username = _txtUsername.Text.Trim();
            string position = _txtPosition.Text.Trim();
            string contactNo = _txtContactNo.Text.Trim();
            string password = _txtPassword.Text;
            string confirmPassword = _txtConfirmPassword.Text;
            var role = _cmbRole.SelectedIndex == 0 ? UserRole.Administrator : UserRole.Staff;
            // Backstop for the disabled combo above: your own role can never
            // change through this dialog, no matter what the control reports.
            if (_isOwnAccount && _originalRole.HasValue)
                role = _originalRole.Value;

            if (string.IsNullOrWhiteSpace(fullName))
            {
                ShowError("Full name is required.");
                return;
            }
            if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            {
                ShowError("Username is required (at least 3 characters).");
                return;
            }
            if (UserService.UsernameExists(username, _editingUserId))
            {
                ShowError("That username is already taken. Please choose another.");
                return;
            }
            if (!IsEditing && string.IsNullOrWhiteSpace(password))
            {
                ShowError("Password is required for a new account.");
                return;
            }
            if (!string.IsNullOrWhiteSpace(password))
            {
                if (password.Length < 6)
                {
                    ShowError("Password must be at least 6 characters long.");
                    return;
                }
                if (password != confirmPassword)
                {
                    ShowError("Password and confirmation do not match.");
                    return;
                }
            }

            Result = new UserAccount
            {
                UserId = _editingUserId,
                FullName = fullName,
                Username = username,
                Position = position,
                ContactNo = contactNo,
                Role = role,
                Password = password // may be empty when editing without a reset — UpdateAccount ignores empty values
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