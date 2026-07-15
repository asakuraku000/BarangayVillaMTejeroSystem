using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using BarangayVillaMTejeroSystem.Models;
using BarangayVillaMTejeroSystem.Services;
using BarangayVillaMTejeroSystem.UI;

namespace BarangayVillaMTejeroSystem.Forms
{
    public class LoginForm : Form
    {
        private TextBox _txtUsername;
        private TextBox _txtPassword;
        private EyeToggleButton _eyeToggle;
        private Label _lblError;
        private FlatButton _btnLogin;

        public LoginForm()
        {
            BuildUi();
        }

        private void BuildUi()
        {
            // ----- Form -----
            Text = "Login — Barangay Villa M. Tejero Integrated Management System";
            ClientSize = new Size(1000, 600);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);

            string iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "app.ico");
            if (File.Exists(iconPath)) Icon = new Icon(iconPath);

            // ----- Left branding panel (white) -----
            var brandPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 420,
                BackColor = Color.White
            };

            string logoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "logo.png");
            var logoBox = new PictureBox
            {
                Size = new Size(160, 160),
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point((420 - 160) / 2, 110)
            };
            if (File.Exists(logoPath)) logoBox.Image = Image.FromFile(logoPath);
            brandPanel.Controls.Add(logoBox);

            var lblBarangay = new Label
            {
                Text = "BARANGAY VILLA M. TEJERO",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 37, 66),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Size = new Size(380, 30),
                Location = new Point(20, 290)
            };
            brandPanel.Controls.Add(lblBarangay);

            var lblLocation = new Label
            {
                Text = "Liloy, Zamboanga del Norte",
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = Color.FromArgb(140, 148, 160),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Size = new Size(380, 24),
                Location = new Point(20, 324)
            };
            brandPanel.Controls.Add(lblLocation);

            var divider = new Panel
            {
                Size = new Size(60, 3),
                BackColor = Color.FromArgb(200, 29, 37),
                Location = new Point((420 - 60) / 2, 364)
            };
            brandPanel.Controls.Add(divider);

            var lblTagline = new Label
            {
                Text = "Integrated Management System",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(160, 168, 180),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Size = new Size(380, 24),
                Location = new Point(20, 384)
            };
            brandPanel.Controls.Add(lblTagline);

            Controls.Add(brandPanel);

            // ----- Right accent panel (gradient) -----
            var accentPanel = new GradientPanel
            {
                Dock = DockStyle.Fill,
                ColorTop = Color.FromArgb(16, 37, 66),
                ColorBottom = Color.FromArgb(27, 90, 130)
            };
            Controls.Add(accentPanel);
            accentPanel.BringToFront();

            // ----- Login card -----
            var card = new RoundedPanel
            {
                Size = new Size(360, 420),
                BackColor = Color.White,
                CornerRadius = 16,
                BorderColor = Color.FromArgb(235, 235, 235),
                Padding = new Padding(32)
            };
            accentPanel.Controls.Add(card);
            CenterCardOnPanel(card, accentPanel);
            accentPanel.Resize += (_, _) => CenterCardOnPanel(card, accentPanel);

            var lblWelcome = new Label
            {
                Text = "Welcome Back",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 37, 66),
                AutoSize = true,
                Location = new Point(32, 28)
            };
            card.Controls.Add(lblWelcome);

            var lblSubtitle = new Label
            {
                Text = "Sign in to access the system",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(140, 148, 160),
                AutoSize = true,
                Location = new Point(32, 58)
            };
            card.Controls.Add(lblSubtitle);

            // Username
            var lblUser = new Label
            {
                Text = "USERNAME",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 148, 160),
                AutoSize = true,
                Location = new Point(32, 106)
            };
            card.Controls.Add(lblUser);

            var userBox = new Panel
            {
                Location = new Point(32, 126),
                Size = new Size(296, 36),
                BackColor = Color.FromArgb(247, 248, 250)
            };
            _txtUsername = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10.5f),
                Location = new Point(10, 8),
                Width = 276,
                BackColor = Color.FromArgb(247, 248, 250)
            };
            userBox.Controls.Add(_txtUsername);
            card.Controls.Add(userBox);

            // Password
            var lblPass = new Label
            {
                Text = "PASSWORD",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 148, 160),
                AutoSize = true,
                Location = new Point(32, 178)
            };
            card.Controls.Add(lblPass);

            var passBox = new Panel
            {
                Location = new Point(32, 198),
                Size = new Size(296, 36),
                BackColor = Color.FromArgb(247, 248, 250)
            };
            _txtPassword = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10.5f),
                Location = new Point(10, 8),
                Width = 252,
                UseSystemPasswordChar = true,
                BackColor = Color.FromArgb(247, 248, 250)
            };
            passBox.Controls.Add(_txtPassword);

            // Eye / eye-slash toggle, docked inside the password field itself.
            _eyeToggle = new EyeToggleButton
            {
                Size = new Size(18, 18),
                Location = new Point(passBox.Width - 26, 9),
                PasswordVisible = false
            };
            _eyeToggle.Click += (_, _) =>
            {
                _txtPassword.UseSystemPasswordChar = !_eyeToggle.PasswordVisible;
            };
            passBox.Controls.Add(_eyeToggle);
            card.Controls.Add(passBox);

            _lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(200, 29, 37),
                AutoSize = false,
                Size = new Size(296, 20),
                Location = new Point(32, 240),
                TextAlign = ContentAlignment.MiddleLeft
            };
            card.Controls.Add(_lblError);

            _btnLogin = new FlatButton
            {
                Text = "LOG IN",
                Size = new Size(296, 44),
                Location = new Point(32, 270)
            };
            _btnLogin.Click += BtnLogin_Click;
            card.Controls.Add(_btnLogin);

            var lblHint = new Label
            {
                Text = "Seeded accounts — Admin: admin / Admin@123\nStaff: staff1 / Staff@123",
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(170, 178, 190),
                AutoSize = false,
                Size = new Size(296, 36),
                Location = new Point(32, 328),
                TextAlign = ContentAlignment.TopLeft
            };
            card.Controls.Add(lblHint);

            AcceptButton = _btnLogin;
            _txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnLogin_Click(s, e); };
        }

        private void CenterCardOnPanel(Control card, Control panel)
        {
            card.Location = new Point(
                (panel.Width - card.Width) / 2,
                (panel.Height - card.Height) / 2);
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string username = _txtUsername.Text.Trim();
            string password = _txtPassword.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                _lblError.Text = "Please enter both username and password.";
                return;
            }

            UserAccount account = UserService.Authenticate(username, password);

            if (account == null)
            {
                _lblError.Text = "Invalid username or password.";
                _txtPassword.Clear();
                _txtPassword.Focus();
                return;
            }

            _lblError.Text = "";
            TransactionLogService.Log(LogType.Authentication, "Signed in", account.FullName, account.UserId,
                $"{account.RoleLabel} session started");
            var dashboard = new DashboardForm(account);
            dashboard.FormClosed += (_, _) =>
            {
                _txtUsername.Clear();
                _txtPassword.Clear();
                Show();
            };
            Hide();
            dashboard.Show();
        }
    }
}
