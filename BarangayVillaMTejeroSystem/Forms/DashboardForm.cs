using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using BarangayVillaMTejeroSystem.Controls;
using BarangayVillaMTejeroSystem.Models;
using BarangayVillaMTejeroSystem.UI;

namespace BarangayVillaMTejeroSystem.Forms
{
    public class DashboardForm : Form
    {
        private readonly UserAccount _user;
        private Panel _content;
        private Label _pageTitle;
        private readonly Dictionary<string, SidebarButton> _navButtons = new();
        private string _activeKey = "dashboard";

        private static readonly Color NavyDark = Color.FromArgb(16, 37, 66);
        private static readonly Color NavySidebar = Color.FromArgb(16, 37, 66);
        private static readonly Color BgLight = Color.FromArgb(244, 246, 249);
        private static readonly Color AccentRed = Color.FromArgb(200, 29, 37);
        private static readonly Color TealAccent = Color.FromArgb(27, 90, 130);

        public DashboardForm(UserAccount user)
        {
            _user = user;
            BuildUi();
            NavigateTo("dashboard");
        }

        private void BuildUi()
        {
            Text = "Dashboard — Barangay Villa M. Tejero Integrated Management System";
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1100, 650);
            BackColor = BgLight;
            Font = new Font("Segoe UI", 9.5f);

            string iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "app.ico");
            if (File.Exists(iconPath)) Icon = new Icon(iconPath);

            // ===== Sidebar =====
            // Width is fixed by rootLayout's 250px column below, not by Dock.
            var sidebar = new Panel
            {
                BackColor = NavySidebar
            };

            // Logo + brand
            var brandStrip = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = NavySidebar };
            string logoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "logo.png");
            var logoBox = new PictureBox
            {
                Size = new Size(44, 44),
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(18, 22)
            };
            if (File.Exists(logoPath)) logoBox.Image = Image.FromFile(logoPath);
            brandStrip.Controls.Add(logoBox);

            var lblBrand = new Label
            {
                Text = "Villa M. Tejero",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(72, 24)
            };
            brandStrip.Controls.Add(lblBrand);

            var lblBrandSub = new Label
            {
                Text = "Barangay Liloy, Z.N.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(160, 175, 195),
                AutoSize = true,
                Location = new Point(72, 46)
            };
            brandStrip.Controls.Add(lblBrandSub);

            sidebar.Controls.Add(brandStrip);

            var navDivider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(35, 58, 92) };
            sidebar.Controls.Add(navDivider);

            // Nav items panel (flow layout for clean stacking)
            var navFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Height = 400,
                BackColor = NavySidebar,
                Padding = new Padding(0, 12, 0, 0)
            };

            AddNavButton(navFlow, "dashboard", "🏠", "Dashboard");
            AddNavButton(navFlow, "residents", "🧑‍🤝‍🧑", "Resident Records");
            AddNavButton(navFlow, "documents", "📄", "Barangay Documents");
            AddNavButton(navFlow, "logs", "📊", "Transaction Logs");
            if (_user.Role == UserRole.Administrator)
            {
                AddNavButton(navFlow, "users", "👤", "User Management");
                AddNavButton(navFlow, "backup", "💾", "Backup && Restore");
            }

            sidebar.Controls.Add(navFlow);
            navFlow.BringToFront();
            brandStrip.SendToBack();

            // Logout pinned at bottom
            var logoutPanel = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = NavySidebar };
            var logoutDivider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(35, 58, 92) };
            logoutPanel.Controls.Add(logoutDivider);

            var btnLogout = new SidebarButton
            {
                Dock = DockStyle.Top,
                IconGlyph = "🚪",
                Text = "Logout",
                Height = 46,
                Margin = new Padding(0, 12, 0, 0)
            };
            btnLogout.Click += BtnLogout_Click;
            logoutPanel.Controls.Add(btnLogout);
            sidebar.Controls.Add(logoutPanel);

            // ===== Root layout: 2 fixed columns, like two divs floated left =====
            // Column 0 = sidebar (fixed 250px). Column 1 = main panel (100% of
            // whatever width remains). A TableLayoutPanel enforces these widths
            // directly — it does NOT depend on WinForms' Dock/BringToFront
            // z-order rules, so the two columns can never overlap or slide
            // underneath each other, on the dashboard or any other page.
            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = BgLight
            };
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250f));
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            Controls.Add(rootLayout);

            sidebar.Dock = DockStyle.Fill; // fills its 250px-wide column exactly
            rootLayout.Controls.Add(sidebar, 0, 0);

            // ===== Main panel (everything that is NOT the sidebar) =====
            // This is "div 2" — it fills column 1, i.e. all remaining width.
            // Internally it is itself a 2-row table: row 0 = header (fixed 64px),
            // row 1 = content (100% of remaining height). Same reasoning as
            // rootLayout above — fixed rows/columns instead of Dock z-order,
            // so header and content can never overlap either.
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = BgLight
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 64f));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            rootLayout.Controls.Add(mainPanel, 1, 0);

            // ===== Header =====
            // Height comes from mainPanel's row 0 (Absolute 64f), not from Dock.
            var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            var headerLine = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(228, 231, 236) };
            header.Controls.Add(headerLine);

            _pageTitle = new Label
            {
                Text = "Dashboard",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = NavyDark,
                AutoSize = true,
                Location = new Point(28, 18)
            };
            header.Controls.Add(_pageTitle);

            // User info (right-aligned)
            var avatar = new RoundedPanel
            {
                Size = new Size(36, 36),
                CornerRadius = 18,
                BackColor = TealAccent,
                BorderThickness = 0
            };
            var avatarLabel = new Label
            {
                Text = _user.Initial,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            avatar.Controls.Add(avatarLabel);

            var lblName = new Label
            {
                Text = _user.FullName,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = NavyDark,
                AutoSize = true
            };
            var lblRole = new Label
            {
                Text = _user.RoleLabel,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(140, 148, 160),
                AutoSize = true
            };

            header.Resize += (_, _) => PositionHeaderRight(header, avatar, lblName, lblRole);
            header.Controls.Add(avatar);
            header.Controls.Add(lblName);
            header.Controls.Add(lblRole);

            mainPanel.Controls.Add(header, 0, 0);
            PositionHeaderRight(header, avatar, lblName, lblRole);

            // ===== Content host =====
            // No padding here — each content panel manages its own internal spacing
            // so there is no offset-clipping ambiguity with Dock=Fill children.
            _content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgLight,
                Padding = new Padding(0)
            };
            mainPanel.Controls.Add(_content, 0, 1);
        }

        private void PositionHeaderRight(Panel header, Control avatar, Label name, Label role)
        {
            int rightMargin = 28;
            avatar.Location = new Point(header.Width - rightMargin - avatar.Width, (header.Height - avatar.Height) / 2);
            name.Location = new Point(avatar.Left - 10 - name.Width, header.Height / 2 - 18);
            role.Location = new Point(avatar.Left - 10 - role.Width, header.Height / 2 + 2);
        }

        private void AddNavButton(FlowLayoutPanel host, string key, string icon, string text)
        {
            var btn = new SidebarButton
            {
                IconGlyph = icon,
                Text = text,
                Width = 250,
                Margin = new Padding(0)
            };
            btn.Click += (_, _) => NavigateTo(key);
            host.Controls.Add(btn);
            _navButtons[key] = btn;
        }

        private void NavigateTo(string key)
        {
            _activeKey = key;
            foreach (var kvp in _navButtons)
                kvp.Value.SetActive(kvp.Key == key);

            _content.Controls.Clear();

            Control panel = key switch
            {
                "dashboard" => BuildDashboardPanel(),
                "residents" => new ResidentManagementControl(_user),
                "documents" => new DocumentIssuanceControl(_user),
                "logs" => new TransactionLogsControl(_user),
                "users" => _user.Role == UserRole.Administrator
                    ? new UserManagementControl(_user)
                    : BuildPlaceholderPanel("👤", "User Management",
                        "This module is only available to Administrator accounts."),
                "backup" => BuildPlaceholderPanel("💾", "Backup && Restore",
                    "Protect barangay records with manual backup and restore tools.\nThis module will be available in the next development phase."),
                _ => BuildDashboardPanel()
            };

            _pageTitle.Text = key switch
            {
                "dashboard" => "Dashboard",
                "residents" => "Resident Records",
                "documents" => "Barangay Documents",
                "logs" => "Transaction Logs",
                "users" => "User Management",
                "backup" => "Backup && Restore",
                _ => "Dashboard"
            };

            panel.Dock = DockStyle.Fill;
            _content.Controls.Add(panel);
        }

        private Control BuildDashboardPanel()
        {
            // Root scroll container — owns all the outer margin (was previously on _content)
            var root = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = BgLight,
                Padding = new Padding(28, 24, 28, 24)
            };

            // ── 1. Welcome header (stacked Dock=Top) ───────────────────────────
            var welcomePanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = BgLight,
                Padding = new Padding(0, 0, 0, 8)
            };
            welcomePanel.Controls.Add(new Label
            {
                Text = $"Welcome back, {_user.FullName.Split(' ')[^1]}!",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = NavyDark,
                AutoSize = true,
                Location = new Point(0, 0)
            });
            welcomePanel.Controls.Add(new Label
            {
                Text = DateTime.Now.ToString("dddd, MMMM d, yyyy"),
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(140, 148, 160),
                AutoSize = true,
                Location = new Point(0, 34)
            });

            // ── 2. Stat cards (FlowLayoutPanel — auto-wraps, no pixel math) ───
            var cardsFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                MinimumSize = new Size(0, 166),  // card height 150 + gap 16
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                BackColor = BgLight,
                Padding = new Padding(0, 0, 0, 0)
            };

            var stats = new[]
            {
                ("👥", "Total Residents",    ResidentCountPlaceholder(), Color.FromArgb(27, 90, 130)),
                ("📄", "Documents Issued",   DocumentsIssuedPlaceholder(), Color.FromArgb(200, 29, 37)),
                ("⏳", "Pending Requests",   PendingRequestsPlaceholder(), Color.FromArgb(214, 158, 46)),
                ("🔐", "Registered Users",   UserCountPlaceholder(),  Color.FromArgb(60, 130, 90)),
            };
            foreach (var (icon, title, value, accent) in stats)
            {
                cardsFlow.Controls.Add(new StatCard(icon, title, value, accent)
                {
                    Size = new Size(220, 150),
                    Margin = new Padding(0, 0, 16, 16)
                });
            }

            // ── 3. Spacer between cards and activity card ──────────────────────
            var spacer = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = BgLight };

            // ── 4. Recent Activity card (Dock=Top, fills width minus right margin) ──
            var activityWrapper = new Panel
            {
                Dock = DockStyle.Top,
                Height = 220,
                BackColor = BgLight,
                Padding = new Padding(0, 0, 16, 0)   // right gap so it doesn't butt up to edge
            };
            var activityCard = new RoundedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                CornerRadius = 14,
                BorderColor = Color.FromArgb(232, 235, 240)
            };
            activityCard.Controls.Add(new Label
            {
                Text = "Recent Activity",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = NavyDark,
                AutoSize = true,
                Location = new Point(20, 18)
            });

            // Live recent activity from the Transaction Logs audit trail.
            int ay = 52;
            var recent = Services.TransactionLogService.GetRecent(6);
            if (recent.Count == 0)
            {
                activityCard.Controls.Add(new Label
                {
                    Text = "No recent activity yet.",
                    Font = new Font("Segoe UI", 9.5f),
                    ForeColor = Color.FromArgb(150, 158, 170),
                    AutoSize = false,
                    Size = new Size(600, 30),
                    Location = new Point(20, ay)
                });
            }
            else
            {
                foreach (var log in recent)
                {
                    var dot = new Panel
                    {
                        Location = new Point(24, ay + 6),
                        Size = new Size(8, 8),
                        BackColor = log.Type.Color()
                    };
                    activityCard.Controls.Add(dot);

                    var entry = new Label
                    {
                        Text = $"{log.Action}  —  {log.Actor}",
                        Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                        ForeColor = NavyDark,
                        AutoSize = false,
                        Size = new Size(activityCard.Width - 60, 18),
                        Location = new Point(40, ay)
                    };
                    activityCard.Controls.Add(entry);

                    var sub = new Label
                    {
                        Text = log.TimestampLabel,
                        Font = new Font("Segoe UI", 8f),
                        ForeColor = Color.FromArgb(150, 158, 170),
                        AutoSize = false,
                        Size = new Size(activityCard.Width - 60, 16),
                        Location = new Point(40, ay + 17)
                    };
                    activityCard.Controls.Add(sub);
                    ay += 30;
                }
            }

            // Divider line under "Recent Activity" title
            activityCard.Controls.Add(new Panel
            {
                Location = new Point(20, 44),
                Size = new Size(2000, 1),       // wide; gets clipped by card bounds
                BackColor = Color.FromArgb(232, 235, 240)
            });

            activityWrapper.Controls.Add(activityCard);

            // Add in reverse DockStyle.Top order (last added = topmost)
            root.Controls.Add(activityWrapper);
            root.Controls.Add(spacer);
            root.Controls.Add(cardsFlow);
            root.Controls.Add(welcomePanel);

            return root;
        }

        private string UserCountPlaceholder()
        {
            return Services.UserService.GetAllAccounts().Count.ToString();
        }

        private string ResidentCountPlaceholder()
        {
            return Services.ResidentService.TotalActiveResidents.ToString();
        }

        private string DocumentsIssuedPlaceholder()
        {
            return Services.DocumentService.TotalIssued.ToString();
        }

        private string PendingRequestsPlaceholder()
        {
            return Services.DocumentService.PendingRequests.ToString();
        }

        private Control BuildPlaceholderPanel(string icon, string title, string subtitle)
        {
            var root = new Panel { Dock = DockStyle.Fill, BackColor = BgLight, Padding = new Padding(28) };

            var card = new RoundedPanel
            {
                Size = new Size(560, 280),
                BackColor = Color.White,
                CornerRadius = 16,
                BorderColor = Color.FromArgb(232, 235, 240)
            };
            root.Controls.Add(card);

            void Reposition()
            {
                card.Location = new Point((root.Width - card.Width) / 2, Math.Max(20, (root.Height - card.Height) / 2));
            }
            root.Resize += (_, _) => Reposition();

            var lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 36f),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(card.Width, 70),
                Location = new Point(0, 36)
            };
            card.Controls.Add(lblIcon);

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = NavyDark,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Size = new Size(card.Width - 40, 28),
                Location = new Point(20, 116)
            };
            card.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(150, 158, 170),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Size = new Size(card.Width - 60, 60),
                Location = new Point(30, 156)
            };
            card.Controls.Add(lblSub);

            var badge = new Label
            {
                Text = "COMING SOON",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = AccentRed,
                BackColor = Color.FromArgb(252, 235, 236),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Size = new Size(130, 26),
                Location = new Point((card.Width - 130) / 2, 226)
            };
            card.Controls.Add(badge);

            Reposition();
            return root;
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to log out?",
                "Confirm Logout",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (result == DialogResult.Yes)
            {
                Services.TransactionLogService.Log(LogType.Authentication, "Signed out", _user.FullName, _user.UserId,
                    $"{_user.RoleLabel} session ended");
                Close();
            }
        }
    }
}
