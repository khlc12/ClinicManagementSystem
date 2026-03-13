using System.ComponentModel;
using System.Globalization;
using System.Text;
using ClinicManagementSystem.Forms;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Services;
using ClinicManagementSystem.Ui;

namespace ClinicManagementSystem;

public partial class Form1 : Form
{
    private readonly ClinicContext context;
    private readonly ClinicLookupService lookupService;
    private readonly PrescriptionService prescriptionService;
    private readonly UserRulesService userRulesService;
    private readonly NavigationTabControl moduleTabs = new() { Dock = DockStyle.Fill };
    private readonly FlowLayoutPanel navigationBar = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        WrapContents = false,
        BackColor = ClinicTheme.Surface,
        Padding = new Padding(10, 2, 10, 2)
    };
    private readonly Label welcomeLabel = new() { AutoSize = true, Font = ClinicTheme.BodyBold };
    private readonly Label statusLabel = new() { AutoSize = true, Font = ClinicTheme.Caption, ForeColor = ClinicTheme.TextSecondary };
    private readonly List<Action> refreshBindings = new();
    private readonly List<Action> refreshReports = new();
    private readonly Dictionary<string, Label> dashboardMetrics = new();
    private readonly Dictionary<TabPage, Button> navigationButtons = new();
    private bool adjustingNavigationPadding;
    public bool LoggedOut { get; private set; }

    public Form1(ClinicContext context)
    {
        this.context = context;
        lookupService = new ClinicLookupService(context.Data);
        prescriptionService = new PrescriptionService(context.Data);
        userRulesService = new UserRulesService(context.Data);
        InitializeComponent();
        BuildLayout();
        PopulateTabs();
        RefreshDashboard();
    }

    private void BuildLayout()
    {
        Text = "Clinic Management System";
        WindowState = FormWindowState.Maximized;
        ClinicTheme.StyleSurface(this);
        moduleTabs.BackColor = ClinicTheme.AppBackground;
        moduleTabs.SelectedIndexChanged += (_, _) => UpdateNavigationSelection();

        var shellLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = ClinicTheme.AppBackground
        };
        shellLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 168));
        shellLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        shellLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var headerHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 16, 20, 8),
            BackColor = ClinicTheme.AppBackground
        };

        var headerPanel = new GradientPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 22, 28, 18),
            StartColor = ClinicTheme.BrandDark,
            EndColor = ClinicTheme.Brand,
            ShapeColor = Color.FromArgb(26, 255, 255, 255),
            OverlayImage = ClinicTheme.GetOverlayImage("header-overlay.png", "hero-overlay.png"),
            DrawDecorativeShapes = false,
            ScrimColor = Color.FromArgb(88, 10, 42, 48)
        };
        ClinicTheme.RoundControl(headerPanel, 28);

        var headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));

        var brandPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        brandPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        brandPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var brandMark = new Panel
        {
            Size = new Size(56, 56),
            Margin = new Padding(0, 10, 16, 0),
            BackColor = ClinicTheme.Accent
        };
        ClinicTheme.RoundControl(brandMark, 18);
        var logoImage = ClinicTheme.GetOverlayImage("app-logo.png", "clinic-logo.png", "logo.png");
        if (logoImage is not null)
        {
            brandMark.BackColor = Color.FromArgb(236, 247, 248);
            brandMark.Padding = new Padding(6);
            var logoBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                Image = logoImage,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            brandMark.Controls.Add(logoBox);
        }
        else
        {
            var markLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "CM",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Bahnschrift SemiBold", 16f, FontStyle.Bold),
                ForeColor = ClinicTheme.BrandDark,
                BackColor = Color.Transparent
            };
            brandMark.Controls.Add(markLabel);
        }

        var brandTextPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        brandTextPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        brandTextPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        brandTextPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var titleLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = "Clinic Management System",
            Font = ClinicTheme.DisplayLarge,
            ForeColor = Color.White,
            UseCompatibleTextRendering = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0),
            Padding = new Padding(0, 2, 0, 0)
        };
        var subtitleLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = "Records, visits, billing, and stock in one offline workspace.",
            Font = ClinicTheme.Body,
            ForeColor = Color.FromArgb(246, 251, 252),
            MaximumSize = new Size(520, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0)
        };
        var brandPills = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        brandPills.Controls.Add(ClinicTheme.CreatePill("Offline desktop system", ClinicTheme.AccentSoft, ClinicTheme.BrandDark));
        brandPills.Controls.Add(ClinicTheme.CreatePill("Local data storage", Color.FromArgb(55, 111, 118), Color.White));
        brandTextPanel.Controls.Add(titleLabel, 0, 0);
        brandTextPanel.Controls.Add(subtitleLabel, 0, 1);
        brandTextPanel.Controls.Add(brandPills, 0, 2);
        brandPanel.Controls.Add(brandMark, 0, 0);
        brandPanel.Controls.Add(brandTextPanel, 1, 0);

        welcomeLabel.Text = $"Logged in as {context.CurrentUser?.FullName}";
        welcomeLabel.ForeColor = Color.FromArgb(250, 254, 255);
        statusLabel.Text = $"{context.CurrentUser?.Role} access | Offline mode enabled";
        statusLabel.ForeColor = Color.FromArgb(241, 249, 250);

        var utilityLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        utilityLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        utilityLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var utilityMeta = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        var dateLabel = new Label
        {
            AutoSize = true,
            Text = DateTime.Now.ToString("dddd, MMMM d"),
            Font = ClinicTheme.BodyBold,
            ForeColor = Color.White,
            Padding = new Padding(0, 6, 0, 0),
            Margin = new Padding(0)
        };
        var workspaceLabel = new Label
        {
            AutoSize = true,
            Text = "Local clinic workspace",
            Font = ClinicTheme.Caption,
            ForeColor = Color.FromArgb(243, 250, 251),
            Padding = new Padding(0, 8, 18, 0),
            Margin = new Padding(0)
        };
        utilityMeta.Controls.Add(dateLabel);
        utilityMeta.Controls.Add(workspaceLabel);

        var utilityBottom = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        utilityBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        utilityBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 138));

        var sessionPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(55, 111, 118),
            Padding = new Padding(18, 14, 18, 14),
            Margin = new Padding(18, 0, 16, 0)
        };
        ClinicTheme.RoundControl(sessionPanel, 20);
        sessionPanel.Paint += (_, e) => ClinicTheme.DrawRoundedBorder(e.Graphics, sessionPanel.ClientRectangle, 20, Color.FromArgb(190, 223, 226));
        var sessionTitle = new Label
        {
            AutoSize = true,
            Text = "Current Session",
            Font = ClinicTheme.Caption,
            ForeColor = Color.FromArgb(225, 237, 239),
            Location = new Point(18, 10)
        };
        welcomeLabel.Location = new Point(18, 30);
        statusLabel.Location = new Point(18, 54);
        sessionPanel.Controls.Add(sessionTitle);
        sessionPanel.Controls.Add(welcomeLabel);
        sessionPanel.Controls.Add(statusLabel);

        var actionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 6, 0, 0),
            Margin = new Padding(0)
        };
        var logoutButton = new Button
        {
            Text = "Logout",
            Width = 112,
            Height = 40,
            Margin = new Padding(0)
        };
        ClinicTheme.StyleGhostButton(logoutButton);
        logoutButton.Click += (_, _) => Logout();
        actionsPanel.Controls.Add(logoutButton);

        utilityBottom.Controls.Add(sessionPanel, 0, 0);
        utilityBottom.Controls.Add(actionsPanel, 1, 0);
        utilityLayout.Controls.Add(utilityMeta, 0, 0);
        utilityLayout.Controls.Add(utilityBottom, 0, 1);

        headerLayout.Controls.Add(brandPanel, 0, 0);
        headerLayout.Controls.Add(utilityLayout, 1, 0);
        headerPanel.Controls.Add(headerLayout);
        headerHost.Controls.Add(headerPanel);

        var navigationHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 2, 20, 2),
            BackColor = ClinicTheme.AppBackground
        };
        var navigationCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ClinicTheme.Surface
        };
        ClinicTheme.RoundControl(navigationCard, 24);
        navigationCard.Controls.Add(navigationBar);
        navigationHost.Controls.Add(navigationCard);

        var contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 0, 20, 20),
            BackColor = ClinicTheme.AppBackground
        };
        contentHost.Controls.Add(moduleTabs);

        shellLayout.Controls.Add(headerHost, 0, 0);
        shellLayout.Controls.Add(navigationHost, 0, 1);
        shellLayout.Controls.Add(contentHost, 0, 2);
        Controls.Add(shellLayout);

        void ApplyShellLayout()
        {
            var compact = ClientSize.Height <= 760;
            shellLayout.RowStyles[0].Height = compact ? 156 : 168;
            shellLayout.RowStyles[1].Height = compact ? 52 : 56;
            headerHost.Padding = compact ? new Padding(16, 12, 16, 6) : new Padding(20, 16, 20, 8);
            headerPanel.Padding = compact ? new Padding(24, 18, 24, 16) : new Padding(28, 22, 28, 18);
            navigationHost.Padding = compact ? new Padding(16, 2, 16, 2) : new Padding(20, 2, 20, 2);
            navigationBar.Padding = compact ? new Padding(8, 2, 8, 2) : new Padding(10, 2, 10, 2);
            utilityBottom.ColumnStyles[1].Width = compact ? 128 : 138;
            logoutButton.Width = compact ? 104 : 112;
            logoutButton.Height = compact ? 38 : 40;
            brandPills.Visible = !compact;
            brandTextPanel.RowStyles[2].SizeType = compact ? SizeType.Absolute : SizeType.Percent;
            brandTextPanel.RowStyles[2].Height = compact ? 0 : 100;

            foreach (var button in navigationButtons.Values)
            {
                button.MinimumSize = new Size(compact ? 122 : 132, compact ? 38 : 42);
                button.Height = compact ? 38 : 42;
                button.Padding = compact ? new Padding(16, 0, 16, 0) : new Padding(18, 0, 18, 0);
            }

            BalanceNavigationPadding();
        }

        Resize += (_, _) => ApplyShellLayout();
        navigationBar.SizeChanged += (_, _) => BalanceNavigationPadding();
        ApplyShellLayout();
    }

    private void PopulateTabs()
    {
        var previousPageTitle = moduleTabs.SelectedTab?.Text;
        moduleTabs.SuspendLayout();
        moduleTabs.TabPages.Clear();

        moduleTabs.TabPages.Add(BuildDashboardTab());
        moduleTabs.TabPages.Add(BuildPatientsTab());
        moduleTabs.TabPages.Add(BuildPatientHistoryTab());
        moduleTabs.TabPages.Add(BuildAppointmentsTab());

        if (HasAnyRole(UserRole.Administrator, UserRole.Doctor))
        {
            moduleTabs.TabPages.Add(BuildConsultationsTab());
        }

        if (HasAnyRole(UserRole.Administrator, UserRole.Receptionist))
        {
            moduleTabs.TabPages.Add(BuildBillingTab());
        }

        if (HasAnyRole(UserRole.Administrator, UserRole.Doctor))
        {
            moduleTabs.TabPages.Add(BuildMedicinesTab());
        }

        moduleTabs.TabPages.Add(BuildReportsTab());

        if (HasAnyRole(UserRole.Administrator))
        {
            moduleTabs.TabPages.Add(BuildUsersTab());
        }

        RefreshNavigationButtons();
        var targetPage = moduleTabs.TabPages.Cast<TabPage>().FirstOrDefault(page => string.Equals(page.Text, previousPageTitle, StringComparison.Ordinal))
            ?? moduleTabs.TabPages.Cast<TabPage>().FirstOrDefault();
        if (targetPage is not null)
        {
            moduleTabs.SelectedTab = targetPage;
        }

        moduleTabs.ResumeLayout();
        UpdateNavigationSelection();
    }

    private void RefreshNavigationButtons()
    {
        navigationButtons.Clear();
        navigationBar.Controls.Clear();
        var tabCount = moduleTabs.TabPages.Count;
        var tabIndex = 0;

        foreach (TabPage page in moduleTabs.TabPages)
        {
            var isLast = tabIndex == tabCount - 1;
            var button = new Button
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(132, 42),
                Height = 42,
                Text = page.Text,
                Margin = isLast ? new Padding(0) : new Padding(0, 0, 8, 0),
                Padding = new Padding(18, 0, 18, 0),
                FlatStyle = FlatStyle.Flat,
                Font = ClinicTheme.Navigation,
                Cursor = Cursors.Hand,
                TabStop = false,
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = ClinicTheme.SurfaceMuted;
            button.FlatAppearance.MouseDownBackColor = ClinicTheme.SurfaceMuted;
            button.Click += (_, _) => moduleTabs.SelectedTab = page;
            ClinicTheme.RoundControl(button, 16);
            navigationButtons[page] = button;
            navigationBar.Controls.Add(button);
            tabIndex++;
        }

        BalanceNavigationPadding();
        UpdateNavigationSelection();
    }

    private void BalanceNavigationPadding()
    {
        if (adjustingNavigationPadding || navigationBar.Controls.Count == 0)
        {
            return;
        }

        var compact = ClientSize.Height <= 760;
        var baseHorizontal = compact ? 8 : 10;
        var baseVertical = 2;
        var totalButtonsWidth = navigationBar.Controls
            .Cast<Control>()
            .Sum(control => control.Width + control.Margin.Left + control.Margin.Right);
        var availableWidth = navigationBar.ClientSize.Width;
        var spare = availableWidth - totalButtonsWidth - (baseHorizontal * 2);
        var horizontal = baseHorizontal + Math.Max(0, spare / 2);
        var desired = new Padding(horizontal, baseVertical, horizontal, baseVertical);

        if (navigationBar.Padding == desired)
        {
            return;
        }

        adjustingNavigationPadding = true;
        try
        {
            navigationBar.Padding = desired;
        }
        finally
        {
            adjustingNavigationPadding = false;
        }
    }

    private void UpdateNavigationSelection()
    {
        foreach (var pair in navigationButtons)
        {
            var isSelected = moduleTabs.SelectedTab == pair.Key;
            var button = pair.Value;
            button.BackColor = isSelected ? ClinicTheme.BrandDark : ClinicTheme.SurfaceRaised;
            button.ForeColor = isSelected ? Color.White : ClinicTheme.TextSecondary;
            button.FlatAppearance.BorderColor = isSelected ? ClinicTheme.BrandDark : ClinicTheme.Border;
            button.FlatAppearance.BorderSize = isSelected ? 1 : 1;
            button.FlatAppearance.MouseOverBackColor = isSelected ? ClinicTheme.BrandDark : ClinicTheme.SurfaceMuted;
            button.FlatAppearance.MouseDownBackColor = isSelected ? ClinicTheme.BrandDark : ClinicTheme.SurfaceMuted;
            button.Padding = isSelected ? new Padding(20, 0, 20, 0) : new Padding(18, 0, 18, 0);
        }
    }

    private TabPage BuildDashboardTab()
    {
        var page = new TabPage("Dashboard") { BackColor = ClinicTheme.AppBackground };
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(14, 14, 14, 14),
            BackColor = ClinicTheme.AppBackground
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 188));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var heroPanel = new GradientPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(26, 20, 26, 18),
            StartColor = ClinicTheme.BrandDark,
            EndColor = ClinicTheme.Brand,
            ShapeColor = Color.FromArgb(24, 255, 255, 255),
            OverlayImage = ClinicTheme.GetOverlayImage("dashboard-hero-overlay.png", "hero-overlay.png"),
            DrawDecorativeShapes = false,
            ScrimColor = Color.FromArgb(96, 8, 40, 47)
        };
        ClinicTheme.RoundControl(heroPanel, 28);

        var heroLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var heroTitle = new Label
        {
            AutoSize = true,
            Text = "Welcome back to the clinic floor.",
            Font = ClinicTheme.DisplayLarge,
            ForeColor = Color.White,
            MaximumSize = new Size(920, 0),
            Margin = new Padding(0, 0, 0, 4)
        };
        var heroSubtitle = new Label
        {
            AutoSize = true,
            Text = "Quick overview of today's clinic activity and key operational counts.",
            Font = ClinicTheme.Body,
            ForeColor = Color.FromArgb(242, 249, 250),
            MaximumSize = new Size(920, 0),
            Margin = new Padding(0, 0, 0, 4)
        };
        var heroPills = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 12, 0, 0)
        };
        heroPills.Controls.Add(ClinicTheme.CreatePill("Offline workflow", ClinicTheme.AccentSoft, ClinicTheme.BrandDark));
        heroPills.Controls.Add(ClinicTheme.CreatePill("Role-aware modules", Color.FromArgb(55, 111, 118), Color.White));
        heroPills.Controls.Add(ClinicTheme.CreatePill("Local reports", Color.FromArgb(55, 111, 118), Color.White));

        var heroTextPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Padding = new Padding(6, 6, 6, 4)
        };
        heroTextPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        heroTextPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        heroTextPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        heroTextPanel.Controls.Add(heroTitle, 0, 0);
        heroTextPanel.Controls.Add(heroSubtitle, 0, 1);
        heroTextPanel.Controls.Add(heroPills, 0, 2);

        heroLayout.Controls.Add(heroTextPanel, 0, 0);
        heroPanel.Controls.Add(heroLayout);

        var statsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            BackColor = ClinicTheme.AppBackground,
            Margin = new Padding(0, 12, 0, 0)
        };
        for (var column = 0; column < 3; column++)
        {
            statsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        }

        statsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        statsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        AddDashboardCard(statsLayout, 0, 0, "Patients", "Registered profiles on file", ClinicTheme.Brand);
        AddDashboardCard(statsLayout, 1, 0, "Appointments Today", "Visits scheduled for today", ClinicTheme.Accent);
        AddDashboardCard(statsLayout, 2, 0, "Consultations", "Documented medical encounters", ClinicTheme.Success);
        AddDashboardCard(statsLayout, 0, 1, "Unpaid Bills", "Open or partial settlements", ClinicTheme.Danger);
        AddDashboardCard(statsLayout, 1, 1, "Medicines", "Items tracked in inventory", ClinicTheme.BrandDark);
        AddDashboardCard(statsLayout, 2, 1, "Low Stock Items", "Restock attention needed", ClinicTheme.Accent);

        root.Controls.Add(heroPanel, 0, 0);
        root.Controls.Add(statsLayout, 0, 1);
        page.Controls.Add(root);

        void ApplyDashboardLayout()
        {
            var compact = page.ClientSize.Height <= 560;

            root.Padding = compact ? new Padding(12) : new Padding(14, 14, 14, 14);
            root.RowStyles[0].Height = compact ? 176 : 188;

            heroPanel.Padding = compact ? new Padding(22, 18, 22, 16) : new Padding(26, 20, 26, 18);
            heroTextPanel.Padding = compact ? new Padding(4, 4, 4, 2) : new Padding(6, 6, 6, 4);
            heroTitle.Font = compact ? new Font("Bahnschrift SemiBold", 24f, FontStyle.Bold) : ClinicTheme.DisplayLarge;
            heroTitle.MaximumSize = new Size(compact ? 820 : 920, 0);
            heroSubtitle.MaximumSize = new Size(compact ? 840 : 920, 0);
            heroPills.Visible = !compact;
            statsLayout.Margin = compact ? new Padding(0, 12, 0, 0) : new Padding(0, 14, 0, 0);
        }

        page.Resize += (_, _) => ApplyDashboardLayout();
        ApplyDashboardLayout();
        return page;
    }

    private void AddDashboardCard(TableLayoutPanel layout, int column, int row, string title, string subtitle, Color accentColor)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(7),
            Padding = new Padding(20, 14, 20, 12)
        };
        ClinicTheme.StyleCard(panel, ClinicTheme.Surface, 24);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = ClinicTheme.Surface
        };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var headerPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ClinicTheme.Surface
        };
        var accentBar = new Panel
        {
            Width = 38,
            Height = 4,
            BackColor = accentColor,
            Location = new Point(0, 0)
        };
        ClinicTheme.RoundControl(accentBar, 2);
        var titleLabel = new Label
        {
            AutoSize = true,
            Text = title,
            Font = ClinicTheme.BodyBold,
            ForeColor = ClinicTheme.TextSecondary,
            Location = new Point(0, 12)
        };
        headerPanel.Controls.Add(accentBar);
        headerPanel.Controls.Add(titleLabel);

        var valueLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = "0",
            Font = new Font("Bahnschrift SemiBold", 30f, FontStyle.Bold),
            ForeColor = ClinicTheme.TextPrimary,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0)
        };

        content.Controls.Add(headerPanel, 0, 0);
        content.Controls.Add(valueLabel, 0, 1);
        panel.Controls.Add(content);
        dashboardMetrics[title] = valueLabel;
        layout.Controls.Add(panel, column, row);
    }

    private static (Panel Panel, Label ValueLabel) CreateModuleMetricCard(string title, string subtitle, Color accent)
    {
        _ = subtitle;

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(4),
            Padding = new Padding(10, 6, 10, 6)
        };
        ClinicTheme.StyleCard(panel, ClinicTheme.SurfaceRaised, 22);

        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 24,
            BackColor = ClinicTheme.SurfaceRaised
        };
        var accentBar = new Panel
        {
            Width = 28,
            Height = 3,
            BackColor = accent,
            Location = new Point(0, 1)
        };
        ClinicTheme.RoundControl(accentBar, 2);
        var titleLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = title,
            Font = ClinicTheme.Caption,
            ForeColor = ClinicTheme.TextSecondary,
            TextAlign = ContentAlignment.TopLeft,
            Padding = new Padding(0, 7, 0, 0)
        };
        headerPanel.Controls.Add(accentBar);
        headerPanel.Controls.Add(titleLabel);

        var valueLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = "0",
            TextAlign = ContentAlignment.BottomLeft,
            Padding = new Padding(0, 0, 0, 3),
            Font = new Font("Bahnschrift SemiBold", 15f, FontStyle.Bold),
            ForeColor = ClinicTheme.TextPrimary,
            Margin = new Padding(0)
        };

        panel.Controls.Add(valueLabel);
        panel.Controls.Add(headerPanel);
        return (panel, valueLabel);
    }

    private TabPage BuildPatientsTab()
    {
        var allowEdit = HasAnyRole(UserRole.Administrator, UserRole.Receptionist);
        var page = new TabPage("Patients") { BackColor = ClinicTheme.AppBackground };
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12),
            BackColor = ClinicTheme.AppBackground
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 206));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var heroCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(26, 22, 26, 22),
            Margin = new Padding(8, 8, 8, 10)
        };
        ClinicTheme.StyleCard(heroCard, ClinicTheme.Surface, 28);

        var heroLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = ClinicTheme.Surface
        };
        heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));

        var introPanel = new Panel { Dock = DockStyle.Fill, BackColor = ClinicTheme.Surface };
        var introAccent = new Panel
        {
            Width = 52,
            Height = 7,
            BackColor = ClinicTheme.Brand,
            Location = new Point(0, 6)
        };
        ClinicTheme.RoundControl(introAccent, 3);
        var heroTitle = new Label
        {
            AutoSize = true,
            Text = "Patient Registry",
            Font = ClinicTheme.DisplayMedium,
            ForeColor = ClinicTheme.TextPrimary,
            Location = new Point(0, 28)
        };
        var heroSubtitle = new Label
        {
            AutoSize = true,
            Text = "Register profiles, review demographic details, and keep a clean front-desk view of who is in the system and how active each record has become.",
            Font = ClinicTheme.Body,
            ForeColor = ClinicTheme.TextSecondary,
            MaximumSize = new Size(470, 0),
            Location = new Point(0, 72)
        };
        var heroPills = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = ClinicTheme.Surface,
            Location = new Point(0, 142)
        };
        heroPills.Controls.Add(ClinicTheme.CreatePill("Searchable records", ClinicTheme.AccentSoft, ClinicTheme.BrandDark));
        heroPills.Controls.Add(ClinicTheme.CreatePill("Live activity counts", ClinicTheme.SurfaceMuted, ClinicTheme.BrandDark));
        heroPills.Controls.Add(ClinicTheme.CreatePill("History-linked profiles", ClinicTheme.SuccessSoft, ClinicTheme.Success));
        introPanel.Controls.Add(introAccent);
        introPanel.Controls.Add(heroTitle);
        introPanel.Controls.Add(heroSubtitle);
        introPanel.Controls.Add(heroPills);

        var metricGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(18, 2, 0, 0),
            BackColor = ClinicTheme.Surface
        };
        metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        metricGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        metricGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        (Panel Panel, Label ValueLabel) CreateMetricCard(string title, string subtitle, Color accent)
        {
            return CreateModuleMetricCard(title, subtitle, accent);
        }

        var totalPatientsCard = CreateMetricCard("Total Patients", "All registered profiles", ClinicTheme.Brand);
        var newThisMonthCard = CreateMetricCard("New This Month", "Recent patient registrations", ClinicTheme.Accent);
        var activeRecordsCard = CreateMetricCard("With Clinical History", "Profiles with consultations", ClinicTheme.Success);
        var seniorsCard = CreateMetricCard("Senior Patients", "Age 60 and above", ClinicTheme.Danger);
        metricGrid.Controls.Add(totalPatientsCard.Panel, 0, 0);
        metricGrid.Controls.Add(newThisMonthCard.Panel, 1, 0);
        metricGrid.Controls.Add(activeRecordsCard.Panel, 0, 1);
        metricGrid.Controls.Add(seniorsCard.Panel, 1, 1);

        heroLayout.Controls.Add(introPanel, 0, 0);
        heroLayout.Controls.Add(metricGrid, 1, 0);
        heroCard.Controls.Add(heroLayout);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel2,
            SplitterDistance = 820,
            SplitterWidth = 10,
            BackColor = ClinicTheme.AppBackground
        };
        split.Panel1.Padding = new Padding(8, 0, 10, 8);
        split.Panel2.Padding = new Padding(10, 0, 8, 8);

        var leftCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 16, 18, 18)
        };
        ClinicTheme.StyleCard(leftCard, ClinicTheme.Surface, 28);

        var leftLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = ClinicTheme.Surface
        };
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var toolbarModel = CreateFilterToolbar("Search by name, ID, contact, or address...", includeCrudButtons: true, allowEdit: allowEdit);
        var toolbar = toolbarModel.Host;
        var searchBox = toolbarModel.SearchBox;
        var newButton = toolbarModel.NewButton!;
        var editButton = toolbarModel.EditButton!;
        var deleteButton = toolbarModel.DeleteButton!;

        var bindingSource = new BindingSource();
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            DataSource = bindingSource,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false
        };
        ClinicTheme.StyleGrid(grid);
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Patient.FullName),
            HeaderText = "Patient",
            FillWeight = 170
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Patient.PatientId),
            HeaderText = "ID",
            FillWeight = 80
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Patient.Age),
            HeaderText = "Age",
            FillWeight = 50
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Patient.Sex),
            HeaderText = "Sex",
            FillWeight = 60
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Patient.ContactNumber),
            HeaderText = "Contact",
            FillWeight = 110
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Patient.DateRegistered),
            HeaderText = "Registered",
            FillWeight = 90,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "d" }
        });

        leftLayout.Controls.Add(toolbar, 0, 0);
        leftLayout.Controls.Add(grid, 0, 1);
        leftCard.Controls.Add(leftLayout);

        var detailCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 18, 18, 18)
        };
        ClinicTheme.StyleCard(detailCard, ClinicTheme.Surface, 28);

        var detailLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = ClinicTheme.Surface
        };
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 176));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var profilePanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 18, 18, 18),
            Margin = new Padding(0, 0, 0, 12)
        };
        ClinicTheme.StyleCard(profilePanel, ClinicTheme.SurfaceRaised, 24);

        var initialsBadge = new Panel
        {
            Size = new Size(78, 78),
            Location = new Point(18, 18),
            BackColor = ClinicTheme.Accent
        };
        ClinicTheme.RoundControl(initialsBadge, 24);
        var initialsLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "--",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Bahnschrift SemiBold", 24f, FontStyle.Bold),
            ForeColor = ClinicTheme.BrandDark
        };
        initialsBadge.Controls.Add(initialsLabel);

        var detailNameLabel = new Label
        {
            AutoSize = true,
            Text = "Select a patient",
            Font = ClinicTheme.Heading,
            ForeColor = ClinicTheme.TextPrimary,
            Location = new Point(112, 24)
        };
        var detailIdLabel = new Label
        {
            AutoSize = true,
            Text = "No record selected",
            Font = ClinicTheme.Body,
            ForeColor = ClinicTheme.TextSecondary,
            Location = new Point(112, 54)
        };
        var pillRow = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = ClinicTheme.SurfaceRaised,
            Location = new Point(112, 90)
        };
        var sexPill = ClinicTheme.CreatePill("Sex: -", ClinicTheme.SurfaceMuted, ClinicTheme.BrandDark);
        var agePill = ClinicTheme.CreatePill("Age: -", ClinicTheme.AccentSoft, ClinicTheme.BrandDark);
        var registeredPill = ClinicTheme.CreatePill("Registered: -", ClinicTheme.SuccessSoft, ClinicTheme.Success);
        pillRow.Controls.Add(sexPill);
        pillRow.Controls.Add(agePill);
        pillRow.Controls.Add(registeredPill);

        var detailHeroText = new Label
        {
            AutoSize = true,
            Text = "This panel tracks the selected patient's contact data and operational activity.",
            Font = ClinicTheme.Caption,
            ForeColor = ClinicTheme.TextSecondary,
            MaximumSize = new Size(300, 0),
            Location = new Point(18, 112)
        };

        profilePanel.Controls.Add(initialsBadge);
        profilePanel.Controls.Add(detailNameLabel);
        profilePanel.Controls.Add(detailIdLabel);
        profilePanel.Controls.Add(pillRow);
        profilePanel.Controls.Add(detailHeroText);

        Panel CreateDetailSection(string title, out Label valueLabel)
        {
            var sectionPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 16, 18, 16),
                Margin = new Padding(0, 0, 0, 12)
            };
            ClinicTheme.StyleCard(sectionPanel, ClinicTheme.SurfaceRaised, 22);

            var titleLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 24,
                Text = title,
                Font = ClinicTheme.BodyBold,
                ForeColor = ClinicTheme.TextPrimary
            };
            valueLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = ClinicTheme.Body,
                ForeColor = ClinicTheme.TextSecondary
            };

            var bodyHost = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 0),
                BackColor = Color.Transparent
            };
            bodyHost.Controls.Add(valueLabel);
            sectionPanel.Controls.Add(bodyHost);
            sectionPanel.Controls.Add(titleLabel);
            return sectionPanel;
        }

        var contactPanel = CreateDetailSection("Contact & Address", out var detailContactLabel);
        var activityPanel = CreateDetailSection("Care Activity", out var detailActivityLabel);
        var recentPanel = CreateDetailSection("Recent Context", out var detailRecentLabel);

        detailLayout.Controls.Add(profilePanel, 0, 0);
        detailLayout.Controls.Add(contactPanel, 0, 1);
        detailLayout.Controls.Add(activityPanel, 0, 2);
        detailLayout.Controls.Add(recentPanel, 0, 3);
        detailCard.Controls.Add(detailLayout);

        split.Panel1.Controls.Add(leftCard);
        split.Panel2.Controls.Add(detailCard);

        Patient? SelectedPatient()
        {
            return grid.CurrentRow?.DataBoundItem as Patient;
        }

        bool CanDeletePatient(Patient patient)
        {
            return !context.Data.Appointments.Any(appointment => appointment.PatientId == patient.PatientId)
                && !context.Data.Consultations.Any(consultation => consultation.PatientId == patient.PatientId)
                && !context.Data.PrescriptionItems.Any(item => item.PatientId == patient.PatientId)
                && !context.Data.BillingRecords.Any(billing => billing.PatientId == patient.PatientId);
        }

        string BuildRecentPatientContext(Patient patient)
        {
            var latestAppointment = context.Data.Appointments
                .Where(appointment => appointment.PatientId == patient.PatientId)
                .OrderByDescending(appointment => appointment.AppointmentDate)
                .ThenByDescending(appointment => appointment.AppointmentTime)
                .FirstOrDefault();
            var latestConsultation = context.Data.Consultations
                .Where(consultation => consultation.PatientId == patient.PatientId)
                .OrderByDescending(consultation => consultation.DateOfVisit)
                .FirstOrDefault();
            var latestBilling = context.Data.BillingRecords
                .Where(record => record.PatientId == patient.PatientId)
                .OrderByDescending(record => record.BillingId)
                .FirstOrDefault();

            return string.Join(
                Environment.NewLine,
                new[]
                {
                    latestAppointment is null
                        ? "Latest appointment: none recorded"
                        : $"Latest appointment: {latestAppointment.AppointmentDate:d} {latestAppointment.AppointmentTime} ({latestAppointment.Status})",
                    latestConsultation is null
                        ? "Latest consultation: none recorded"
                        : $"Latest consultation: {latestConsultation.DateOfVisit:d} | {latestConsultation.Diagnosis}",
                    latestBilling is null
                        ? "Latest billing: none recorded"
                        : $"Latest billing: {latestBilling.BillingId} | {latestBilling.PaymentStatus} | {latestBilling.TotalAmount:C}"
                });
        }

        void RefreshPatientMetrics()
        {
            totalPatientsCard.ValueLabel.Text = context.Data.Patients.Count.ToString();
            newThisMonthCard.ValueLabel.Text = context.Data.Patients.Count(patient =>
                patient.DateRegistered.Month == DateTime.Today.Month &&
                patient.DateRegistered.Year == DateTime.Today.Year).ToString();
            activeRecordsCard.ValueLabel.Text = context.Data.Patients.Count(patient =>
                context.Data.Consultations.Any(consultation => consultation.PatientId == patient.PatientId)).ToString();
            seniorsCard.ValueLabel.Text = context.Data.Patients.Count(patient => patient.Age >= 60).ToString();
        }

        void RefreshPatientDetail()
        {
            var patient = SelectedPatient();
            if (patient is null)
            {
                initialsLabel.Text = "--";
                detailNameLabel.Text = "Select a patient";
                detailIdLabel.Text = "No record selected";
                sexPill.Text = "Sex: -";
                agePill.Text = "Age: -";
                registeredPill.Text = "Registered: -";
                detailContactLabel.Text = "Choose a patient from the table to inspect demographic and contact information.";
                detailActivityLabel.Text = "Appointments: -\nConsultations: -\nBilling records: -";
                detailRecentLabel.Text = "Recent activity will appear here once a patient is selected.";
                return;
            }

            var appointments = context.Data.Appointments.Count(appointment => appointment.PatientId == patient.PatientId);
            var consultations = context.Data.Consultations.Count(consultation => consultation.PatientId == patient.PatientId);
            var billingCount = context.Data.BillingRecords.Count(record => record.PatientId == patient.PatientId);
            var unpaidCount = context.Data.BillingRecords.Count(record => record.PatientId == patient.PatientId && record.PaymentStatus != PaymentStatus.Paid);

            initialsLabel.Text = BuildPatientInitials(patient);
            detailNameLabel.Text = patient.FullName;
            detailIdLabel.Text = $"{patient.PatientId}  |  Added {patient.DateRegistered:d}";
            sexPill.Text = $"Sex: {patient.Sex}";
            agePill.Text = $"Age: {patient.Age}";
            registeredPill.Text = $"Registered: {patient.DateRegistered:MMM d, yyyy}";
            detailContactLabel.Text = $"Phone: {patient.ContactNumber}{Environment.NewLine}Address: {patient.Address}";
            detailActivityLabel.Text = string.Join(
                Environment.NewLine,
                new[]
                {
                    $"Appointments: {appointments}",
                    $"Consultations: {consultations}",
                    $"Billing records: {billingCount}",
                    $"Outstanding bills: {unpaidCount}"
                });
            detailRecentLabel.Text = BuildRecentPatientContext(patient);
        }

        void ApplyFilter()
        {
            var term = searchBox.Text.Trim();
            bindingSource.DataSource = string.IsNullOrWhiteSpace(term)
                ? context.Data.Patients
                : context.Data.Patients.Where(patient =>
                    $"{patient.PatientId} {patient.FirstName} {patient.LastName} {patient.ContactNumber} {patient.Address}".Contains(term, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            if (grid.Rows.Count > 0)
            {
                grid.ClearSelection();
                grid.Rows[0].Selected = true;
                grid.CurrentCell = grid.Rows[0].Cells[0];
            }

            RefreshPatientMetrics();
            RefreshPatientDetail();
        }

        void SaveSelection(string message, Patient? selected = null)
        {
            PersistAndRefresh(message);
            if (selected is not null)
            {
                SelectItem(bindingSource, grid, searchBox, selected, ApplyFilter);
            }

            RefreshPatientMetrics();
            RefreshPatientDetail();
        }

        void CreatePatient()
        {
            var patient = new Patient
            {
                DateRegistered = DateTime.Today,
                Birthdate = DateTime.Today.AddYears(-18),
                Sex = "Unspecified"
            };

            if (!EditorForms.EditPatient(this, patient))
            {
                return;
            }

            patient.PatientId = IdGenerator.Next("PAT", context.Data.Patients.Select(entry => entry.PatientId));
            context.Data.Patients.Add(patient);
            lookupService.SyncPatientReferences(patient);
            SaveSelection("Patient record saved.", patient);
        }

        void EditSelected()
        {
            if (!allowEdit)
            {
                return;
            }

            var patient = SelectedPatient();
            if (patient is null || !EditorForms.EditPatient(this, patient))
            {
                return;
            }

            lookupService.SyncPatientReferences(patient);
            SaveSelection("Patient record saved.", patient);
        }

        void DeleteSelected()
        {
            var patient = SelectedPatient();
            if (patient is null)
            {
                return;
            }

            if (!CanDeletePatient(patient))
            {
                MessageBox.Show(this, "Cannot delete a patient that is already linked to appointments, consultations, prescriptions, or billing records.", "Delete Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show(this, $"Delete patient {patient.FullName}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            context.Data.Patients.Remove(patient);
            SaveSelection("Patient record deleted.");
        }

        searchBox.TextChanged += (_, _) => ApplyFilter();
        newButton.Click += (_, _) => CreatePatient();
        editButton.Click += (_, _) => EditSelected();
        deleteButton.Click += (_, _) => DeleteSelected();
        if (allowEdit)
        {
            grid.CellDoubleClick += (_, _) => EditSelected();
        }
        grid.SelectionChanged += (_, _) => RefreshPatientDetail();
        refreshBindings.Add(ApplyFilter);

        root.Controls.Add(heroCard, 0, 0);
        root.Controls.Add(split, 0, 1);
        page.Controls.Add(root);

        RefreshPatientMetrics();
        ApplyFilter();
        return page;
    }

    private TabPage BuildPatientHistoryTab()
    {
        var page = new TabPage("Patient History") { BackColor = ClinicTheme.AppBackground };
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12),
            BackColor = ClinicTheme.AppBackground
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 170));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var heroCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 18, 24, 16),
            Margin = new Padding(8, 6, 8, 8)
        };
        ClinicTheme.StyleCard(heroCard, ClinicTheme.Surface, 28);

        var heroLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = ClinicTheme.Surface
        };
        heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));

        var introPanel = new Panel { Dock = DockStyle.Fill, BackColor = ClinicTheme.Surface };
        var introAccent = new Panel
        {
            Width = 52,
            Height = 7,
            BackColor = ClinicTheme.BrandDark,
            Location = new Point(0, 6)
        };
        ClinicTheme.RoundControl(introAccent, 3);
        var heroTitle = new Label
        {
            AutoSize = true,
            Text = "Patient History",
            Font = ClinicTheme.DisplayMedium,
            ForeColor = ClinicTheme.TextPrimary,
            Location = new Point(0, 24)
        };
        var heroSubtitle = new Label
        {
            AutoSize = true,
            Text = "Review each patient as a complete story, from appointments and consultations to prescriptions and billing, without hopping between modules.",
            Font = ClinicTheme.Body,
            ForeColor = ClinicTheme.TextSecondary,
            MaximumSize = new Size(540, 0),
            Location = new Point(0, 62)
        };
        introPanel.Controls.Add(introAccent);
        introPanel.Controls.Add(heroTitle);
        introPanel.Controls.Add(heroSubtitle);

        (Panel Panel, Label ValueLabel) CreateMetricCard(string title, Color accent)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                Padding = new Padding(10, 6, 10, 6)
            };
            ClinicTheme.StyleCard(panel, ClinicTheme.SurfaceRaised, 22);

            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 16,
                BackColor = ClinicTheme.SurfaceRaised
            };
            var accentBar = new Panel
            {
                Width = 28,
                Height = 3,
                BackColor = accent,
                Location = new Point(0, 0)
            };
            ClinicTheme.RoundControl(accentBar, 2);
            var titleLabel = new Label
            {
                AutoSize = true,
                Text = title,
                Font = ClinicTheme.Caption,
                ForeColor = ClinicTheme.TextSecondary,
                Location = new Point(0, 4)
            };
            headerPanel.Controls.Add(accentBar);
            headerPanel.Controls.Add(titleLabel);

            var valueLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Bottom,
                Height = 24,
                Text = "0",
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0, 0, 0, 1),
                Font = new Font("Bahnschrift SemiBold", 15f, FontStyle.Bold),
                ForeColor = ClinicTheme.TextPrimary,
                Margin = new Padding(0)
            };

            panel.Controls.Add(valueLabel);
            panel.Controls.Add(headerPanel);
            return (panel, valueLabel);
        }

        var metricGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(14, 0, 0, 0),
            BackColor = ClinicTheme.Surface
        };
        metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        metricGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        metricGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var totalCard = CreateMetricCard("Total Patients", ClinicTheme.Brand);
        var visitsCard = CreateMetricCard("Consultations", ClinicTheme.Success);
        var billingCard = CreateMetricCard("Open Bills", ClinicTheme.Danger);
        var activeCard = CreateMetricCard("Active Today", ClinicTheme.Accent);
        metricGrid.Controls.Add(totalCard.Panel, 0, 0);
        metricGrid.Controls.Add(visitsCard.Panel, 1, 0);
        metricGrid.Controls.Add(billingCard.Panel, 0, 1);
        metricGrid.Controls.Add(activeCard.Panel, 1, 1);

        heroLayout.Controls.Add(introPanel, 0, 0);
        heroLayout.Controls.Add(metricGrid, 1, 0);
        heroCard.Controls.Add(heroLayout);

        var contentLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = ClinicTheme.AppBackground
        };
        contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var leftCard = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 0, 8, 8),
            Padding = new Padding(18, 16, 18, 18)
        };
        ClinicTheme.StyleCard(leftCard, ClinicTheme.Surface, 28);

        var leftLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = ClinicTheme.Surface
        };
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var toolbarModel = CreateFilterToolbar("Search by patient, contact, address, or ID...", includeCrudButtons: false, allowEdit: false);
        var toolbar = toolbarModel.Host;
        var searchBox = toolbarModel.SearchBox;

        var bindingSource = new BindingSource();
        var patientGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            DataSource = bindingSource,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false
        };
        ClinicTheme.StyleGrid(patientGrid);
        patientGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Patient.FullName),
            HeaderText = "Patient",
            FillWeight = 142
        });
        patientGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Patient.ContactNumber),
            HeaderText = "Contact",
            FillWeight = 132
        });
        patientGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Patient.PatientId),
            HeaderText = "ID",
            FillWeight = 84
        });

        leftLayout.Controls.Add(toolbar, 0, 0);
        leftLayout.Controls.Add(patientGrid, 0, 1);
        leftCard.Controls.Add(leftLayout);

        var detailCard = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 0, 8, 8),
            Padding = new Padding(18, 18, 18, 18),
            AutoScroll = true
        };
        ClinicTheme.StyleCard(detailCard, ClinicTheme.Surface, 28);
        var initialScrollResetApplied = false;

        var detailLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = ClinicTheme.Surface,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        detailLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var profilePanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 188,
            Padding = new Padding(16, 14, 16, 12),
            Margin = new Padding(0, 0, 0, 12)
        };
        ClinicTheme.StyleCard(profilePanel, ClinicTheme.SurfaceRaised, 24);

        var initialsBadge = new Panel
        {
            Size = new Size(72, 72),
            BackColor = ClinicTheme.Brand
        };
        ClinicTheme.RoundControl(initialsBadge, 28);
        var initialsLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "--",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Bahnschrift SemiBold", 18f, FontStyle.Bold),
            ForeColor = Color.White
        };
        initialsBadge.Controls.Add(initialsLabel);

        var detailPatientLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 34,
            AutoEllipsis = true,
            Text = "Select a patient",
            Font = ClinicTheme.Heading,
            ForeColor = ClinicTheme.TextPrimary,
            Margin = new Padding(0, 0, 0, 2)
        };
        var detailIdLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 24,
            AutoEllipsis = true,
            Text = "No patient selected",
            Font = ClinicTheme.Body,
            ForeColor = ClinicTheme.TextSecondary,
            Margin = new Padding(0, 0, 0, 6)
        };
        var pillRow = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = ClinicTheme.SurfaceRaised,
            Margin = new Padding(0)
        };
        var demographicsPill = ClinicTheme.CreatePill("Age / Sex: -", ClinicTheme.SurfaceMuted, ClinicTheme.BrandDark);
        var registeredPill = ClinicTheme.CreatePill("Registered: -", ClinicTheme.AccentSoft, ClinicTheme.BrandDark);
        var visitsPill = ClinicTheme.CreatePill("Visits: -", ClinicTheme.SuccessSoft, ClinicTheme.Success);
        var billsPill = ClinicTheme.CreatePill("Open Bills: -", ClinicTheme.DangerSoft, ClinicTheme.Danger);
        pillRow.Controls.Add(demographicsPill);
        pillRow.Controls.Add(registeredPill);
        pillRow.Controls.Add(visitsPill);
        pillRow.Controls.Add(billsPill);

        var detailHeroText = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            Text = "This panel combines the patient's operational snapshot with the full chronology shown below.",
            Font = ClinicTheme.Caption,
            ForeColor = ClinicTheme.TextSecondary,
            Margin = new Padding(0, 8, 0, 0)
        };

        var badgeHost = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            BackColor = ClinicTheme.SurfaceRaised,
            Margin = new Padding(0),
            Padding = new Padding(0, 2, 10, 0)
        };
        initialsBadge.Margin = new Padding(0);
        badgeHost.Controls.Add(initialsBadge);

        var summaryLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = ClinicTheme.SurfaceRaised,
            Margin = new Padding(0)
        };
        summaryLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        summaryLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        summaryLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        summaryLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        summaryLayout.Controls.Add(detailPatientLabel, 0, 0);
        summaryLayout.Controls.Add(detailIdLabel, 0, 1);
        summaryLayout.Controls.Add(pillRow, 0, 2);
        summaryLayout.Controls.Add(detailHeroText, 0, 3);

        var profileLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = ClinicTheme.SurfaceRaised
        };
        profileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84));
        profileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        profileLayout.Controls.Add(badgeHost, 0, 0);
        profileLayout.Controls.Add(summaryLayout, 1, 0);
        profilePanel.Controls.Add(profileLayout);

        Panel CreateDetailSection(string title, Color accent, out Label valueLabel)
        {
            var sectionPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 132,
                Padding = new Padding(18, 16, 18, 16),
                Margin = new Padding(0, 0, 0, 12)
            };
            ClinicTheme.StyleCard(sectionPanel, ClinicTheme.SurfaceRaised, 22);

            var accentBar = new Panel
            {
                Width = 34,
                Height = 4,
                BackColor = accent,
                Location = new Point(18, 16)
            };
            ClinicTheme.RoundControl(accentBar, 2);

            var titleLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 30,
                Text = title,
                Font = ClinicTheme.BodyBold,
                ForeColor = ClinicTheme.TextPrimary,
                Padding = new Padding(0, 10, 0, 0)
            };
            valueLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = ClinicTheme.Body,
                ForeColor = ClinicTheme.TextSecondary
            };

            var bodyHost = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 0),
                BackColor = Color.Transparent
            };
            bodyHost.Controls.Add(valueLabel);
            sectionPanel.Controls.Add(accentBar);
            sectionPanel.Controls.Add(bodyHost);
            sectionPanel.Controls.Add(titleLabel);
            return sectionPanel;
        }

        var visitPanel = CreateDetailSection("Visit Snapshot", ClinicTheme.Brand, out var visitSummaryLabel);
        var financePanel = CreateDetailSection("Billing Snapshot", ClinicTheme.Danger, out var financeSummaryLabel);

        var historyHost = new Panel
        {
            Dock = DockStyle.Top,
            Margin = new Padding(0),
            Height = 320
        };
        ClinicTheme.StyleCard(historyHost, ClinicTheme.SurfaceRaised, 22);

        var historyLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(18, 16, 18, 16),
            BackColor = ClinicTheme.SurfaceRaised
        };
        historyLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        historyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var historyHeader = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = ClinicTheme.SurfaceRaised
        };
        historyHeader.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        historyHeader.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var headingRow = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ClinicTheme.SurfaceRaised
        };
        var historyAccent = new Panel
        {
            Width = 34,
            Height = 4,
            BackColor = ClinicTheme.Success,
            Location = new Point(0, 2)
        };
        ClinicTheme.RoundControl(historyAccent, 2);
        var historyTitleLabel = new Label
        {
            AutoSize = true,
            Text = "Prescription And Full History",
            Font = ClinicTheme.BodyBold,
            ForeColor = ClinicTheme.TextPrimary,
            Location = new Point(0, 12)
        };
        var prescriptionSummaryLabel = new Label
        {
            AutoSize = true,
            Text = "Prescription totals and the complete visit chronology will appear here.",
            Font = ClinicTheme.Body,
            ForeColor = ClinicTheme.TextSecondary,
            Margin = new Padding(0, 2, 0, 0)
        };
        void SyncPrescriptionSummaryWrapping()
        {
            prescriptionSummaryLabel.MaximumSize = new Size(Math.Max(140, historyHeader.ClientSize.Width - 6), 0);
        }

        historyHeader.Resize += (_, _) => SyncPrescriptionSummaryWrapping();
        headingRow.Controls.Add(historyAccent);
        headingRow.Controls.Add(historyTitleLabel);
        historyHeader.Controls.Add(headingRow, 0, 0);
        historyHeader.Controls.Add(prescriptionSummaryLabel, 0, 1);
        SyncPrescriptionSummaryWrapping();

        var historyBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            DetectUrls = false,
            WordWrap = true,
            ScrollBars = RichTextBoxScrollBars.Vertical
        };
        ClinicTheme.StyleRichText(historyBox);
        historyBox.Font = ClinicTheme.Body;
        historyBox.BackColor = ClinicTheme.SurfaceRaised;

        historyLayout.Controls.Add(historyHeader, 0, 0);
        historyLayout.Controls.Add(historyBox, 0, 1);
        historyHost.Controls.Add(historyLayout);

        detailLayout.Controls.Add(profilePanel, 0, 0);
        detailLayout.Controls.Add(visitPanel, 0, 1);
        detailLayout.Controls.Add(financePanel, 0, 2);
        detailLayout.Controls.Add(historyHost, 0, 3);
        detailCard.Controls.Add(detailLayout);

        contentLayout.Controls.Add(leftCard, 0, 0);
        contentLayout.Controls.Add(detailCard, 1, 0);

        Patient? SelectedPatient()
        {
            return patientGrid.CurrentRow?.DataBoundItem as Patient;
        }

        static string ValueOrPlaceholder(string value, string placeholder)
        {
            return string.IsNullOrWhiteSpace(value) ? placeholder : value.Trim();
        }

        DateTime PatientActivityDate(Patient patient)
        {
            var latestAppointment = context.Data.Appointments
                .Where(appointment => appointment.PatientId == patient.PatientId)
                .Select(appointment => appointment.AppointmentDate.Date)
                .DefaultIfEmpty(patient.DateRegistered.Date)
                .Max();
            var latestConsultation = context.Data.Consultations
                .Where(consultation => consultation.PatientId == patient.PatientId)
                .Select(consultation => consultation.DateOfVisit.Date)
                .DefaultIfEmpty(patient.DateRegistered.Date)
                .Max();

            return latestAppointment > latestConsultation ? latestAppointment : latestConsultation;
        }

        string BuildVisitSummary(Patient patient)
        {
            var appointments = context.Data.Appointments
                .Where(appointment => appointment.PatientId == patient.PatientId)
                .OrderByDescending(appointment => appointment.AppointmentDate)
                .ThenByDescending(appointment => appointment.AppointmentId)
                .ToList();
            var consultations = context.Data.Consultations
                .Where(consultation => consultation.PatientId == patient.PatientId)
                .OrderByDescending(consultation => consultation.DateOfVisit)
                .ToList();
            var latestConsultation = consultations.FirstOrDefault();
            var latestAppointment = appointments.FirstOrDefault();

            return string.Join(
                Environment.NewLine,
                new[]
                {
                    $"Appointments recorded: {appointments.Count}",
                    $"Consultations recorded: {consultations.Count}",
                    latestAppointment is null
                        ? "Latest appointment: none recorded"
                        : $"Latest appointment: {latestAppointment.AppointmentDate:d} at {latestAppointment.AppointmentTime} ({latestAppointment.Status})",
                    latestConsultation is null
                        ? "Latest diagnosis: none recorded"
                        : $"Latest diagnosis: {ValueOrPlaceholder(latestConsultation.Diagnosis, "Not recorded")} ({latestConsultation.DateOfVisit:d})"
                });
        }

        string BuildFinanceSummary(Patient patient)
        {
            var bills = context.Data.BillingRecords
                .Where(record => record.PatientId == patient.PatientId)
                .OrderByDescending(record => record.BillingId)
                .ToList();
            var openBills = bills.Count(record => record.PaymentStatus != PaymentStatus.Paid);
            var paidRevenue = bills.Where(record => record.PaymentStatus == PaymentStatus.Paid).Sum(record => record.TotalAmount);
            var latestBill = bills.FirstOrDefault();

            return string.Join(
                Environment.NewLine,
                new[]
                {
                    $"Billing records: {bills.Count}",
                    $"Open bills: {openBills}",
                    $"Paid revenue from patient: {paidRevenue:C}",
                    latestBill is null
                        ? "Latest bill: none recorded"
                        : $"Latest bill: {latestBill.BillingId} | {latestBill.PaymentStatus} | {latestBill.TotalAmount:C}"
                });
        }

        string BuildPrescriptionSummary(Patient patient)
        {
            var prescriptionItems = context.Data.PrescriptionItems
                .Where(item => item.PatientId == patient.PatientId)
                .OrderByDescending(item => context.Data.Consultations.FirstOrDefault(consultation => consultation.ConsultationId == item.ConsultationId)?.DateOfVisit ?? DateTime.MinValue)
                .ThenBy(item => item.MedicineName)
                .ToList();
            var totalValue = prescriptionItems.Sum(item => item.TotalCost);
            var topMedicines = prescriptionItems
                .GroupBy(item => item.MedicineName, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(group => group.Sum(item => item.Quantity))
                .ThenBy(group => group.Key)
                .Take(3)
                .Select(group => $"{group.Key} ({group.Sum(item => item.Quantity)} unit(s))")
                .ToList();

            return string.Join(
                Environment.NewLine,
                new[]
                {
                    $"Prescription items: {prescriptionItems.Count}",
                    $"Prescription value: {totalValue:C}",
                    topMedicines.Count == 0
                        ? "Common medicines: none recorded"
                        : $"Common medicines: {string.Join(", ", topMedicines)}"
                });
        }

        void RefreshHistoryMetrics()
        {
            totalCard.ValueLabel.Text = context.Data.Patients.Count.ToString();
            visitsCard.ValueLabel.Text = context.Data.Patients.Count(patient => context.Data.Consultations.Any(consultation => consultation.PatientId == patient.PatientId)).ToString();
            billingCard.ValueLabel.Text = context.Data.Patients.Count(patient => context.Data.BillingRecords.Any(record => record.PatientId == patient.PatientId && record.PaymentStatus != PaymentStatus.Paid)).ToString();
            activeCard.ValueLabel.Text = context.Data.Patients.Count(patient =>
                context.Data.Appointments.Any(appointment => appointment.PatientId == patient.PatientId && appointment.AppointmentDate.Date == DateTime.Today)
                || context.Data.Consultations.Any(consultation => consultation.PatientId == patient.PatientId && consultation.DateOfVisit.Date == DateTime.Today)).ToString();
        }

        void RefreshHistoryDetail()
        {
            var patient = SelectedPatient();
            if (patient is null)
            {
                initialsLabel.Text = "--";
                detailPatientLabel.Text = "Select a patient";
                detailIdLabel.Text = "No patient selected";
                demographicsPill.Text = "Age / Sex: -";
                registeredPill.Text = "Registered: -";
                visitsPill.Text = "Visits: -";
                billsPill.Text = "Open Bills: -";
                detailHeroText.Text = "This panel combines the patient's operational snapshot with the full chronology shown below.";
                visitSummaryLabel.Text = "Select a patient to review appointments, consultations, and the most recent diagnosis.";
                financeSummaryLabel.Text = "Billing totals and open account context will appear here.";
                prescriptionSummaryLabel.Text = "Prescription totals and the complete visit chronology will appear here.";
                RenderHistoryPlaceholder(historyBox);
                return;
            }

            var consultationCount = context.Data.Consultations.Count(consultation => consultation.PatientId == patient.PatientId);
            var openBillCount = context.Data.BillingRecords.Count(record => record.PatientId == patient.PatientId && record.PaymentStatus != PaymentStatus.Paid);
            var latestActivity = PatientActivityDate(patient);
            initialsLabel.Text = BuildPatientInitials(patient);
            detailPatientLabel.Text = patient.FullName;
            detailIdLabel.Text = $"{patient.PatientId}  |  {ValueOrPlaceholder(patient.ContactNumber, "No contact number")}";
            demographicsPill.Text = $"Age / Sex: {patient.Age} / {patient.Sex}";
            registeredPill.Text = $"Registered: {patient.DateRegistered:MMM d, yyyy}";
            visitsPill.Text = $"Visits: {consultationCount}";
            billsPill.Text = $"Open Bills: {openBillCount}";
            detailHeroText.Text = latestActivity == patient.DateRegistered.Date && consultationCount == 0
                ? "This patient has demographic registration data but no clinical visit history yet."
                : $"Latest recorded activity: {latestActivity:MMM d, yyyy}. Review the timeline below for the full chronology.";
            visitSummaryLabel.Text = BuildVisitSummary(patient);
            financeSummaryLabel.Text = BuildFinanceSummary(patient);
            prescriptionSummaryLabel.Text = BuildPrescriptionSummary(patient);
            RenderPatientHistoryViewer(historyBox, patient);
        }

        void ApplyFilter()
        {
            var term = searchBox.Text.Trim();
            var filtered = context.Data.Patients
                .Where(patient => string.IsNullOrWhiteSpace(term)
                    || $"{patient.PatientId} {patient.FirstName} {patient.LastName} {patient.ContactNumber} {patient.Address}"
                        .Contains(term, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(PatientActivityDate)
                .ThenBy(patient => patient.LastName)
                .ThenBy(patient => patient.FirstName)
                .ToList();
            bindingSource.DataSource = filtered;

            if (patientGrid.Rows.Count > 0)
            {
                patientGrid.ClearSelection();
                patientGrid.Rows[0].Selected = true;
                patientGrid.CurrentCell = patientGrid.Rows[0].Cells[0];
            }

            RefreshHistoryMetrics();
            RefreshHistoryDetail();

            if (!initialScrollResetApplied)
            {
                initialScrollResetApplied = true;
                detailCard.AutoScrollPosition = new Point(0, 0);
            }
        }

        searchBox.TextChanged += (_, _) => ApplyFilter();
        patientGrid.SelectionChanged += (_, _) => RefreshHistoryDetail();
        page.Enter += (_, _) => detailCard.AutoScrollPosition = new Point(0, 0);
        refreshBindings.Add(ApplyFilter);

        root.Controls.Add(heroCard, 0, 0);
        root.Controls.Add(contentLayout, 0, 1);
        page.Controls.Add(root);
        RefreshHistoryMetrics();
        ApplyFilter();
        return page;
    }

    private TabPage BuildAppointmentsTab()
    {
        var allowEdit = HasAnyRole(UserRole.Administrator, UserRole.Receptionist);
        var page = new TabPage("Appointments") { BackColor = ClinicTheme.AppBackground };
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12),
            BackColor = ClinicTheme.AppBackground
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 206));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var heroCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(26, 22, 26, 22),
            Margin = new Padding(8, 8, 8, 10)
        };
        ClinicTheme.StyleCard(heroCard, ClinicTheme.Surface, 28);

        var heroLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = ClinicTheme.Surface
        };
        heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));

        var introPanel = new Panel { Dock = DockStyle.Fill, BackColor = ClinicTheme.Surface };
        var introAccent = new Panel
        {
            Width = 52,
            Height = 7,
            BackColor = ClinicTheme.Accent,
            Location = new Point(0, 6)
        };
        ClinicTheme.RoundControl(introAccent, 3);
        var heroTitle = new Label
        {
            AutoSize = true,
            Text = "Appointment Desk",
            Font = ClinicTheme.DisplayMedium,
            ForeColor = ClinicTheme.TextPrimary,
            Location = new Point(0, 28)
        };
        var heroSubtitle = new Label
        {
            AutoSize = true,
            Text = "Monitor the clinic schedule, keep doctor assignments visible, and spot which visits are still pending before they become front-desk bottlenecks.",
            Font = ClinicTheme.Body,
            ForeColor = ClinicTheme.TextSecondary,
            MaximumSize = new Size(470, 0),
            Location = new Point(0, 72)
        };
        var heroPills = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = ClinicTheme.Surface,
            Location = new Point(0, 142)
        };
        heroPills.Controls.Add(ClinicTheme.CreatePill("Schedule-first view", ClinicTheme.AccentSoft, ClinicTheme.BrandDark));
        heroPills.Controls.Add(ClinicTheme.CreatePill("Doctor assignment visible", ClinicTheme.SurfaceMuted, ClinicTheme.BrandDark));
        heroPills.Controls.Add(ClinicTheme.CreatePill("Patient-linked visits", ClinicTheme.SuccessSoft, ClinicTheme.Success));
        introPanel.Controls.Add(introAccent);
        introPanel.Controls.Add(heroTitle);
        introPanel.Controls.Add(heroSubtitle);
        introPanel.Controls.Add(heroPills);

        (Panel Panel, Label ValueLabel) CreateMetricCard(string title, string subtitle, Color accent)
        {
            return CreateModuleMetricCard(title, subtitle, accent);
        }

        var metricGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(18, 2, 0, 0),
            BackColor = ClinicTheme.Surface
        };
        metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        metricGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        metricGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var todaysCard = CreateMetricCard("Today's Queue", "Appointments scheduled today", ClinicTheme.Brand);
        var pendingCard = CreateMetricCard("Pending", "Visits still waiting to happen", ClinicTheme.Accent);
        var completedCard = CreateMetricCard("Completed Today", "Visits already handled today", ClinicTheme.Success);
        var cancelledCard = CreateMetricCard("Cancelled", "Appointments marked cancelled", ClinicTheme.Danger);
        metricGrid.Controls.Add(todaysCard.Panel, 0, 0);
        metricGrid.Controls.Add(pendingCard.Panel, 1, 0);
        metricGrid.Controls.Add(completedCard.Panel, 0, 1);
        metricGrid.Controls.Add(cancelledCard.Panel, 1, 1);

        heroLayout.Controls.Add(introPanel, 0, 0);
        heroLayout.Controls.Add(metricGrid, 1, 0);
        heroCard.Controls.Add(heroLayout);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel2,
            SplitterDistance = 820,
            SplitterWidth = 10,
            BackColor = ClinicTheme.AppBackground
        };
        split.Panel1.Padding = new Padding(8, 0, 10, 8);
        split.Panel2.Padding = new Padding(10, 0, 8, 8);

        var leftCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 16, 18, 18)
        };
        ClinicTheme.StyleCard(leftCard, ClinicTheme.Surface, 28);

        var leftLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = ClinicTheme.Surface
        };
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var toolbarModel = CreateFilterToolbar("Search by patient, doctor, date, or ID...", includeCrudButtons: true, allowEdit: allowEdit);
        var toolbar = toolbarModel.Host;
        var searchBox = toolbarModel.SearchBox;
        var newButton = toolbarModel.NewButton!;
        var editButton = toolbarModel.EditButton!;
        var deleteButton = toolbarModel.DeleteButton!;

        var bindingSource = new BindingSource();
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            DataSource = bindingSource,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false
        };
        ClinicTheme.StyleGrid(grid);
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Appointment.AppointmentDate),
            HeaderText = "Date",
            FillWeight = 80,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "d" }
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Appointment.AppointmentTime),
            HeaderText = "Time",
            FillWeight = 70
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Appointment.PatientName),
            HeaderText = "Patient",
            FillWeight = 165
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Appointment.DoctorAssigned),
            HeaderText = "Doctor",
            FillWeight = 150
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Appointment.Status),
            HeaderText = "Status",
            FillWeight = 80
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Appointment.AppointmentId),
            HeaderText = "ID",
            FillWeight = 72
        });

        leftLayout.Controls.Add(toolbar, 0, 0);
        leftLayout.Controls.Add(grid, 0, 1);
        leftCard.Controls.Add(leftLayout);

        var detailCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 18, 18, 18)
        };
        ClinicTheme.StyleCard(detailCard, ClinicTheme.Surface, 28);

        var detailLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = ClinicTheme.Surface
        };
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 184));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var profilePanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 18, 18, 18),
            Margin = new Padding(0, 0, 0, 12)
        };
        ClinicTheme.StyleCard(profilePanel, ClinicTheme.SurfaceRaised, 24);

        var timeBadge = new Panel
        {
            Size = new Size(92, 92),
            Location = new Point(18, 18),
            BackColor = ClinicTheme.Brand
        };
        ClinicTheme.RoundControl(timeBadge, 28);
        var timeBadgeLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "--",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Bahnschrift SemiBold", 17f, FontStyle.Bold),
            ForeColor = Color.White
        };
        timeBadge.Controls.Add(timeBadgeLabel);

        var detailPatientLabel = new Label
        {
            AutoSize = true,
            Text = "Select an appointment",
            Font = ClinicTheme.Heading,
            ForeColor = ClinicTheme.TextPrimary,
            Location = new Point(126, 24)
        };
        var detailIdLabel = new Label
        {
            AutoSize = true,
            Text = "No schedule item selected",
            Font = ClinicTheme.Body,
            ForeColor = ClinicTheme.TextSecondary,
            Location = new Point(126, 54)
        };
        var pillRow = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = ClinicTheme.SurfaceRaised,
            Location = new Point(126, 92)
        };
        var statusPill = ClinicTheme.CreatePill("Status: -", ClinicTheme.SurfaceMuted, ClinicTheme.BrandDark);
        var datePill = ClinicTheme.CreatePill("Date: -", ClinicTheme.AccentSoft, ClinicTheme.BrandDark);
        var doctorPill = ClinicTheme.CreatePill("Doctor: -", ClinicTheme.SuccessSoft, ClinicTheme.Success);
        pillRow.Controls.Add(statusPill);
        pillRow.Controls.Add(datePill);
        pillRow.Controls.Add(doctorPill);

        var detailHeroText = new Label
        {
            AutoSize = true,
            Text = "Use this panel to confirm timing, doctor assignment, and patient context before the visit begins.",
            Font = ClinicTheme.Caption,
            ForeColor = ClinicTheme.TextSecondary,
            MaximumSize = new Size(300, 0),
            Location = new Point(18, 124)
        };

        profilePanel.Controls.Add(timeBadge);
        profilePanel.Controls.Add(detailPatientLabel);
        profilePanel.Controls.Add(detailIdLabel);
        profilePanel.Controls.Add(pillRow);
        profilePanel.Controls.Add(detailHeroText);

        Panel CreateDetailSection(string title, out Label valueLabel)
        {
            var sectionPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 16, 18, 16),
                Margin = new Padding(0, 0, 0, 12)
            };
            ClinicTheme.StyleCard(sectionPanel, ClinicTheme.SurfaceRaised, 22);

            var titleLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 24,
                Text = title,
                Font = ClinicTheme.BodyBold,
                ForeColor = ClinicTheme.TextPrimary
            };
            valueLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = ClinicTheme.Body,
                ForeColor = ClinicTheme.TextSecondary
            };

            var bodyHost = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 0),
                BackColor = Color.Transparent
            };
            bodyHost.Controls.Add(valueLabel);
            sectionPanel.Controls.Add(bodyHost);
            sectionPanel.Controls.Add(titleLabel);
            return sectionPanel;
        }

        var patientPanel = CreateDetailSection("Patient Context", out var detailPatientContextLabel);
        var doctorPanel = CreateDetailSection("Doctor Schedule", out var detailDoctorScheduleLabel);
        var readinessPanel = CreateDetailSection("Visit Readiness", out var detailReadinessLabel);

        detailLayout.Controls.Add(profilePanel, 0, 0);
        detailLayout.Controls.Add(patientPanel, 0, 1);
        detailLayout.Controls.Add(doctorPanel, 0, 2);
        detailLayout.Controls.Add(readinessPanel, 0, 3);
        detailCard.Controls.Add(detailLayout);

        split.Panel1.Controls.Add(leftCard);
        split.Panel2.Controls.Add(detailCard);

        Appointment? SelectedAppointment()
        {
            return grid.CurrentRow?.DataBoundItem as Appointment;
        }

        static int AppointmentTimeKey(string timeText)
        {
            return DateTime.TryParse(timeText, out var parsedTime) ? parsedTime.Hour * 60 + parsedTime.Minute : int.MaxValue;
        }

        void ApplyStatusPill(AppointmentStatus status)
        {
            statusPill.Text = $"Status: {status}";
            switch (status)
            {
                case AppointmentStatus.Completed:
                    statusPill.BackColor = ClinicTheme.SuccessSoft;
                    statusPill.ForeColor = ClinicTheme.Success;
                    break;
                case AppointmentStatus.Cancelled:
                    statusPill.BackColor = ClinicTheme.DangerSoft;
                    statusPill.ForeColor = ClinicTheme.Danger;
                    break;
                default:
                    statusPill.BackColor = ClinicTheme.AccentSoft;
                    statusPill.ForeColor = ClinicTheme.BrandDark;
                    break;
            }
        }

        string BuildPatientContext(Appointment appointment)
        {
            var patient = lookupService.FindPatient(appointment.PatientId);
            if (patient is null)
            {
                return "The linked patient profile could not be found.";
            }

            return string.Join(
                Environment.NewLine,
                new[]
                {
                    $"Patient ID: {patient.PatientId}",
                    $"Contact: {patient.ContactNumber}",
                    $"Address: {patient.Address}",
                    $"Age / Sex: {patient.Age} / {patient.Sex}"
                });
        }

        string BuildDoctorScheduleContext(Appointment appointment)
        {
            var sameDoctorSchedule = context.Data.Appointments
                .Where(item => item.DoctorAssigned == appointment.DoctorAssigned && item.AppointmentDate.Date == appointment.AppointmentDate.Date)
                .OrderBy(item => AppointmentTimeKey(item.AppointmentTime))
                .ToList();

            var lines = new List<string>
            {
                $"Doctor: {appointment.DoctorAssigned}",
                $"Appointments that day: {sameDoctorSchedule.Count}"
            };

            var surrounding = sameDoctorSchedule
                .Take(5)
                .Select(item => $"{item.AppointmentTime} - {item.PatientName} ({item.Status})")
                .ToList();
            lines.Add(surrounding.Count == 0 ? "No other schedule items found." : string.Join(Environment.NewLine, surrounding));
            return string.Join(Environment.NewLine, lines);
        }

        string BuildVisitReadiness(Appointment appointment)
        {
            var consultationCount = context.Data.Consultations.Count(consultation => consultation.PatientId == appointment.PatientId);
            var unpaidBills = context.Data.BillingRecords.Count(record => record.PatientId == appointment.PatientId && record.PaymentStatus != PaymentStatus.Paid);
            var latestConsultation = context.Data.Consultations
                .Where(consultation => consultation.PatientId == appointment.PatientId)
                .OrderByDescending(consultation => consultation.DateOfVisit)
                .FirstOrDefault();

            return string.Join(
                Environment.NewLine,
                new[]
                {
                    $"Prior consultations: {consultationCount}",
                    $"Open bills: {unpaidBills}",
                    latestConsultation is null
                        ? "Latest diagnosis: none on record"
                        : $"Latest diagnosis: {latestConsultation.Diagnosis} ({latestConsultation.DateOfVisit:d})"
                });
        }

        void RefreshAppointmentMetrics()
        {
            todaysCard.ValueLabel.Text = context.Data.Appointments.Count(appointment => appointment.AppointmentDate.Date == DateTime.Today).ToString();
            pendingCard.ValueLabel.Text = context.Data.Appointments.Count(appointment => appointment.Status == AppointmentStatus.Pending).ToString();
            completedCard.ValueLabel.Text = context.Data.Appointments.Count(appointment =>
                appointment.AppointmentDate.Date == DateTime.Today && appointment.Status == AppointmentStatus.Completed).ToString();
            cancelledCard.ValueLabel.Text = context.Data.Appointments.Count(appointment => appointment.Status == AppointmentStatus.Cancelled).ToString();
        }

        void RefreshAppointmentDetail()
        {
            var appointment = SelectedAppointment();
            if (appointment is null)
            {
                timeBadgeLabel.Text = "--";
                detailPatientLabel.Text = "Select an appointment";
                detailIdLabel.Text = "No schedule item selected";
                statusPill.Text = "Status: -";
                statusPill.BackColor = ClinicTheme.SurfaceMuted;
                statusPill.ForeColor = ClinicTheme.BrandDark;
                datePill.Text = "Date: -";
                doctorPill.Text = "Doctor: -";
                detailPatientContextLabel.Text = "Choose an appointment from the schedule to inspect the linked patient information.";
                detailDoctorScheduleLabel.Text = "Doctor load and same-day schedule will appear here.";
                detailReadinessLabel.Text = "Visit readiness notes will appear here.";
                return;
            }

            timeBadgeLabel.Text = appointment.AppointmentTime.Replace(" ", Environment.NewLine);
            detailPatientLabel.Text = appointment.PatientName;
            detailIdLabel.Text = $"{appointment.AppointmentId}  |  {appointment.PatientId}";
            ApplyStatusPill(appointment.Status);
            datePill.Text = $"Date: {appointment.AppointmentDate:MMM d}";
            doctorPill.Text = $"Doctor: {appointment.DoctorAssigned}";
            detailPatientContextLabel.Text = BuildPatientContext(appointment);
            detailDoctorScheduleLabel.Text = BuildDoctorScheduleContext(appointment);
            detailReadinessLabel.Text = BuildVisitReadiness(appointment);
        }

        void ApplyFilter()
        {
            var term = searchBox.Text.Trim();
            var filtered = context.Data.Appointments
                .Where(appointment => string.IsNullOrWhiteSpace(term)
                    || $"{appointment.AppointmentId} {appointment.PatientId} {appointment.PatientName} {appointment.DoctorAssigned} {appointment.Status} {appointment.AppointmentDate:d} {appointment.AppointmentTime}"
                        .Contains(term, StringComparison.OrdinalIgnoreCase))
                .OrderBy(appointment => appointment.AppointmentDate)
                .ThenBy(appointment => AppointmentTimeKey(appointment.AppointmentTime))
                .ToList();
            bindingSource.DataSource = filtered;

            if (grid.Rows.Count > 0)
            {
                grid.ClearSelection();
                grid.Rows[0].Selected = true;
                grid.CurrentCell = grid.Rows[0].Cells[0];
            }

            RefreshAppointmentMetrics();
            RefreshAppointmentDetail();
        }

        void SaveSelection(string message, Appointment? selected = null)
        {
            PersistAndRefresh(message);
            if (selected is not null)
            {
                SelectItem(bindingSource, grid, searchBox, selected, ApplyFilter);
            }

            RefreshAppointmentMetrics();
            RefreshAppointmentDetail();
        }

        void CreateAppointment()
        {
            var appointment = new Appointment
            {
                AppointmentDate = DateTime.Today,
                AppointmentTime = "09:00 AM",
                Status = AppointmentStatus.Pending
            };

            if (!EditorForms.EditAppointment(this, appointment, lookupService))
            {
                return;
            }

            appointment.AppointmentId = IdGenerator.Next("APT", context.Data.Appointments.Select(entry => entry.AppointmentId));
            context.Data.Appointments.Add(appointment);
            SaveSelection("Appointment record saved.", appointment);
        }

        void EditSelected()
        {
            if (!allowEdit)
            {
                return;
            }

            var appointment = SelectedAppointment();
            if (appointment is null || !EditorForms.EditAppointment(this, appointment, lookupService))
            {
                return;
            }

            SaveSelection("Appointment record saved.", appointment);
        }

        void DeleteSelected()
        {
            var appointment = SelectedAppointment();
            if (appointment is null)
            {
                return;
            }

            if (MessageBox.Show(this, $"Delete appointment {appointment.AppointmentId} for {appointment.PatientName}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            context.Data.Appointments.Remove(appointment);
            SaveSelection("Appointment record deleted.");
        }

        searchBox.TextChanged += (_, _) => ApplyFilter();
        newButton.Click += (_, _) => CreateAppointment();
        editButton.Click += (_, _) => EditSelected();
        deleteButton.Click += (_, _) => DeleteSelected();
        if (allowEdit)
        {
            grid.CellDoubleClick += (_, _) => EditSelected();
        }
        grid.SelectionChanged += (_, _) => RefreshAppointmentDetail();
        refreshBindings.Add(ApplyFilter);

        root.Controls.Add(heroCard, 0, 0);
        root.Controls.Add(split, 0, 1);
        page.Controls.Add(root);

        RefreshAppointmentMetrics();
        ApplyFilter();
        return page;
    }

    private TabPage BuildConsultationsTab()
    {
        var allowEdit = HasAnyRole(UserRole.Administrator, UserRole.Doctor);
        var page = new TabPage("Consultations") { BackColor = ClinicTheme.AppBackground };
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12),
            BackColor = ClinicTheme.AppBackground
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 206));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var heroCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(26, 22, 26, 22),
            Margin = new Padding(8, 8, 8, 10)
        };
        ClinicTheme.StyleCard(heroCard, ClinicTheme.Surface, 28);

        var heroLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = ClinicTheme.Surface
        };
        heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));

        var introPanel = new Panel { Dock = DockStyle.Fill, BackColor = ClinicTheme.Surface };
        var introAccent = new Panel
        {
            Width = 52,
            Height = 7,
            BackColor = ClinicTheme.Success,
            Location = new Point(0, 6)
        };
        ClinicTheme.RoundControl(introAccent, 3);
        var heroTitle = new Label
        {
            AutoSize = true,
            Text = "Consultation Records",
            Font = ClinicTheme.DisplayMedium,
            ForeColor = ClinicTheme.TextPrimary,
            Location = new Point(0, 28)
        };
        var heroSubtitle = new Label
        {
            AutoSize = true,
            Text = "Track each clinical encounter from complaint to diagnosis, prescriptions, and billing readiness without losing the patient context behind the record.",
            Font = ClinicTheme.Body,
            ForeColor = ClinicTheme.TextSecondary,
            MaximumSize = new Size(470, 0),
            Location = new Point(0, 72)
        };
        var heroPills = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = ClinicTheme.Surface,
            Location = new Point(0, 142)
        };
        heroPills.Controls.Add(ClinicTheme.CreatePill("Diagnosis-first view", ClinicTheme.SuccessSoft, ClinicTheme.Success));
        heroPills.Controls.Add(ClinicTheme.CreatePill("Prescription-aware", ClinicTheme.SurfaceMuted, ClinicTheme.BrandDark));
        heroPills.Controls.Add(ClinicTheme.CreatePill("Billing-linked encounters", ClinicTheme.AccentSoft, ClinicTheme.BrandDark));
        introPanel.Controls.Add(introAccent);
        introPanel.Controls.Add(heroTitle);
        introPanel.Controls.Add(heroSubtitle);
        introPanel.Controls.Add(heroPills);

        (Panel Panel, Label ValueLabel) CreateMetricCard(string title, string subtitle, Color accent)
        {
            return CreateModuleMetricCard(title, subtitle, accent);
        }

        var metricGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(18, 2, 0, 0),
            BackColor = ClinicTheme.Surface
        };
        metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        metricGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        metricGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var totalCard = CreateMetricCard("Total Records", "All consultation entries", ClinicTheme.Brand);
        var completedCard = CreateMetricCard("Completed", "Clinical notes finalized", ClinicTheme.Success);
        var readyCard = CreateMetricCard("Ready For Billing", "Completed encounters without a bill", ClinicTheme.Accent);
        var prescribedCard = CreateMetricCard("With Prescriptions", "Encounters with medicine items", ClinicTheme.Danger);
        metricGrid.Controls.Add(totalCard.Panel, 0, 0);
        metricGrid.Controls.Add(completedCard.Panel, 1, 0);
        metricGrid.Controls.Add(readyCard.Panel, 0, 1);
        metricGrid.Controls.Add(prescribedCard.Panel, 1, 1);

        heroLayout.Controls.Add(introPanel, 0, 0);
        heroLayout.Controls.Add(metricGrid, 1, 0);
        heroCard.Controls.Add(heroLayout);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel2,
            SplitterDistance = 820,
            SplitterWidth = 10,
            BackColor = ClinicTheme.AppBackground
        };
        split.Panel1.Padding = new Padding(8, 0, 10, 8);
        split.Panel2.Padding = new Padding(10, 0, 8, 8);

        var leftCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 16, 18, 18)
        };
        ClinicTheme.StyleCard(leftCard, ClinicTheme.Surface, 28);

        var leftLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = ClinicTheme.Surface
        };
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var toolbarModel = CreateFilterToolbar("Search by patient, complaint, diagnosis, doctor, medicine, or ID...", includeCrudButtons: true, allowEdit: allowEdit);
        var toolbar = toolbarModel.Host;
        var searchBox = toolbarModel.SearchBox;
        var newButton = toolbarModel.NewButton!;
        var editButton = toolbarModel.EditButton!;
        var deleteButton = toolbarModel.DeleteButton!;

        var bindingSource = new BindingSource();
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            DataSource = bindingSource,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false
        };
        ClinicTheme.StyleGrid(grid);
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Consultation.DateOfVisit),
            HeaderText = "Visit",
            FillWeight = 78,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "d" }
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Consultation.PatientName),
            HeaderText = "Patient",
            FillWeight = 142
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Consultation.Doctor),
            HeaderText = "Doctor",
            FillWeight = 122
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Consultation.ChiefComplaint),
            HeaderText = "Complaint",
            FillWeight = 132
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Consultation.Diagnosis),
            HeaderText = "Diagnosis",
            FillWeight = 138
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Consultation.Status),
            HeaderText = "Status",
            FillWeight = 75
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Consultation.ConsultationId),
            HeaderText = "ID",
            FillWeight = 70
        });

        leftLayout.Controls.Add(toolbar, 0, 0);
        leftLayout.Controls.Add(grid, 0, 1);
        leftCard.Controls.Add(leftLayout);

        var detailCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 18, 18, 18)
        };
        ClinicTheme.StyleCard(detailCard, ClinicTheme.Surface, 28);

        var detailLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = ClinicTheme.Surface
        };
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 196));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 136));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var profilePanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 18, 18, 18),
            Margin = new Padding(0, 0, 0, 12)
        };
        ClinicTheme.StyleCard(profilePanel, ClinicTheme.SurfaceRaised, 24);

        var dateBadge = new Panel
        {
            Size = new Size(92, 92),
            Location = new Point(18, 18),
            BackColor = ClinicTheme.Success
        };
        ClinicTheme.RoundControl(dateBadge, 28);
        var dateBadgeLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "--",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Bahnschrift SemiBold", 15f, FontStyle.Bold),
            ForeColor = Color.White
        };
        dateBadge.Controls.Add(dateBadgeLabel);

        var detailPatientLabel = new Label
        {
            AutoSize = true,
            Text = "Select a consultation",
            Font = ClinicTheme.Heading,
            ForeColor = ClinicTheme.TextPrimary,
            Location = new Point(126, 24)
        };
        var detailIdLabel = new Label
        {
            AutoSize = true,
            Text = "No encounter selected",
            Font = ClinicTheme.Body,
            ForeColor = ClinicTheme.TextSecondary,
            Location = new Point(126, 54)
        };
        var pillRow = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = ClinicTheme.SurfaceRaised,
            Location = new Point(126, 92)
        };
        var statusPill = ClinicTheme.CreatePill("Status: -", ClinicTheme.SurfaceMuted, ClinicTheme.BrandDark);
        var visitPill = ClinicTheme.CreatePill("Visit: -", ClinicTheme.AccentSoft, ClinicTheme.BrandDark);
        var doctorPill = ClinicTheme.CreatePill("Doctor: -", ClinicTheme.SurfaceMuted, ClinicTheme.BrandDark);
        var billingPill = ClinicTheme.CreatePill("Billing: -", ClinicTheme.SuccessSoft, ClinicTheme.Success);
        var prescriptionPill = ClinicTheme.CreatePill("Rx: -", ClinicTheme.DangerSoft, ClinicTheme.Danger);
        pillRow.Controls.Add(statusPill);
        pillRow.Controls.Add(visitPill);
        pillRow.Controls.Add(doctorPill);
        pillRow.Controls.Add(billingPill);
        pillRow.Controls.Add(prescriptionPill);

        var detailHeroText = new Label
        {
            AutoSize = true,
            Text = "Use this panel to review clinical notes, patient context, prescription cost, and billing readiness before changing the encounter.",
            Font = ClinicTheme.Caption,
            ForeColor = ClinicTheme.TextSecondary,
            MaximumSize = new Size(300, 0),
            Location = new Point(18, 126)
        };

        profilePanel.Controls.Add(dateBadge);
        profilePanel.Controls.Add(detailPatientLabel);
        profilePanel.Controls.Add(detailIdLabel);
        profilePanel.Controls.Add(pillRow);
        profilePanel.Controls.Add(detailHeroText);

        Panel CreateDetailSection(string title, Color accent, out Label valueLabel)
        {
            var sectionPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 16, 18, 16),
                Margin = new Padding(0, 0, 0, 12)
            };
            ClinicTheme.StyleCard(sectionPanel, ClinicTheme.SurfaceRaised, 22);

            var accentBar = new Panel
            {
                Width = 34,
                Height = 4,
                BackColor = accent,
                Location = new Point(18, 16)
            };
            ClinicTheme.RoundControl(accentBar, 2);

            var titleLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 30,
                Text = title,
                Font = ClinicTheme.BodyBold,
                ForeColor = ClinicTheme.TextPrimary,
                Padding = new Padding(0, 10, 0, 0)
            };
            valueLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = ClinicTheme.Body,
                ForeColor = ClinicTheme.TextSecondary
            };

            var bodyHost = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 0),
                BackColor = Color.Transparent
            };
            bodyHost.Controls.Add(valueLabel);
            sectionPanel.Controls.Add(accentBar);
            sectionPanel.Controls.Add(bodyHost);
            sectionPanel.Controls.Add(titleLabel);
            return sectionPanel;
        }

        var patientPanel = CreateDetailSection("Patient Snapshot", ClinicTheme.Brand, out var detailPatientSnapshotLabel);
        var clinicalPanel = CreateDetailSection("Clinical Summary", ClinicTheme.Success, out var detailClinicalLabel);

        var lowerSections = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = ClinicTheme.Surface,
            Margin = new Padding(0)
        };
        lowerSections.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        lowerSections.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var prescriptionPanel = CreateDetailSection("Prescription Ledger", ClinicTheme.Danger, out var detailPrescriptionLabel);
        var billingPanel = CreateDetailSection("Billing State", ClinicTheme.Accent, out var detailBillingLabel);
        lowerSections.Controls.Add(prescriptionPanel, 0, 0);
        lowerSections.Controls.Add(billingPanel, 0, 1);

        detailLayout.Controls.Add(profilePanel, 0, 0);
        detailLayout.Controls.Add(patientPanel, 0, 1);
        detailLayout.Controls.Add(clinicalPanel, 0, 2);
        detailLayout.Controls.Add(lowerSections, 0, 3);
        detailCard.Controls.Add(detailLayout);

        split.Panel1.Controls.Add(leftCard);
        split.Panel2.Controls.Add(detailCard);

        Consultation? SelectedConsultation()
        {
            return grid.CurrentRow?.DataBoundItem as Consultation;
        }

        static string ValueOrPlaceholder(string value, string placeholder)
        {
            return string.IsNullOrWhiteSpace(value) ? placeholder : value.Trim();
        }

        void ApplyStatusPill(ConsultationStatus status)
        {
            statusPill.Text = $"Status: {status}";
            if (status == ConsultationStatus.Completed)
            {
                statusPill.BackColor = ClinicTheme.SuccessSoft;
                statusPill.ForeColor = ClinicTheme.Success;
            }
            else
            {
                statusPill.BackColor = ClinicTheme.AccentSoft;
                statusPill.ForeColor = ClinicTheme.BrandDark;
            }
        }

        void ApplyBillingPill(Consultation consultation)
        {
            var linkedBills = context.Data.BillingRecords.Count(record => record.ConsultationId == consultation.ConsultationId);
            if (linkedBills > 0)
            {
                billingPill.Text = $"Billing: Linked {linkedBills}";
                billingPill.BackColor = ClinicTheme.SuccessSoft;
                billingPill.ForeColor = ClinicTheme.Success;
                return;
            }

            if (consultation.Status == ConsultationStatus.Completed)
            {
                billingPill.Text = "Billing: Ready";
                billingPill.BackColor = ClinicTheme.AccentSoft;
                billingPill.ForeColor = ClinicTheme.BrandDark;
                return;
            }

            billingPill.Text = "Billing: Blocked";
            billingPill.BackColor = ClinicTheme.SurfaceMuted;
            billingPill.ForeColor = ClinicTheme.BrandDark;
        }

        string BuildPatientSnapshot(Consultation consultation)
        {
            var patient = lookupService.FindPatient(consultation.PatientId);
            var appointmentCount = context.Data.Appointments.Count(appointment => appointment.PatientId == consultation.PatientId);
            var consultationCount = context.Data.Consultations.Count(entry => entry.PatientId == consultation.PatientId);
            var openBills = context.Data.BillingRecords.Count(record => record.PatientId == consultation.PatientId && record.PaymentStatus != PaymentStatus.Paid);
            var latestAppointment = context.Data.Appointments
                .Where(appointment => appointment.PatientId == consultation.PatientId)
                .OrderByDescending(appointment => appointment.AppointmentDate)
                .ThenByDescending(appointment => appointment.AppointmentId)
                .FirstOrDefault();

            if (patient is null)
            {
                return string.Join(
                    Environment.NewLine,
                    new[]
                    {
                        $"Patient ID: {consultation.PatientId}",
                        $"Appointments on file: {appointmentCount}",
                        $"Consultations on file: {consultationCount}",
                        $"Open bills: {openBills}"
                    });
            }

            return string.Join(
                Environment.NewLine,
                new[]
                {
                    $"Contact: {ValueOrPlaceholder(patient.ContactNumber, "No contact number")}",
                    $"Age / Sex: {patient.Age} / {patient.Sex}",
                    $"Address: {ValueOrPlaceholder(patient.Address, "No address recorded")}",
                    $"Visits on file: {consultationCount} consultation(s), {appointmentCount} appointment(s)",
                    latestAppointment is null
                        ? $"Open bills: {openBills}"
                        : $"Latest appointment: {latestAppointment.AppointmentDate:d} at {latestAppointment.AppointmentTime} | Open bills: {openBills}"
                });
        }

        string BuildClinicalSummary(Consultation consultation)
        {
            return string.Join(
                Environment.NewLine,
                new[]
                {
                    $"Chief complaint: {ValueOrPlaceholder(consultation.ChiefComplaint, "Not recorded")}",
                    $"Diagnosis: {ValueOrPlaceholder(consultation.Diagnosis, "Not recorded")}",
                    $"Treatment notes: {ValueOrPlaceholder(consultation.TreatmentNotes, "Not recorded")}"
                });
        }

        string BuildPrescriptionSummary(Consultation consultation)
        {
            var items = prescriptionService.GetByConsultation(consultation.ConsultationId);
            if (items.Count == 0)
            {
                return "No structured prescription items are linked to this consultation.";
            }

            var totalUnits = items.Sum(item => item.Quantity);
            var lines = new List<string>
            {
                $"Items: {items.Count}",
                $"Units dispensed: {totalUnits}",
                $"Total value: {items.Sum(item => item.TotalCost):C}"
            };
            lines.AddRange(items.Take(4).Select(item => $"{item.MedicineName} | {item.Dosage} | Qty {item.Quantity}"));
            if (items.Count > 4)
            {
                lines.Add($"+ {items.Count - 4} more item(s)");
            }

            return string.Join(Environment.NewLine, lines);
        }

        string BuildBillingSummary(Consultation consultation)
        {
            var linkedBills = context.Data.BillingRecords
                .Where(record => record.ConsultationId == consultation.ConsultationId)
                .OrderBy(record => record.BillingId)
                .ToList();
            if (linkedBills.Count == 0)
            {
                return consultation.Status == ConsultationStatus.Completed
                    ? string.Join(
                        Environment.NewLine,
                        new[]
                        {
                            "No billing record yet.",
                            $"Suggested medicine charge: {prescriptionService.GetTotalCostForConsultation(consultation.ConsultationId):C}",
                            "This completed consultation is ready for billing."
                        })
                    : "No billing record yet. Billing will remain blocked until the consultation is completed.";
            }

            var totalBilled = linkedBills.Sum(record => record.TotalAmount);
            var openBills = linkedBills.Count(record => record.PaymentStatus != PaymentStatus.Paid);
            var lines = new List<string>
            {
                $"Linked bills: {linkedBills.Count}",
                $"Open bills: {openBills}",
                $"Total billed: {totalBilled:C}"
            };
            lines.AddRange(linkedBills.Select(record =>
                $"{record.BillingId} | {record.PaymentStatus} | Total {record.TotalAmount:C}"));
            return string.Join(Environment.NewLine, lines);
        }

        void RefreshConsultationMetrics()
        {
            totalCard.ValueLabel.Text = context.Data.Consultations.Count.ToString();
            completedCard.ValueLabel.Text = context.Data.Consultations.Count(consultation => consultation.Status == ConsultationStatus.Completed).ToString();
            readyCard.ValueLabel.Text = context.Data.Consultations.Count(consultation =>
                consultation.Status == ConsultationStatus.Completed
                && context.Data.BillingRecords.All(record => record.ConsultationId != consultation.ConsultationId)).ToString();
            prescribedCard.ValueLabel.Text = context.Data.Consultations.Count(consultation =>
                context.Data.PrescriptionItems.Any(item => item.ConsultationId == consultation.ConsultationId)).ToString();
        }

        void RefreshConsultationDetail()
        {
            var consultation = SelectedConsultation();
            if (consultation is null)
            {
                dateBadgeLabel.Text = "--";
                detailPatientLabel.Text = "Select a consultation";
                detailIdLabel.Text = "No encounter selected";
                statusPill.Text = "Status: -";
                statusPill.BackColor = ClinicTheme.SurfaceMuted;
                statusPill.ForeColor = ClinicTheme.BrandDark;
                visitPill.Text = "Visit: -";
                doctorPill.Text = "Doctor: -";
                billingPill.Text = "Billing: -";
                billingPill.BackColor = ClinicTheme.SuccessSoft;
                billingPill.ForeColor = ClinicTheme.Success;
                prescriptionPill.Text = "Rx: -";
                prescriptionPill.BackColor = ClinicTheme.DangerSoft;
                prescriptionPill.ForeColor = ClinicTheme.Danger;
                detailPatientSnapshotLabel.Text = "Choose a consultation to view the linked patient profile and operational context.";
                detailClinicalLabel.Text = "Choose a consultation from the table to inspect the complaint, diagnosis, and treatment notes.";
                detailPrescriptionLabel.Text = "Prescription items and totals will appear here.";
                detailBillingLabel.Text = "Billing linkage and readiness will appear here.";
                return;
            }

            var prescriptionItems = prescriptionService.GetByConsultation(consultation.ConsultationId);
            dateBadgeLabel.Text = consultation.DateOfVisit.ToString("MMM\nd");
            detailPatientLabel.Text = consultation.PatientName;
            detailIdLabel.Text = $"{consultation.ConsultationId}  |  {consultation.PatientId}";
            ApplyStatusPill(consultation.Status);
            visitPill.Text = $"Visit: {consultation.DateOfVisit:MMM d}";
            doctorPill.Text = $"Doctor: {consultation.Doctor}";
            ApplyBillingPill(consultation);
            prescriptionPill.Text = $"Rx: {prescriptionItems.Count} item(s)";
            detailPatientSnapshotLabel.Text = BuildPatientSnapshot(consultation);
            detailClinicalLabel.Text = BuildClinicalSummary(consultation);
            detailPrescriptionLabel.Text = BuildPrescriptionSummary(consultation);
            detailBillingLabel.Text = BuildBillingSummary(consultation);
        }

        void ApplyFilter()
        {
            var term = searchBox.Text.Trim();
            var filtered = context.Data.Consultations
                .Where(consultation => string.IsNullOrWhiteSpace(term)
                    || $"{consultation.ConsultationId} {consultation.PatientId} {consultation.PatientName} {consultation.Doctor} {consultation.Status} {consultation.ChiefComplaint} {consultation.Diagnosis} {consultation.PrescribedMedicines}"
                        .Contains(term, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(consultation => consultation.DateOfVisit)
                .ToList();
            bindingSource.DataSource = filtered;

            if (grid.Rows.Count > 0)
            {
                grid.ClearSelection();
                grid.Rows[0].Selected = true;
                grid.CurrentCell = grid.Rows[0].Cells[0];
            }

            RefreshConsultationMetrics();
            RefreshConsultationDetail();
        }

        void SaveSelection(string message, Consultation? selected = null)
        {
            PersistAndRefresh(message);
            if (selected is not null)
            {
                SelectItem(bindingSource, grid, searchBox, selected, ApplyFilter);
            }

            RefreshConsultationMetrics();
            RefreshConsultationDetail();
        }

        bool TryAutoCompleteLinkedAppointment(Consultation consultation)
        {
            if (consultation.Status != ConsultationStatus.Completed)
            {
                return false;
            }

            var matchingAppointment = context.Data.Appointments
                .Where(appointment =>
                    appointment.PatientId == consultation.PatientId
                    && appointment.Status == AppointmentStatus.Pending
                    && appointment.AppointmentDate.Date <= consultation.DateOfVisit.Date
                    && (string.IsNullOrWhiteSpace(consultation.Doctor)
                        || string.IsNullOrWhiteSpace(appointment.DoctorAssigned)
                        || string.Equals(appointment.DoctorAssigned, consultation.Doctor, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(appointment => appointment.AppointmentDate)
                .ThenByDescending(appointment => appointment.AppointmentId)
                .FirstOrDefault();

            if (matchingAppointment is null)
            {
                return false;
            }

            matchingAppointment.Status = AppointmentStatus.Completed;
            return true;
        }

        void CreateConsultation()
        {
            var defaultDoctor = context.CurrentUser?.Role == UserRole.Doctor
                ? context.CurrentUser.FullName
                : lookupService.GetDoctors().FirstOrDefault()?.FullName ?? string.Empty;
            var consultation = new Consultation
            {
                DateOfVisit = DateTime.Today,
                Doctor = defaultDoctor,
                Status = ConsultationStatus.Pending
            };

            if (!EditorForms.EditConsultation(this, consultation, lookupService, Array.Empty<PrescriptionItem>(), out var prescriptionItems))
            {
                return;
            }

            consultation.ConsultationId = IdGenerator.Next("CON", context.Data.Consultations.Select(entry => entry.ConsultationId));
            context.Data.Consultations.Add(consultation);

            if (!prescriptionService.TryApplyToConsultation(consultation, prescriptionItems, out var error))
            {
                context.Data.Consultations.Remove(consultation);
                MessageBox.Show(this, error, "Prescription Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lookupService.SyncConsultationReferences(consultation);
            var appointmentAutoCompleted = TryAutoCompleteLinkedAppointment(consultation);
            SaveSelection(
                appointmentAutoCompleted
                    ? "Consultation record saved. Matching appointment marked as completed."
                    : "Consultation record saved.",
                consultation);
        }

        void EditSelected()
        {
            if (!allowEdit)
            {
                return;
            }

            var consultation = SelectedConsultation();
            if (consultation is null)
            {
                return;
            }

            var original = CloneConsultation(consultation);
            var existingPrescriptionItems = prescriptionService.GetByConsultation(consultation.ConsultationId);
            if (!EditorForms.EditConsultation(this, consultation, lookupService, existingPrescriptionItems, out var updatedPrescriptionItems))
            {
                return;
            }

            if (consultation.Status != ConsultationStatus.Completed
                && context.Data.BillingRecords.Any(billing => billing.ConsultationId == consultation.ConsultationId))
            {
                RestoreConsultation(consultation, original);
                MessageBox.Show(this, "A consultation linked to billing must remain completed.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!prescriptionService.TryApplyToConsultation(consultation, updatedPrescriptionItems, out var error))
            {
                RestoreConsultation(consultation, original);
                MessageBox.Show(this, error, "Prescription Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lookupService.SyncConsultationReferences(consultation);
            var appointmentAutoCompleted =
                original.Status != ConsultationStatus.Completed
                && consultation.Status == ConsultationStatus.Completed
                && TryAutoCompleteLinkedAppointment(consultation);

            SaveSelection(
                appointmentAutoCompleted
                    ? "Consultation record saved. Matching appointment marked as completed."
                    : "Consultation record saved.",
                consultation);
        }

        void DeleteSelected()
        {
            var consultation = SelectedConsultation();
            if (consultation is null)
            {
                return;
            }

            if (context.Data.BillingRecords.Any(billing => billing.ConsultationId == consultation.ConsultationId))
            {
                MessageBox.Show(this, $"Cannot delete consultation {consultation.ConsultationId} because it is linked to billing.", "Delete Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show(this, $"Delete consultation {consultation.ConsultationId} for {consultation.PatientName}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            prescriptionService.RemoveForConsultation(consultation.ConsultationId);
            context.Data.Consultations.Remove(consultation);
            SaveSelection("Consultation record deleted.");
        }

        newButton.Click += (_, _) => CreateConsultation();
        editButton.Click += (_, _) => EditSelected();
        deleteButton.Click += (_, _) => DeleteSelected();
        searchBox.TextChanged += (_, _) => ApplyFilter();
        if (allowEdit)
        {
            grid.CellDoubleClick += (_, _) => EditSelected();
        }
        grid.SelectionChanged += (_, _) => RefreshConsultationDetail();
        refreshBindings.Add(ApplyFilter);

        root.Controls.Add(heroCard, 0, 0);
        root.Controls.Add(split, 0, 1);
        page.Controls.Add(root);

        RefreshConsultationMetrics();
        ApplyFilter();
        return page;
    }

    private TabPage BuildBillingTab()
    {
        var allowEdit = HasAnyRole(UserRole.Administrator, UserRole.Receptionist);
        var page = new TabPage("Billing") { BackColor = ClinicTheme.AppBackground };
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12),
            BackColor = ClinicTheme.AppBackground
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 206));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var heroCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(26, 22, 26, 22),
            Margin = new Padding(8, 8, 8, 10)
        };
        ClinicTheme.StyleCard(heroCard, ClinicTheme.Surface, 28);

        var heroLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = ClinicTheme.Surface
        };
        heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));

        var introPanel = new Panel { Dock = DockStyle.Fill, BackColor = ClinicTheme.Surface };
        var introAccent = new Panel
        {
            Width = 52,
            Height = 7,
            BackColor = ClinicTheme.Danger,
            Location = new Point(0, 6)
        };
        ClinicTheme.RoundControl(introAccent, 3);
        var heroTitle = new Label
        {
            AutoSize = true,
            Text = "Billing Ledger",
            Font = ClinicTheme.DisplayMedium,
            ForeColor = ClinicTheme.TextPrimary,
            Location = new Point(0, 28)
        };
        var heroSubtitle = new Label
        {
            AutoSize = true,
            Text = "Review charge totals, watch what is still awaiting collection, and keep each billing record anchored to the completed consultation behind it.",
            Font = ClinicTheme.Body,
            ForeColor = ClinicTheme.TextSecondary,
            MaximumSize = new Size(470, 0),
            Location = new Point(0, 72)
        };
        var heroPills = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = ClinicTheme.Surface,
            Location = new Point(0, 142)
        };
        heroPills.Controls.Add(ClinicTheme.CreatePill("Consultation-linked charges", ClinicTheme.DangerSoft, ClinicTheme.Danger));
        heroPills.Controls.Add(ClinicTheme.CreatePill("Payment status visible", ClinicTheme.SurfaceMuted, ClinicTheme.BrandDark));
        heroPills.Controls.Add(ClinicTheme.CreatePill("Prescription-aware totals", ClinicTheme.AccentSoft, ClinicTheme.BrandDark));
        introPanel.Controls.Add(introAccent);
        introPanel.Controls.Add(heroTitle);
        introPanel.Controls.Add(heroSubtitle);
        introPanel.Controls.Add(heroPills);

        (Panel Panel, Label ValueLabel) CreateMetricCard(string title, string subtitle, Color accent)
        {
            return CreateModuleMetricCard(title, subtitle, accent);
        }

        var metricGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(18, 2, 0, 0),
            BackColor = ClinicTheme.Surface
        };
        metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        metricGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        metricGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var totalCard = CreateMetricCard("Total Bills", "Billing records on file", ClinicTheme.Brand);
        var awaitingCard = CreateMetricCard("Awaiting Settlement", "Unpaid or partially paid", ClinicTheme.Danger);
        var readyCard = CreateMetricCard("Ready To Bill", "Completed consultations without bills", ClinicTheme.Accent);
        var revenueCard = CreateMetricCard("Paid Revenue", "Settled billing value", ClinicTheme.Success);
        metricGrid.Controls.Add(totalCard.Panel, 0, 0);
        metricGrid.Controls.Add(awaitingCard.Panel, 1, 0);
        metricGrid.Controls.Add(readyCard.Panel, 0, 1);
        metricGrid.Controls.Add(revenueCard.Panel, 1, 1);

        heroLayout.Controls.Add(introPanel, 0, 0);
        heroLayout.Controls.Add(metricGrid, 1, 0);
        heroCard.Controls.Add(heroLayout);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel2,
            SplitterDistance = 820,
            SplitterWidth = 10,
            BackColor = ClinicTheme.AppBackground
        };
        split.Panel1.Padding = new Padding(8, 0, 10, 8);
        split.Panel2.Padding = new Padding(10, 0, 8, 8);

        var leftCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 16, 18, 18)
        };
        ClinicTheme.StyleCard(leftCard, ClinicTheme.Surface, 28);

        var leftLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = ClinicTheme.Surface
        };
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var toolbarModel = CreateFilterToolbar("Search by bill, patient, consultation, or payment status...", includeCrudButtons: true, allowEdit: allowEdit);
        var toolbar = toolbarModel.Host;
        var searchBox = toolbarModel.SearchBox;
        var newButton = toolbarModel.NewButton!;
        var editButton = toolbarModel.EditButton!;
        var deleteButton = toolbarModel.DeleteButton!;

        var bindingSource = new BindingSource();
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            DataSource = bindingSource,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false
        };
        ClinicTheme.StyleGrid(grid);
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(BillingRecord.BillingId),
            HeaderText = "Bill ID",
            FillWeight = 74
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(BillingRecord.PatientName),
            HeaderText = "Patient",
            FillWeight = 146
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(BillingRecord.ConsultationId),
            HeaderText = "Consultation",
            FillWeight = 96
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(BillingRecord.ServiceCharges),
            HeaderText = "Service",
            FillWeight = 74,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(BillingRecord.MedicineCharges),
            HeaderText = "Medicine",
            FillWeight = 74,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(BillingRecord.TotalAmount),
            HeaderText = "Total",
            FillWeight = 78,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(BillingRecord.PaymentStatus),
            HeaderText = "Status",
            FillWeight = 86
        });

        leftLayout.Controls.Add(toolbar, 0, 0);
        leftLayout.Controls.Add(grid, 0, 1);
        leftCard.Controls.Add(leftLayout);

        var detailCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 18, 18, 18)
        };
        ClinicTheme.StyleCard(detailCard, ClinicTheme.Surface, 28);

        var detailLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = ClinicTheme.Surface
        };
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 202));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 126));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var profilePanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 18, 18, 18),
            Margin = new Padding(0, 0, 0, 12)
        };
        ClinicTheme.StyleCard(profilePanel, ClinicTheme.SurfaceRaised, 24);

        var statusBadge = new Panel
        {
            Size = new Size(92, 92),
            Location = new Point(18, 18),
            BackColor = ClinicTheme.Danger
        };
        ClinicTheme.RoundControl(statusBadge, 28);
        var statusBadgeLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "--",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Bahnschrift SemiBold", 15f, FontStyle.Bold),
            ForeColor = Color.White
        };
        statusBadge.Controls.Add(statusBadgeLabel);

        var detailPatientLabel = new Label
        {
            AutoSize = true,
            Text = "Select a billing record",
            Font = ClinicTheme.Heading,
            ForeColor = ClinicTheme.TextPrimary,
            Location = new Point(126, 24)
        };
        var detailIdLabel = new Label
        {
            AutoSize = true,
            Text = "No billing entry selected",
            Font = ClinicTheme.Body,
            ForeColor = ClinicTheme.TextSecondary,
            Location = new Point(126, 54)
        };
        var amountCaptionLabel = new Label
        {
            AutoSize = true,
            Text = "Total Charges",
            Font = ClinicTheme.Caption,
            ForeColor = ClinicTheme.TextSecondary,
            Location = new Point(126, 82)
        };
        var amountValueLabel = new Label
        {
            AutoSize = true,
            Text = "--",
            Font = new Font("Bahnschrift SemiBold", 24f, FontStyle.Bold),
            ForeColor = ClinicTheme.TextPrimary,
            Location = new Point(124, 98)
        };
        var pillRow = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = ClinicTheme.SurfaceRaised,
            Location = new Point(18, 144)
        };
        var paymentPill = ClinicTheme.CreatePill("Status: -", ClinicTheme.SurfaceMuted, ClinicTheme.BrandDark);
        var consultationPill = ClinicTheme.CreatePill("Consultation: -", ClinicTheme.AccentSoft, ClinicTheme.BrandDark);
        var patientPill = ClinicTheme.CreatePill("Patient: -", ClinicTheme.SurfaceMuted, ClinicTheme.BrandDark);
        var rxPill = ClinicTheme.CreatePill("Rx Suggestion: -", ClinicTheme.SuccessSoft, ClinicTheme.Success);
        pillRow.Controls.Add(paymentPill);
        pillRow.Controls.Add(consultationPill);
        pillRow.Controls.Add(patientPill);
        pillRow.Controls.Add(rxPill);

        var detailHeroText = new Label
        {
            AutoSize = true,
            Text = "Review the linked consultation and current payment state before collecting or adjusting charges.",
            Font = ClinicTheme.Caption,
            ForeColor = ClinicTheme.TextSecondary,
            MaximumSize = new Size(330, 0),
            Location = new Point(18, 170)
        };

        profilePanel.Controls.Add(statusBadge);
        profilePanel.Controls.Add(detailPatientLabel);
        profilePanel.Controls.Add(detailIdLabel);
        profilePanel.Controls.Add(amountCaptionLabel);
        profilePanel.Controls.Add(amountValueLabel);
        profilePanel.Controls.Add(pillRow);
        profilePanel.Controls.Add(detailHeroText);

        Panel CreateDetailSection(string title, Color accent, out Label valueLabel)
        {
            var sectionPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 16, 18, 16),
                Margin = new Padding(0, 0, 0, 12)
            };
            ClinicTheme.StyleCard(sectionPanel, ClinicTheme.SurfaceRaised, 22);

            var accentBar = new Panel
            {
                Width = 34,
                Height = 4,
                BackColor = accent,
                Location = new Point(18, 16)
            };
            ClinicTheme.RoundControl(accentBar, 2);

            var titleLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 30,
                Text = title,
                Font = ClinicTheme.BodyBold,
                ForeColor = ClinicTheme.TextPrimary,
                Padding = new Padding(0, 10, 0, 0)
            };
            valueLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = ClinicTheme.Body,
                ForeColor = ClinicTheme.TextSecondary
            };

            var bodyHost = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 0),
                BackColor = Color.Transparent
            };
            bodyHost.Controls.Add(valueLabel);
            sectionPanel.Controls.Add(accentBar);
            sectionPanel.Controls.Add(bodyHost);
            sectionPanel.Controls.Add(titleLabel);
            return sectionPanel;
        }

        var consultationPanel = CreateDetailSection("Linked Consultation", ClinicTheme.Accent, out var detailConsultationLabel);
        var chargePanel = CreateDetailSection("Charge Breakdown", ClinicTheme.Danger, out var detailChargeLabel);
        var accountPanel = CreateDetailSection("Account Context", ClinicTheme.Success, out var detailAccountLabel);

        detailLayout.Controls.Add(profilePanel, 0, 0);
        detailLayout.Controls.Add(consultationPanel, 0, 1);
        detailLayout.Controls.Add(chargePanel, 0, 2);
        detailLayout.Controls.Add(accountPanel, 0, 3);
        detailCard.Controls.Add(detailLayout);

        split.Panel1.Controls.Add(leftCard);
        split.Panel2.Controls.Add(detailCard);

        BillingRecord? SelectedBilling()
        {
            return grid.CurrentRow?.DataBoundItem as BillingRecord;
        }

        static string ValueOrPlaceholder(string value, string placeholder)
        {
            return string.IsNullOrWhiteSpace(value) ? placeholder : value.Trim();
        }

        DateTime BillingSortDate(BillingRecord billing)
        {
            return lookupService.FindConsultation(billing.ConsultationId)?.DateOfVisit ?? DateTime.MinValue;
        }

        void ApplyPaymentStyle(PaymentStatus status)
        {
            paymentPill.Text = $"Status: {status}";

            switch (status)
            {
                case PaymentStatus.Paid:
                    statusBadge.BackColor = ClinicTheme.Success;
                    statusBadgeLabel.Text = "PAID";
                    paymentPill.BackColor = ClinicTheme.SuccessSoft;
                    paymentPill.ForeColor = ClinicTheme.Success;
                    break;
                case PaymentStatus.PartiallyPaid:
                    statusBadge.BackColor = ClinicTheme.Accent;
                    statusBadgeLabel.Text = "PART";
                    paymentPill.BackColor = ClinicTheme.AccentSoft;
                    paymentPill.ForeColor = ClinicTheme.BrandDark;
                    break;
                default:
                    statusBadge.BackColor = ClinicTheme.Danger;
                    statusBadgeLabel.Text = "OPEN";
                    paymentPill.BackColor = ClinicTheme.DangerSoft;
                    paymentPill.ForeColor = ClinicTheme.Danger;
                    break;
            }
        }

        string BuildConsultationSummary(BillingRecord billing)
        {
            var consultation = lookupService.FindConsultation(billing.ConsultationId);
            if (consultation is null)
            {
                return string.Join(
                    Environment.NewLine,
                    new[]
                    {
                        "The linked consultation could not be found.",
                        $"Consultation ID: {billing.ConsultationId}",
                        $"Suggested medicine total: {prescriptionService.GetTotalCostForConsultation(billing.ConsultationId):C}"
                    });
            }

            var prescriptionItems = prescriptionService.GetByConsultation(consultation.ConsultationId);
            return string.Join(
                Environment.NewLine,
                new[]
                {
                    $"Visit date: {consultation.DateOfVisit:d}",
                    $"Doctor: {ValueOrPlaceholder(consultation.Doctor, "No doctor recorded")}",
                    $"Status: {consultation.Status}",
                    $"Complaint: {ValueOrPlaceholder(consultation.ChiefComplaint, "Not recorded")}",
                    $"Diagnosis: {ValueOrPlaceholder(consultation.Diagnosis, "Not recorded")}",
                    $"Prescription items: {prescriptionItems.Count} | Suggested medicine total: {prescriptionItems.Sum(item => item.TotalCost):C}"
                });
        }

        string BuildChargeBreakdown(BillingRecord billing)
        {
            var suggestedMedicineCharge = prescriptionService.GetTotalCostForConsultation(billing.ConsultationId);
            var variance = billing.MedicineCharges - suggestedMedicineCharge;

            return string.Join(
                Environment.NewLine,
                new[]
                {
                    $"Service charges: {billing.ServiceCharges:C}",
                    $"Medicine charges: {billing.MedicineCharges:C}",
                    $"Total amount: {billing.TotalAmount:C}",
                    $"Prescription suggestion: {suggestedMedicineCharge:C}",
                    variance == 0
                        ? "Medicine charge matches the current prescription total."
                        : $"Medicine variance: {variance:C}"
                });
        }

        string BuildAccountContext(BillingRecord billing)
        {
            var patientBills = context.Data.BillingRecords
                .Where(record => record.PatientId == billing.PatientId)
                .OrderByDescending(BillingSortDate)
                .ThenByDescending(record => record.BillingId)
                .ToList();
            var unpaidCount = patientBills.Count(record => record.PaymentStatus != PaymentStatus.Paid);
            var paidRevenue = patientBills.Where(record => record.PaymentStatus == PaymentStatus.Paid).Sum(record => record.TotalAmount);
            var billableConsultations = context.Data.Consultations.Count(consultation =>
                consultation.PatientId == billing.PatientId
                && consultation.Status == ConsultationStatus.Completed
                && context.Data.BillingRecords.All(record => record.ConsultationId != consultation.ConsultationId));

            var lines = new List<string>
            {
                $"Patient bills on file: {patientBills.Count}",
                $"Open bills for this patient: {unpaidCount}",
                $"Paid revenue from this patient: {paidRevenue:C}",
                $"Other consultations ready to bill: {billableConsultations}"
            };

            var recentItems = patientBills
                .Where(record => record.BillingId != billing.BillingId)
                .Take(3)
                .Select(record => $"{record.BillingId} | {record.PaymentStatus} | {record.TotalAmount:C}")
                .ToList();
            lines.Add(recentItems.Count == 0 ? "No other billing records for this patient." : string.Join(Environment.NewLine, recentItems));
            return string.Join(Environment.NewLine, lines);
        }

        string BuildCollectionNote(BillingRecord billing)
        {
            return billing.PaymentStatus switch
            {
                PaymentStatus.Paid => "This billing record is already settled.",
                PaymentStatus.PartiallyPaid => "Partial settlement recorded. Review the account context before closing collection.",
                _ => "This charge is still open. Confirm the linked consultation and collect the outstanding amount."
            };
        }

        void RefreshBillingMetrics()
        {
            totalCard.ValueLabel.Text = context.Data.BillingRecords.Count.ToString();
            awaitingCard.ValueLabel.Text = context.Data.BillingRecords.Count(record => record.PaymentStatus != PaymentStatus.Paid).ToString();
            readyCard.ValueLabel.Text = context.Data.Consultations.Count(consultation =>
                consultation.Status == ConsultationStatus.Completed
                && context.Data.BillingRecords.All(record => record.ConsultationId != consultation.ConsultationId)).ToString();
            revenueCard.ValueLabel.Text = context.Data.BillingRecords
                .Where(record => record.PaymentStatus == PaymentStatus.Paid)
                .Sum(record => record.TotalAmount)
                .ToString("C0");
        }

        void RefreshBillingDetail()
        {
            var billing = SelectedBilling();
            if (billing is null)
            {
                statusBadge.BackColor = ClinicTheme.SurfaceMuted;
                statusBadgeLabel.Text = "--";
                detailPatientLabel.Text = "Select a billing record";
                detailIdLabel.Text = "No billing entry selected";
                amountValueLabel.Text = "--";
                paymentPill.Text = "Status: -";
                paymentPill.BackColor = ClinicTheme.SurfaceMuted;
                paymentPill.ForeColor = ClinicTheme.BrandDark;
                consultationPill.Text = "Consultation: -";
                patientPill.Text = "Patient: -";
                rxPill.Text = "Rx Suggestion: -";
                detailHeroText.Text = "Review the linked consultation and current payment state before collecting or adjusting charges.";
                detailConsultationLabel.Text = "Select a billing record to inspect the linked consultation and diagnosis context.";
                detailChargeLabel.Text = "Charge totals and prescription comparisons will appear here.";
                detailAccountLabel.Text = "Patient billing context and other outstanding items will appear here.";
                return;
            }

            ApplyPaymentStyle(billing.PaymentStatus);
            detailPatientLabel.Text = billing.PatientName;
            detailIdLabel.Text = $"{billing.BillingId}  |  {billing.ConsultationId}";
            amountValueLabel.Text = billing.TotalAmount.ToString("C2");
            consultationPill.Text = $"Consultation: {billing.ConsultationId}";
            patientPill.Text = $"Patient: {billing.PatientId}";
            rxPill.Text = $"Rx Suggestion: {prescriptionService.GetTotalCostForConsultation(billing.ConsultationId):C0}";
            detailHeroText.Text = BuildCollectionNote(billing);
            detailConsultationLabel.Text = BuildConsultationSummary(billing);
            detailChargeLabel.Text = BuildChargeBreakdown(billing);
            detailAccountLabel.Text = BuildAccountContext(billing);
        }

        void ApplyFilter()
        {
            var term = searchBox.Text.Trim();
            var filtered = context.Data.BillingRecords
                .Where(billing => string.IsNullOrWhiteSpace(term)
                    || $"{billing.BillingId} {billing.PatientId} {billing.PatientName} {billing.ConsultationId} {billing.PaymentStatus}"
                        .Contains(term, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(BillingSortDate)
                .ThenByDescending(billing => billing.BillingId)
                .ToList();
            bindingSource.DataSource = filtered;

            if (grid.Rows.Count > 0)
            {
                grid.ClearSelection();
                grid.Rows[0].Selected = true;
                grid.CurrentCell = grid.Rows[0].Cells[0];
            }

            RefreshBillingMetrics();
            RefreshBillingDetail();
        }

        void SaveSelection(string message, BillingRecord? selected = null)
        {
            PersistAndRefresh(message);
            if (selected is not null)
            {
                SelectItem(bindingSource, grid, searchBox, selected, ApplyFilter);
            }

            RefreshBillingMetrics();
            RefreshBillingDetail();
        }

        void CreateBilling()
        {
            var billing = new BillingRecord();
            if (!EditorForms.EditBilling(this, billing, lookupService, prescriptionService))
            {
                return;
            }

            billing.BillingId = IdGenerator.Next("BIL", context.Data.BillingRecords.Select(entry => entry.BillingId));
            context.Data.BillingRecords.Add(billing);
            SaveSelection("Billing record saved.", billing);
        }

        void EditSelected()
        {
            if (!allowEdit)
            {
                return;
            }

            var billing = SelectedBilling();
            if (billing is null)
            {
                return;
            }

            if (!EditorForms.EditBilling(this, billing, lookupService, prescriptionService))
            {
                return;
            }

            SaveSelection("Billing record saved.", billing);
        }

        void DeleteSelected()
        {
            var billing = SelectedBilling();
            if (billing is null)
            {
                return;
            }

            if (MessageBox.Show(this, $"Delete billing record {billing.BillingId} for {billing.PatientName}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            context.Data.BillingRecords.Remove(billing);
            SaveSelection("Billing record deleted.");
        }

        searchBox.TextChanged += (_, _) => ApplyFilter();
        newButton.Click += (_, _) => CreateBilling();
        editButton.Click += (_, _) => EditSelected();
        deleteButton.Click += (_, _) => DeleteSelected();
        if (allowEdit)
        {
            grid.CellDoubleClick += (_, _) => EditSelected();
        }
        grid.SelectionChanged += (_, _) => RefreshBillingDetail();
        refreshBindings.Add(ApplyFilter);

        root.Controls.Add(heroCard, 0, 0);
        root.Controls.Add(split, 0, 1);
        page.Controls.Add(root);

        RefreshBillingMetrics();
        ApplyFilter();
        return page;
    }

    private TabPage BuildMedicinesTab()
    {
        var allowEdit = HasAnyRole(UserRole.Administrator);
        var page = new TabPage("Medicines") { BackColor = ClinicTheme.AppBackground };
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12),
            BackColor = ClinicTheme.AppBackground
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 206));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var heroCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(26, 22, 26, 22),
            Margin = new Padding(8, 8, 8, 10)
        };
        ClinicTheme.StyleCard(heroCard, ClinicTheme.Surface, 28);

        var heroLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = ClinicTheme.Surface
        };
        heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));

        var introPanel = new Panel { Dock = DockStyle.Fill, BackColor = ClinicTheme.Surface };
        var introAccent = new Panel
        {
            Width = 52,
            Height = 7,
            BackColor = ClinicTheme.BrandDark,
            Location = new Point(0, 6)
        };
        ClinicTheme.RoundControl(introAccent, 3);
        var heroTitle = new Label
        {
            AutoSize = true,
            Text = "Medicine Inventory",
            Font = ClinicTheme.DisplayMedium,
            ForeColor = ClinicTheme.TextPrimary,
            Location = new Point(0, 28)
        };
        var heroSubtitle = new Label
        {
            AutoSize = true,
            Text = "Track stock levels, identify items nearing expiry, and keep prescription-linked medicines accurate before inventory gaps affect clinic operations.",
            Font = ClinicTheme.Body,
            ForeColor = ClinicTheme.TextSecondary,
            MaximumSize = new Size(470, 0),
            Location = new Point(0, 72)
        };
        var heroPills = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = ClinicTheme.Surface,
            Location = new Point(0, 142)
        };
        heroPills.Controls.Add(ClinicTheme.CreatePill("Stock-aware board", ClinicTheme.SurfaceMuted, ClinicTheme.BrandDark));
        heroPills.Controls.Add(ClinicTheme.CreatePill("Expiry visibility", ClinicTheme.AccentSoft, ClinicTheme.BrandDark));
        heroPills.Controls.Add(ClinicTheme.CreatePill("Prescription-linked items", ClinicTheme.SuccessSoft, ClinicTheme.Success));
        introPanel.Controls.Add(introAccent);
        introPanel.Controls.Add(heroTitle);
        introPanel.Controls.Add(heroSubtitle);
        introPanel.Controls.Add(heroPills);

        (Panel Panel, Label ValueLabel) CreateMetricCard(string title, string subtitle, Color accent)
        {
            return CreateModuleMetricCard(title, subtitle, accent);
        }

        var metricGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(18, 2, 0, 0),
            BackColor = ClinicTheme.Surface
        };
        metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        metricGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        metricGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var totalCard = CreateMetricCard("Tracked Items", "Medicines currently in inventory", ClinicTheme.Brand);
        var lowStockCard = CreateMetricCard("Low Stock", "Items at or below 10 units", ClinicTheme.Danger);
        var expiringCard = CreateMetricCard("Expiring Soon", "Items expiring within 30 days", ClinicTheme.Accent);
        var inventoryValueCard = CreateMetricCard("Inventory Value", "Estimated on-hand stock value", ClinicTheme.Success);
        metricGrid.Controls.Add(totalCard.Panel, 0, 0);
        metricGrid.Controls.Add(lowStockCard.Panel, 1, 0);
        metricGrid.Controls.Add(expiringCard.Panel, 0, 1);
        metricGrid.Controls.Add(inventoryValueCard.Panel, 1, 1);

        heroLayout.Controls.Add(introPanel, 0, 0);
        heroLayout.Controls.Add(metricGrid, 1, 0);
        heroCard.Controls.Add(heroLayout);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel2,
            SplitterDistance = 820,
            SplitterWidth = 10,
            BackColor = ClinicTheme.AppBackground
        };
        split.Panel1.Padding = new Padding(8, 0, 10, 8);
        split.Panel2.Padding = new Padding(10, 0, 8, 8);

        var leftCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 16, 18, 18)
        };
        ClinicTheme.StyleCard(leftCard, ClinicTheme.Surface, 28);

        var leftLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = ClinicTheme.Surface
        };
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var toolbarModel = CreateFilterToolbar("Search by medicine, category, stock, or ID...", includeCrudButtons: true, allowEdit: allowEdit);
        var toolbar = toolbarModel.Host;
        var searchBox = toolbarModel.SearchBox;
        var newButton = toolbarModel.NewButton!;
        var editButton = toolbarModel.EditButton!;
        var deleteButton = toolbarModel.DeleteButton!;

        var bindingSource = new BindingSource();
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            DataSource = bindingSource,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false
        };
        ClinicTheme.StyleGrid(grid);
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Medicine.MedicineName),
            HeaderText = "Medicine",
            FillWeight = 158
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Medicine.Category),
            HeaderText = "Category",
            FillWeight = 116
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Medicine.Quantity),
            HeaderText = "Qty",
            FillWeight = 64
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Medicine.UnitPrice),
            HeaderText = "Unit Price",
            FillWeight = 78,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Medicine.ExpirationDate),
            HeaderText = "Expiration",
            FillWeight = 82,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "d" }
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Medicine.MedicineId),
            HeaderText = "ID",
            FillWeight = 68
        });

        leftLayout.Controls.Add(toolbar, 0, 0);
        leftLayout.Controls.Add(grid, 0, 1);
        leftCard.Controls.Add(leftLayout);

        var detailCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 18, 18, 18)
        };
        ClinicTheme.StyleCard(detailCard, ClinicTheme.Surface, 28);

        var detailLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = ClinicTheme.Surface
        };
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 202));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 122));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 126));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var profilePanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 18, 18, 18),
            Margin = new Padding(0, 0, 0, 12)
        };
        ClinicTheme.StyleCard(profilePanel, ClinicTheme.SurfaceRaised, 24);

        var quantityBadge = new Panel
        {
            Size = new Size(92, 92),
            Location = new Point(18, 18),
            BackColor = ClinicTheme.Brand
        };
        ClinicTheme.RoundControl(quantityBadge, 28);
        var quantityBadgeLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "--",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Bahnschrift SemiBold", 16f, FontStyle.Bold),
            ForeColor = Color.White
        };
        quantityBadge.Controls.Add(quantityBadgeLabel);

        var detailMedicineLabel = new Label
        {
            AutoSize = true,
            Text = "Select a medicine",
            Font = ClinicTheme.Heading,
            ForeColor = ClinicTheme.TextPrimary,
            Location = new Point(126, 24)
        };
        var detailIdLabel = new Label
        {
            AutoSize = true,
            Text = "No inventory item selected",
            Font = ClinicTheme.Body,
            ForeColor = ClinicTheme.TextSecondary,
            Location = new Point(126, 54)
        };
        var inventoryCaptionLabel = new Label
        {
            AutoSize = true,
            Text = "On-hand Value",
            Font = ClinicTheme.Caption,
            ForeColor = ClinicTheme.TextSecondary,
            Location = new Point(126, 82)
        };
        var inventoryValueLabel = new Label
        {
            AutoSize = true,
            Text = "--",
            Font = new Font("Bahnschrift SemiBold", 24f, FontStyle.Bold),
            ForeColor = ClinicTheme.TextPrimary,
            Location = new Point(124, 98)
        };
        var pillRow = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = ClinicTheme.SurfaceRaised,
            Location = new Point(18, 144)
        };
        var stockPill = ClinicTheme.CreatePill("Stock: -", ClinicTheme.SurfaceMuted, ClinicTheme.BrandDark);
        var categoryPill = ClinicTheme.CreatePill("Category: -", ClinicTheme.AccentSoft, ClinicTheme.BrandDark);
        var expiryPill = ClinicTheme.CreatePill("Expiry: -", ClinicTheme.SurfaceMuted, ClinicTheme.BrandDark);
        var usagePill = ClinicTheme.CreatePill("Prescriptions: -", ClinicTheme.SuccessSoft, ClinicTheme.Success);
        pillRow.Controls.Add(stockPill);
        pillRow.Controls.Add(categoryPill);
        pillRow.Controls.Add(expiryPill);
        pillRow.Controls.Add(usagePill);

        var detailHeroText = new Label
        {
            AutoSize = true,
            Text = "Review stock pressure, expiry risk, and prescription usage before adjusting inventory levels.",
            Font = ClinicTheme.Caption,
            ForeColor = ClinicTheme.TextSecondary,
            MaximumSize = new Size(330, 0),
            Location = new Point(18, 170)
        };

        profilePanel.Controls.Add(quantityBadge);
        profilePanel.Controls.Add(detailMedicineLabel);
        profilePanel.Controls.Add(detailIdLabel);
        profilePanel.Controls.Add(inventoryCaptionLabel);
        profilePanel.Controls.Add(inventoryValueLabel);
        profilePanel.Controls.Add(pillRow);
        profilePanel.Controls.Add(detailHeroText);

        Panel CreateDetailSection(string title, Color accent, out Label valueLabel)
        {
            var sectionPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 16, 18, 16),
                Margin = new Padding(0, 0, 0, 12)
            };
            ClinicTheme.StyleCard(sectionPanel, ClinicTheme.SurfaceRaised, 22);

            var accentBar = new Panel
            {
                Width = 34,
                Height = 4,
                BackColor = accent,
                Location = new Point(18, 16)
            };
            ClinicTheme.RoundControl(accentBar, 2);

            var titleLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 30,
                Text = title,
                Font = ClinicTheme.BodyBold,
                ForeColor = ClinicTheme.TextPrimary,
                Padding = new Padding(0, 10, 0, 0)
            };
            valueLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = ClinicTheme.Body,
                ForeColor = ClinicTheme.TextSecondary
            };

            var bodyHost = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 0),
                BackColor = Color.Transparent
            };
            bodyHost.Controls.Add(valueLabel);
            sectionPanel.Controls.Add(accentBar);
            sectionPanel.Controls.Add(bodyHost);
            sectionPanel.Controls.Add(titleLabel);
            return sectionPanel;
        }

        var stockPanel = CreateDetailSection("Stock Outlook", ClinicTheme.Brand, out var detailStockLabel);
        var usagePanel = CreateDetailSection("Prescription Impact", ClinicTheme.Success, out var detailUsageLabel);
        var expiryPanel = CreateDetailSection("Shelf Life", ClinicTheme.Accent, out var detailExpiryLabel);

        detailLayout.Controls.Add(profilePanel, 0, 0);
        detailLayout.Controls.Add(stockPanel, 0, 1);
        detailLayout.Controls.Add(usagePanel, 0, 2);
        detailLayout.Controls.Add(expiryPanel, 0, 3);
        detailCard.Controls.Add(detailLayout);

        split.Panel1.Controls.Add(leftCard);
        split.Panel2.Controls.Add(detailCard);

        Medicine? SelectedMedicine()
        {
            return grid.CurrentRow?.DataBoundItem as Medicine;
        }

        static string ValueOrPlaceholder(string value, string placeholder)
        {
            return string.IsNullOrWhiteSpace(value) ? placeholder : value.Trim();
        }

        string StockState(Medicine medicine)
        {
            if (medicine.Quantity <= 0)
            {
                return "Out of Stock";
            }

            return medicine.IsLowStock ? "Low Stock" : "Healthy";
        }

        void ApplyStockStyle(Medicine medicine)
        {
            var stockState = StockState(medicine);
            stockPill.Text = $"Stock: {stockState}";

            switch (stockState)
            {
                case "Out of Stock":
                    quantityBadge.BackColor = ClinicTheme.Danger;
                    stockPill.BackColor = ClinicTheme.DangerSoft;
                    stockPill.ForeColor = ClinicTheme.Danger;
                    break;
                case "Low Stock":
                    quantityBadge.BackColor = ClinicTheme.Accent;
                    stockPill.BackColor = ClinicTheme.AccentSoft;
                    stockPill.ForeColor = ClinicTheme.BrandDark;
                    break;
                default:
                    quantityBadge.BackColor = ClinicTheme.Success;
                    stockPill.BackColor = ClinicTheme.SuccessSoft;
                    stockPill.ForeColor = ClinicTheme.Success;
                    break;
            }
        }

        void ApplyExpiryStyle(Medicine medicine)
        {
            var daysLeft = (medicine.ExpirationDate.Date - DateTime.Today).Days;
            if (daysLeft < 0)
            {
                expiryPill.Text = "Expiry: Expired";
                expiryPill.BackColor = ClinicTheme.DangerSoft;
                expiryPill.ForeColor = ClinicTheme.Danger;
                return;
            }

            if (daysLeft <= 30)
            {
                expiryPill.Text = $"Expiry: {daysLeft} day(s)";
                expiryPill.BackColor = ClinicTheme.AccentSoft;
                expiryPill.ForeColor = ClinicTheme.BrandDark;
                return;
            }

            expiryPill.Text = $"Expiry: {medicine.ExpirationDate:MMM d}";
            expiryPill.BackColor = ClinicTheme.SurfaceMuted;
            expiryPill.ForeColor = ClinicTheme.BrandDark;
        }

        string BuildStockOutlook(Medicine medicine)
        {
            var onHandValue = medicine.Quantity * medicine.UnitPrice;
            var daysLeft = (medicine.ExpirationDate.Date - DateTime.Today).Days;
            return string.Join(
                Environment.NewLine,
                new[]
                {
                    $"Quantity on hand: {medicine.Quantity}",
                    $"Unit price: {medicine.UnitPrice:C}",
                    $"On-hand value: {onHandValue:C}",
                    $"Reorder attention: {(medicine.IsLowStock ? "Recommended now" : "Not urgent")}",
                    daysLeft < 0 ? $"Expired {Math.Abs(daysLeft)} day(s) ago" : $"Days until expiration: {daysLeft}"
                });
        }

        string BuildUsageImpact(Medicine medicine)
        {
            var items = context.Data.PrescriptionItems
                .Where(item => item.MedicineId == medicine.MedicineId)
                .OrderByDescending(item => context.Data.Consultations.FirstOrDefault(consultation => consultation.ConsultationId == item.ConsultationId)?.DateOfVisit ?? DateTime.MinValue)
                .ToList();
            var consultationCount = items.Select(item => item.ConsultationId).Distinct(StringComparer.Ordinal).Count();
            var dispensedUnits = items.Sum(item => item.Quantity);

            var lines = new List<string>
            {
                $"Prescription entries: {items.Count}",
                $"Consultations using this medicine: {consultationCount}",
                $"Total units dispensed: {dispensedUnits}"
            };

            var latestConsultation = items
                .Select(item => lookupService.FindConsultation(item.ConsultationId))
                .Where(consultation => consultation is not null)
                .OrderByDescending(consultation => consultation!.DateOfVisit)
                .FirstOrDefault();

            lines.Add(latestConsultation is null
                ? "Latest use: no prescriptions on file"
                : $"Latest use: {latestConsultation.DateOfVisit:d} for {latestConsultation.PatientName}");

            return string.Join(Environment.NewLine, lines);
        }

        string BuildShelfLife(Medicine medicine)
        {
            var daysLeft = (medicine.ExpirationDate.Date - DateTime.Today).Days;
            var inUse = context.Data.PrescriptionItems.Any(item => item.MedicineId == medicine.MedicineId);
            return string.Join(
                Environment.NewLine,
                new[]
                {
                    $"Expiration date: {medicine.ExpirationDate:d}",
                    daysLeft < 0
                        ? $"Status: expired {Math.Abs(daysLeft)} day(s) ago"
                        : daysLeft <= 30
                            ? $"Status: expiring soon in {daysLeft} day(s)"
                            : $"Status: stable for {daysLeft} more day(s)",
                    $"Delete allowed: {(inUse ? "No, referenced by prescriptions" : "Yes")}",
                    $"Category note: {ValueOrPlaceholder(medicine.Category, "No category recorded")}"
                });
        }

        void RefreshMedicineMetrics()
        {
            totalCard.ValueLabel.Text = context.Data.Medicines.Count.ToString();
            lowStockCard.ValueLabel.Text = context.Data.Medicines.Count(medicine => medicine.IsLowStock).ToString();
            expiringCard.ValueLabel.Text = context.Data.Medicines.Count(medicine =>
            {
                var daysLeft = (medicine.ExpirationDate.Date - DateTime.Today).Days;
                return daysLeft >= 0 && daysLeft <= 30;
            }).ToString();
            inventoryValueCard.ValueLabel.Text = context.Data.Medicines.Sum(medicine => medicine.Quantity * medicine.UnitPrice).ToString("C0");
        }

        void RefreshMedicineDetail()
        {
            var medicine = SelectedMedicine();
            if (medicine is null)
            {
                quantityBadge.BackColor = ClinicTheme.SurfaceMuted;
                quantityBadgeLabel.Text = "--";
                detailMedicineLabel.Text = "Select a medicine";
                detailIdLabel.Text = "No inventory item selected";
                inventoryValueLabel.Text = "--";
                stockPill.Text = "Stock: -";
                stockPill.BackColor = ClinicTheme.SurfaceMuted;
                stockPill.ForeColor = ClinicTheme.BrandDark;
                categoryPill.Text = "Category: -";
                expiryPill.Text = "Expiry: -";
                expiryPill.BackColor = ClinicTheme.SurfaceMuted;
                expiryPill.ForeColor = ClinicTheme.BrandDark;
                usagePill.Text = "Prescriptions: -";
                detailHeroText.Text = "Review stock pressure, expiry risk, and prescription usage before adjusting inventory levels.";
                detailStockLabel.Text = "Select an inventory item to inspect the current stock outlook and reorder pressure.";
                detailUsageLabel.Text = "Prescription usage and consultation impact will appear here.";
                detailExpiryLabel.Text = "Expiry state and delete eligibility will appear here.";
                return;
            }

            var usageCount = context.Data.PrescriptionItems.Count(item => item.MedicineId == medicine.MedicineId);
            quantityBadgeLabel.Text = medicine.Quantity.ToString();
            detailMedicineLabel.Text = medicine.MedicineName;
            detailIdLabel.Text = $"{medicine.MedicineId}  |  {ValueOrPlaceholder(medicine.Category, "Uncategorized")}";
            inventoryValueLabel.Text = (medicine.Quantity * medicine.UnitPrice).ToString("C2");
            categoryPill.Text = $"Category: {ValueOrPlaceholder(medicine.Category, "None")}";
            usagePill.Text = $"Prescriptions: {usageCount}";
            ApplyStockStyle(medicine);
            ApplyExpiryStyle(medicine);
            detailStockLabel.Text = BuildStockOutlook(medicine);
            detailUsageLabel.Text = BuildUsageImpact(medicine);
            detailExpiryLabel.Text = BuildShelfLife(medicine);
        }

        void ApplyFilter()
        {
            var term = searchBox.Text.Trim();
            var filtered = context.Data.Medicines
                .Where(medicine => string.IsNullOrWhiteSpace(term)
                    || $"{medicine.MedicineId} {medicine.MedicineName} {medicine.Category} {medicine.Quantity} {medicine.UnitPrice} {medicine.ExpirationDate:d}"
                        .Contains(term, StringComparison.OrdinalIgnoreCase))
                .OrderBy(medicine => medicine.IsLowStock ? 0 : 1)
                .ThenBy(medicine => medicine.ExpirationDate)
                .ThenBy(medicine => medicine.MedicineName)
                .ToList();
            bindingSource.DataSource = filtered;

            if (grid.Rows.Count > 0)
            {
                grid.ClearSelection();
                grid.Rows[0].Selected = true;
                grid.CurrentCell = grid.Rows[0].Cells[0];
            }

            RefreshMedicineMetrics();
            RefreshMedicineDetail();
        }

        void SaveSelection(string message, Medicine? selected = null)
        {
            PersistAndRefresh(message);
            if (selected is not null)
            {
                SelectItem(bindingSource, grid, searchBox, selected, ApplyFilter);
            }

            RefreshMedicineMetrics();
            RefreshMedicineDetail();
        }

        void CreateMedicine()
        {
            var medicine = new Medicine { ExpirationDate = DateTime.Today.AddYears(1) };
            if (!EditorForms.EditMedicine(this, medicine))
            {
                return;
            }

            medicine.MedicineId = IdGenerator.Next("MED", context.Data.Medicines.Select(entry => entry.MedicineId));
            context.Data.Medicines.Add(medicine);
            SyncMedicineReferences(medicine);
            SaveSelection("Medicine record saved.", medicine);
        }

        void EditSelected()
        {
            if (!allowEdit)
            {
                return;
            }

            var medicine = SelectedMedicine();
            if (medicine is null)
            {
                return;
            }

            if (!EditorForms.EditMedicine(this, medicine))
            {
                return;
            }

            SyncMedicineReferences(medicine);
            SaveSelection("Medicine record saved.", medicine);
        }

        void DeleteSelected()
        {
            var medicine = SelectedMedicine();
            if (medicine is null)
            {
                return;
            }

            if (context.Data.PrescriptionItems.Any(item => item.MedicineId == medicine.MedicineId))
            {
                MessageBox.Show(this, "Cannot delete a medicine that is already referenced in prescriptions.", "Delete Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show(this, $"Delete medicine {medicine.MedicineName}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            context.Data.Medicines.Remove(medicine);
            SaveSelection("Medicine record deleted.");
        }

        searchBox.TextChanged += (_, _) => ApplyFilter();
        newButton.Click += (_, _) => CreateMedicine();
        editButton.Click += (_, _) => EditSelected();
        deleteButton.Click += (_, _) => DeleteSelected();
        if (allowEdit)
        {
            grid.CellDoubleClick += (_, _) => EditSelected();
        }
        grid.SelectionChanged += (_, _) => RefreshMedicineDetail();
        refreshBindings.Add(ApplyFilter);

        root.Controls.Add(heroCard, 0, 0);
        root.Controls.Add(split, 0, 1);
        page.Controls.Add(root);

        RefreshMedicineMetrics();
        ApplyFilter();
        return page;
    }

    private TabPage BuildUsersTab()
    {
        var allowEdit = HasAnyRole(UserRole.Administrator);
        var page = new TabPage("Users") { BackColor = ClinicTheme.AppBackground };
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12),
            BackColor = ClinicTheme.AppBackground
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 206));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var heroCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(26, 22, 26, 22),
            Margin = new Padding(8, 8, 8, 10)
        };
        ClinicTheme.StyleCard(heroCard, ClinicTheme.Surface, 28);

        var heroLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = ClinicTheme.Surface
        };
        heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));

        var introPanel = new Panel { Dock = DockStyle.Fill, BackColor = ClinicTheme.Surface };
        var introAccent = new Panel
        {
            Width = 52,
            Height = 7,
            BackColor = ClinicTheme.BrandDark,
            Location = new Point(0, 6)
        };
        ClinicTheme.RoundControl(introAccent, 3);
        var heroTitle = new Label
        {
            AutoSize = true,
            Text = "User Administration",
            Font = ClinicTheme.DisplayMedium,
            ForeColor = ClinicTheme.TextPrimary,
            Location = new Point(0, 28)
        };
        var heroSubtitle = new Label
        {
            AutoSize = true,
            Text = "Manage clinic accounts with role visibility, activation state, and assignment risk surfaced before a user change affects appointments or consultations.",
            Font = ClinicTheme.Body,
            ForeColor = ClinicTheme.TextSecondary,
            MaximumSize = new Size(470, 0),
            Location = new Point(0, 72)
        };
        var heroPills = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = ClinicTheme.Surface,
            Location = new Point(0, 142)
        };
        heroPills.Controls.Add(ClinicTheme.CreatePill("Role-aware admin board", ClinicTheme.SurfaceMuted, ClinicTheme.BrandDark));
        heroPills.Controls.Add(ClinicTheme.CreatePill("Assignment safeguards", ClinicTheme.AccentSoft, ClinicTheme.BrandDark));
        heroPills.Controls.Add(ClinicTheme.CreatePill("Current user protected", ClinicTheme.SuccessSoft, ClinicTheme.Success));
        introPanel.Controls.Add(introAccent);
        introPanel.Controls.Add(heroTitle);
        introPanel.Controls.Add(heroSubtitle);
        introPanel.Controls.Add(heroPills);

        (Panel Panel, Label ValueLabel) CreateMetricCard(string title, string subtitle, Color accent)
        {
            return CreateModuleMetricCard(title, subtitle, accent);
        }

        var metricGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(18, 2, 0, 0),
            BackColor = ClinicTheme.Surface
        };
        metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        metricGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        metricGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var totalCard = CreateMetricCard("Total Accounts", "Users configured in the system", ClinicTheme.Brand);
        var activeCard = CreateMetricCard("Active Users", "Accounts that can log in", ClinicTheme.Success);
        var doctorsCard = CreateMetricCard("Doctors", "Users assigned clinical work", ClinicTheme.Accent);
        var inactiveCard = CreateMetricCard("Inactive", "Accounts currently disabled", ClinicTheme.Danger);
        metricGrid.Controls.Add(totalCard.Panel, 0, 0);
        metricGrid.Controls.Add(activeCard.Panel, 1, 0);
        metricGrid.Controls.Add(doctorsCard.Panel, 0, 1);
        metricGrid.Controls.Add(inactiveCard.Panel, 1, 1);

        heroLayout.Controls.Add(introPanel, 0, 0);
        heroLayout.Controls.Add(metricGrid, 1, 0);
        heroCard.Controls.Add(heroLayout);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel2,
            SplitterDistance = 820,
            SplitterWidth = 10,
            BackColor = ClinicTheme.AppBackground
        };
        split.Panel1.Padding = new Padding(8, 0, 10, 8);
        split.Panel2.Padding = new Padding(10, 0, 8, 8);

        var leftCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 16, 18, 18)
        };
        ClinicTheme.StyleCard(leftCard, ClinicTheme.Surface, 28);

        var leftLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = ClinicTheme.Surface
        };
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var toolbarModel = CreateFilterToolbar("Search by name, username, role, or ID...", includeCrudButtons: true, allowEdit: allowEdit);
        var toolbar = toolbarModel.Host;
        var searchBox = toolbarModel.SearchBox;
        var newButton = toolbarModel.NewButton!;
        var editButton = toolbarModel.EditButton!;
        var deleteButton = toolbarModel.DeleteButton!;

        var bindingSource = new BindingSource();
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            DataSource = bindingSource,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false
        };
        ClinicTheme.StyleGrid(grid);
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(User.FullName),
            HeaderText = "Name",
            FillWeight = 152
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(User.Username),
            HeaderText = "Username",
            FillWeight = 112
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(User.Role),
            HeaderText = "Role",
            FillWeight = 90
        });
        grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(User.IsActive),
            HeaderText = "Active",
            FillWeight = 58
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(User.UserId),
            HeaderText = "ID",
            FillWeight = 74
        });

        leftLayout.Controls.Add(toolbar, 0, 0);
        leftLayout.Controls.Add(grid, 0, 1);
        leftCard.Controls.Add(leftLayout);

        var detailCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 18, 18, 18)
        };
        ClinicTheme.StyleCard(detailCard, ClinicTheme.Surface, 28);

        var detailLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = ClinicTheme.Surface
        };
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 192));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var profilePanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 18, 18, 18),
            Margin = new Padding(0, 0, 0, 12)
        };
        ClinicTheme.StyleCard(profilePanel, ClinicTheme.SurfaceRaised, 24);

        var roleBadge = new Panel
        {
            Size = new Size(92, 92),
            Location = new Point(18, 18),
            BackColor = ClinicTheme.Brand
        };
        ClinicTheme.RoundControl(roleBadge, 28);
        var roleBadgeLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "--",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Bahnschrift SemiBold", 18f, FontStyle.Bold),
            ForeColor = Color.White
        };
        roleBadge.Controls.Add(roleBadgeLabel);

        var detailNameLabel = new Label
        {
            AutoSize = true,
            Text = "Select a user",
            Font = ClinicTheme.Heading,
            ForeColor = ClinicTheme.TextPrimary,
            Location = new Point(126, 24)
        };
        var detailIdentityLabel = new Label
        {
            AutoSize = true,
            Text = "No account selected",
            Font = ClinicTheme.Body,
            ForeColor = ClinicTheme.TextSecondary,
            Location = new Point(126, 54)
        };
        var pillRow = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = ClinicTheme.SurfaceRaised,
            Location = new Point(126, 92)
        };
        var rolePill = ClinicTheme.CreatePill("Role: -", ClinicTheme.SurfaceMuted, ClinicTheme.BrandDark);
        var statusPill = ClinicTheme.CreatePill("Status: -", ClinicTheme.AccentSoft, ClinicTheme.BrandDark);
        var assignmentPill = ClinicTheme.CreatePill("Assignments: -", ClinicTheme.SuccessSoft, ClinicTheme.Success);
        var sessionPill = ClinicTheme.CreatePill("Session: -", ClinicTheme.DangerSoft, ClinicTheme.Danger);
        pillRow.Controls.Add(rolePill);
        pillRow.Controls.Add(statusPill);
        pillRow.Controls.Add(assignmentPill);
        pillRow.Controls.Add(sessionPill);

        var detailHeroText = new Label
        {
            AutoSize = true,
            Text = "Review role scope, active state, and assignment risk before changing account access.",
            Font = ClinicTheme.Caption,
            ForeColor = ClinicTheme.TextSecondary,
            MaximumSize = new Size(320, 0),
            Location = new Point(18, 126)
        };

        profilePanel.Controls.Add(roleBadge);
        profilePanel.Controls.Add(detailNameLabel);
        profilePanel.Controls.Add(detailIdentityLabel);
        profilePanel.Controls.Add(pillRow);
        profilePanel.Controls.Add(detailHeroText);

        Panel CreateDetailSection(string title, Color accent, out Label valueLabel)
        {
            var sectionPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 16, 18, 16),
                Margin = new Padding(0, 0, 0, 12)
            };
            ClinicTheme.StyleCard(sectionPanel, ClinicTheme.SurfaceRaised, 22);

            var accentBar = new Panel
            {
                Width = 34,
                Height = 4,
                BackColor = accent,
                Location = new Point(18, 16)
            };
            ClinicTheme.RoundControl(accentBar, 2);

            var titleLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 30,
                Text = title,
                Font = ClinicTheme.BodyBold,
                ForeColor = ClinicTheme.TextPrimary,
                Padding = new Padding(0, 10, 0, 0)
            };
            valueLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = ClinicTheme.Body,
                ForeColor = ClinicTheme.TextSecondary
            };

            var bodyHost = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 0),
                BackColor = Color.Transparent
            };
            bodyHost.Controls.Add(valueLabel);
            sectionPanel.Controls.Add(accentBar);
            sectionPanel.Controls.Add(bodyHost);
            sectionPanel.Controls.Add(titleLabel);
            return sectionPanel;
        }

        var scopePanel = CreateDetailSection("Access Scope", ClinicTheme.Brand, out var detailScopeLabel);
        var assignmentPanel = CreateDetailSection("Assignment Context", ClinicTheme.Accent, out var detailAssignmentLabel);
        var notesPanel = CreateDetailSection("Account Notes", ClinicTheme.Danger, out var detailNotesLabel);

        detailLayout.Controls.Add(profilePanel, 0, 0);
        detailLayout.Controls.Add(scopePanel, 0, 1);
        detailLayout.Controls.Add(assignmentPanel, 0, 2);
        detailLayout.Controls.Add(notesPanel, 0, 3);
        detailCard.Controls.Add(detailLayout);

        split.Panel1.Controls.Add(leftCard);
        split.Panel2.Controls.Add(detailCard);

        User? SelectedUser()
        {
            return grid.CurrentRow?.DataBoundItem as User;
        }

        static string ValueOrPlaceholder(string value, string placeholder)
        {
            return string.IsNullOrWhiteSpace(value) ? placeholder : value.Trim();
        }

        bool CanDeleteUser(User user)
        {
            return user.UserId != context.CurrentUser?.UserId
                && !context.Data.Appointments.Any(appointment => appointment.DoctorAssigned == user.FullName)
                && !context.Data.Consultations.Any(consultation => consultation.Doctor == user.FullName);
        }

        string DeleteBlockedReason(User user)
        {
            return user.UserId == context.CurrentUser?.UserId
                ? "You cannot delete the currently logged-in user."
                : "Cannot delete a user who is linked to appointments or consultations.";
        }

        static string RoleBadgeText(UserRole role)
        {
            return role switch
            {
                UserRole.Administrator => "ADM",
                UserRole.Doctor => "DOC",
                _ => "REC"
            };
        }

        void ApplyRoleStyle(User user)
        {
            roleBadgeLabel.Text = RoleBadgeText(user.Role);
            rolePill.Text = $"Role: {user.Role}";

            switch (user.Role)
            {
                case UserRole.Administrator:
                    roleBadge.BackColor = ClinicTheme.BrandDark;
                    rolePill.BackColor = ClinicTheme.SurfaceMuted;
                    rolePill.ForeColor = ClinicTheme.BrandDark;
                    break;
                case UserRole.Doctor:
                    roleBadge.BackColor = ClinicTheme.Success;
                    rolePill.BackColor = ClinicTheme.SuccessSoft;
                    rolePill.ForeColor = ClinicTheme.Success;
                    break;
                default:
                    roleBadge.BackColor = ClinicTheme.Accent;
                    rolePill.BackColor = ClinicTheme.AccentSoft;
                    rolePill.ForeColor = ClinicTheme.BrandDark;
                    break;
            }
        }

        void ApplyStatusStyle(User user)
        {
            statusPill.Text = user.IsActive ? "Status: Active" : "Status: Inactive";
            if (user.IsActive)
            {
                statusPill.BackColor = ClinicTheme.SuccessSoft;
                statusPill.ForeColor = ClinicTheme.Success;
                return;
            }

            statusPill.BackColor = ClinicTheme.DangerSoft;
            statusPill.ForeColor = ClinicTheme.Danger;
        }

        string BuildAccessScope(User user)
        {
            return user.Role switch
            {
                UserRole.Administrator => string.Join(
                    Environment.NewLine,
                    new[]
                    {
                        "Modules: Dashboard, Patients, Patient History, Appointments, Consultations, Billing, Medicines, Reports, Users",
                        "Administrative access includes account management and inventory oversight."
                    }),
                UserRole.Doctor => string.Join(
                    Environment.NewLine,
                    new[]
                    {
                        "Modules: Dashboard, Patients, Patient History, Appointments, Consultations, Medicines, Reports",
                        "Clinical access focuses on consultations, prescriptions, and patient review."
                    }),
                _ => string.Join(
                    Environment.NewLine,
                    new[]
                    {
                        "Modules: Dashboard, Patients, Patient History, Appointments, Billing, Reports",
                        "Front-desk access supports registration, scheduling, and payment intake."
                    })
            };
        }

        string BuildAssignmentContext(User user)
        {
            var appointmentCount = context.Data.Appointments.Count(appointment => appointment.DoctorAssigned == user.FullName);
            var consultationCount = context.Data.Consultations.Count(consultation => consultation.Doctor == user.FullName);
            var totalAssignments = appointmentCount + consultationCount;

            if (user.Role != UserRole.Doctor)
            {
                return string.Join(
                    Environment.NewLine,
                    new[]
                    {
                        "This user is not a doctor account.",
                        $"Doctor-linked appointments: {appointmentCount}",
                        $"Doctor-linked consultations: {consultationCount}",
                        totalAssignments == 0
                            ? "No clinical assignments are tied to this profile."
                            : "This profile still appears in doctor assignments and should be reviewed carefully."
                    });
            }

            var nextAppointment = context.Data.Appointments
                .Where(appointment => appointment.DoctorAssigned == user.FullName)
                .OrderBy(appointment => appointment.AppointmentDate)
                .ThenBy(appointment => appointment.AppointmentTime)
                .FirstOrDefault();
            var latestConsultation = context.Data.Consultations
                .Where(consultation => consultation.Doctor == user.FullName)
                .OrderByDescending(consultation => consultation.DateOfVisit)
                .FirstOrDefault();

            return string.Join(
                Environment.NewLine,
                new[]
                {
                    $"Appointments assigned: {appointmentCount}",
                    $"Consultations assigned: {consultationCount}",
                    nextAppointment is null
                        ? "Next appointment: none scheduled"
                        : $"Next appointment: {nextAppointment.AppointmentDate:d} | {nextAppointment.PatientName}",
                    latestConsultation is null
                        ? "Latest consultation: none recorded"
                        : $"Latest consultation: {latestConsultation.DateOfVisit:d} | {latestConsultation.PatientName}"
                });
        }

        string BuildAccountNotes(User user)
        {
            var notes = new List<string>
            {
                $"Current session user: {(user.UserId == context.CurrentUser?.UserId ? "Yes" : "No")}",
                $"Delete allowed: {(CanDeleteUser(user) ? "Yes" : "No")}",
                "Username must remain unique."
            };

            if (user.Role == UserRole.Doctor)
            {
                var doctorAssignmentsExist = context.Data.Appointments.Any(appointment => appointment.DoctorAssigned == user.FullName)
                    || context.Data.Consultations.Any(consultation => consultation.Doctor == user.FullName);
                notes.Add(doctorAssignmentsExist
                    ? "Role change or deactivation is blocked while assignments exist."
                    : "No doctor assignments block role change or deactivation.");
            }

            if (!user.IsActive)
            {
                notes.Add("Inactive users cannot log in until reactivated.");
            }

            return string.Join(Environment.NewLine, notes);
        }

        void RefreshUserMetrics()
        {
            totalCard.ValueLabel.Text = context.Data.Users.Count.ToString();
            activeCard.ValueLabel.Text = context.Data.Users.Count(user => user.IsActive).ToString();
            doctorsCard.ValueLabel.Text = context.Data.Users.Count(user => user.Role == UserRole.Doctor).ToString();
            inactiveCard.ValueLabel.Text = context.Data.Users.Count(user => !user.IsActive).ToString();
        }

        void RefreshUserDetail()
        {
            var user = SelectedUser();
            if (user is null)
            {
                roleBadge.BackColor = ClinicTheme.SurfaceMuted;
                roleBadgeLabel.Text = "--";
                detailNameLabel.Text = "Select a user";
                detailIdentityLabel.Text = "No account selected";
                rolePill.Text = "Role: -";
                rolePill.BackColor = ClinicTheme.SurfaceMuted;
                rolePill.ForeColor = ClinicTheme.BrandDark;
                statusPill.Text = "Status: -";
                statusPill.BackColor = ClinicTheme.AccentSoft;
                statusPill.ForeColor = ClinicTheme.BrandDark;
                assignmentPill.Text = "Assignments: -";
                assignmentPill.BackColor = ClinicTheme.SuccessSoft;
                assignmentPill.ForeColor = ClinicTheme.Success;
                sessionPill.Text = "Session: -";
                sessionPill.BackColor = ClinicTheme.DangerSoft;
                sessionPill.ForeColor = ClinicTheme.Danger;
                detailScopeLabel.Text = "Select an account to review which modules this role can access.";
                detailAssignmentLabel.Text = "Assigned appointments and consultations will appear here.";
                detailNotesLabel.Text = "Account safeguards and delete restrictions will appear here.";
                return;
            }

            var assignmentCount = context.Data.Appointments.Count(appointment => appointment.DoctorAssigned == user.FullName)
                + context.Data.Consultations.Count(consultation => consultation.Doctor == user.FullName);

            ApplyRoleStyle(user);
            ApplyStatusStyle(user);
            detailNameLabel.Text = ValueOrPlaceholder(user.FullName, "Unnamed user");
            detailIdentityLabel.Text = $"{user.UserId}  |  @{ValueOrPlaceholder(user.Username, "no-username")}";
            assignmentPill.Text = $"Assignments: {assignmentCount}";
            sessionPill.Text = user.UserId == context.CurrentUser?.UserId ? "Session: Current" : "Session: Other";
            sessionPill.BackColor = user.UserId == context.CurrentUser?.UserId ? ClinicTheme.SuccessSoft : ClinicTheme.SurfaceMuted;
            sessionPill.ForeColor = user.UserId == context.CurrentUser?.UserId ? ClinicTheme.Success : ClinicTheme.BrandDark;
            detailScopeLabel.Text = BuildAccessScope(user);
            detailAssignmentLabel.Text = BuildAssignmentContext(user);
            detailNotesLabel.Text = BuildAccountNotes(user);
        }

        void ApplyFilter()
        {
            var term = searchBox.Text.Trim();
            var filtered = context.Data.Users
                .Where(user => string.IsNullOrWhiteSpace(term)
                    || $"{user.UserId} {user.Username} {user.FullName} {user.Role} {(user.IsActive ? "Active" : "Inactive")}"
                        .Contains(term, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(user => user.IsActive)
                .ThenBy(user => user.Role)
                .ThenBy(user => user.FullName)
                .ToList();
            bindingSource.DataSource = filtered;

            if (grid.Rows.Count > 0)
            {
                grid.ClearSelection();
                grid.Rows[0].Selected = true;
                grid.CurrentCell = grid.Rows[0].Cells[0];
            }

            RefreshUserMetrics();
            RefreshUserDetail();
        }

        void SaveSelection(string message, User? selected = null)
        {
            PersistAndRefresh(message);
            if (selected is not null)
            {
                SelectItem(bindingSource, grid, searchBox, selected, ApplyFilter);
            }

            RefreshUserMetrics();
            RefreshUserDetail();
        }

        void CreateUser()
        {
            var user = new User
            {
                IsActive = true,
                Password = "changeme",
                Role = UserRole.Receptionist
            };

            if (!EditUser(user))
            {
                return;
            }

            user.UserId = IdGenerator.Next("USR", context.Data.Users.Select(entry => entry.UserId));
            context.Data.Users.Add(user);
            SaveSelection("User record saved.", user);
        }

        void EditSelected()
        {
            if (!allowEdit)
            {
                return;
            }

            var user = SelectedUser();
            if (user is null)
            {
                return;
            }

            if (!EditUser(user))
            {
                return;
            }

            SaveSelection("User record saved.", user);
        }

        void DeleteSelected()
        {
            var user = SelectedUser();
            if (user is null)
            {
                return;
            }

            if (!CanDeleteUser(user))
            {
                MessageBox.Show(this, DeleteBlockedReason(user), "Delete Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show(this, $"Delete user {user.Username}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            context.Data.Users.Remove(user);
            SaveSelection("User record deleted.");
        }

        searchBox.TextChanged += (_, _) => ApplyFilter();
        newButton.Click += (_, _) => CreateUser();
        editButton.Click += (_, _) => EditSelected();
        deleteButton.Click += (_, _) => DeleteSelected();
        if (allowEdit)
        {
            grid.CellDoubleClick += (_, _) => EditSelected();
        }
        grid.SelectionChanged += (_, _) => RefreshUserDetail();
        refreshBindings.Add(ApplyFilter);

        root.Controls.Add(heroCard, 0, 0);
        root.Controls.Add(split, 0, 1);
        page.Controls.Add(root);

        RefreshUserMetrics();
        ApplyFilter();
        return page;
    }

    private bool EditUser(User user)
    {
        var previousFullName = user.FullName;
        var accepted = EditorForms.EditUser(this, user, userRulesService, context.CurrentUser);
        if (accepted && user.Role == UserRole.Doctor)
        {
            lookupService.SyncDoctorReferences(previousFullName, user);
        }

        return accepted;
    }

    private TabPage BuildReportsTab()
    {
        var page = new TabPage("Reports") { BackColor = ClinicTheme.AppBackground };
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12),
            BackColor = ClinicTheme.AppBackground
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 206));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var heroCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(26, 22, 26, 22),
            Margin = new Padding(8, 8, 8, 10)
        };
        ClinicTheme.StyleCard(heroCard, ClinicTheme.Surface, 28);

        var heroLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = ClinicTheme.Surface
        };
        heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));

        var introPanel = new Panel { Dock = DockStyle.Fill, BackColor = ClinicTheme.Surface };
        var introAccent = new Panel
        {
            Width = 52,
            Height = 7,
            BackColor = ClinicTheme.Brand,
            Location = new Point(0, 6)
        };
        ClinicTheme.RoundControl(introAccent, 3);
        var heroTitle = new Label
        {
            AutoSize = true,
            Text = "Operational Reports",
            Font = ClinicTheme.DisplayMedium,
            ForeColor = ClinicTheme.TextPrimary,
            Location = new Point(0, 28)
        };
        var heroSubtitle = new Label
        {
            AutoSize = true,
            Text = "Monitor clinic activity, finance, prescriptions, and inventory from one reporting workspace, then export the same snapshot for submission or review.",
            Font = ClinicTheme.Body,
            ForeColor = ClinicTheme.TextSecondary,
            MaximumSize = new Size(470, 0),
            Location = new Point(0, 72)
        };
        var heroPills = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = ClinicTheme.Surface,
            Location = new Point(0, 142)
        };
        heroPills.Controls.Add(ClinicTheme.CreatePill("Daily operations view", ClinicTheme.AccentSoft, ClinicTheme.BrandDark));
        heroPills.Controls.Add(ClinicTheme.CreatePill("Finance snapshot", ClinicTheme.SuccessSoft, ClinicTheme.Success));
        heroPills.Controls.Add(ClinicTheme.CreatePill("Export-ready summary", ClinicTheme.SurfaceMuted, ClinicTheme.BrandDark));
        introPanel.Controls.Add(introAccent);
        introPanel.Controls.Add(heroTitle);
        introPanel.Controls.Add(heroSubtitle);
        introPanel.Controls.Add(heroPills);

        (Panel Panel, Label ValueLabel) CreateMetricCard(string title, string subtitle, Color accent)
        {
            return CreateModuleMetricCard(title, subtitle, accent);
        }

        var metricGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(18, 2, 0, 0),
            BackColor = ClinicTheme.Surface
        };
        metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        metricGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        metricGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var consultationsCard = CreateMetricCard("Consultations", "Visits in selected range", ClinicTheme.Brand);
        var appointmentsCard = CreateMetricCard("Appointments", "Schedule in selected range", ClinicTheme.Accent);
        var openBillsCard = CreateMetricCard("Open Bills", "Linked records in selected range", ClinicTheme.Danger);
        var lowStockCard = CreateMetricCard("Low Stock", "Current inventory pressure", ClinicTheme.Success);
        metricGrid.Controls.Add(consultationsCard.Panel, 0, 0);
        metricGrid.Controls.Add(appointmentsCard.Panel, 1, 0);
        metricGrid.Controls.Add(openBillsCard.Panel, 0, 1);
        metricGrid.Controls.Add(lowStockCard.Panel, 1, 1);

        heroLayout.Controls.Add(introPanel, 0, 0);
        heroLayout.Controls.Add(metricGrid, 1, 0);
        heroCard.Controls.Add(heroLayout);

        var workspaceCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22, 18, 22, 18),
            Margin = new Padding(8, 0, 8, 8)
        };
        ClinicTheme.StyleCard(workspaceCard, ClinicTheme.Surface, 28);

        var workspaceLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = ClinicTheme.Surface
        };
        workspaceLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        workspaceLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        workspaceLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        workspaceLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var controlsRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = ClinicTheme.Surface
        };
        controlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        controlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var filterRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            BackColor = ClinicTheme.Surface,
            Padding = new Padding(0, 8, 0, 0),
            Margin = new Padding(0)
        };
        var fromLabel = new Label
        {
            AutoSize = true,
            Text = "From",
            Font = ClinicTheme.BodyBold,
            ForeColor = ClinicTheme.TextPrimary,
            Margin = new Padding(0, 10, 8, 0)
        };
        var fromPicker = new DateTimePicker
        {
            Width = 138,
            Font = ClinicTheme.Body,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "MMM dd, yyyy",
            Value = DateTime.Today,
            Margin = new Padding(0, 4, 14, 0)
        };
        var toLabel = new Label
        {
            AutoSize = true,
            Text = "To",
            Font = ClinicTheme.BodyBold,
            ForeColor = ClinicTheme.TextPrimary,
            Margin = new Padding(0, 10, 8, 0)
        };
        var toPicker = new DateTimePicker
        {
            Width = 138,
            Font = ClinicTheme.Body,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "MMM dd, yyyy",
            Value = DateTime.Today,
            Margin = new Padding(0, 4, 0, 0)
        };
        filterRow.Controls.Add(fromLabel);
        filterRow.Controls.Add(fromPicker);
        filterRow.Controls.Add(toLabel);
        filterRow.Controls.Add(toPicker);

        var actionsRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            BackColor = ClinicTheme.Surface,
            Margin = new Padding(0),
            Padding = new Padding(0, 6, 0, 0)
        };
        var openPreviewButton = new Button
        {
            Text = "Open Report Preview",
            Width = 184,
            Height = 38,
            Margin = new Padding(0)
        };
        ClinicTheme.StylePrimaryButton(openPreviewButton);
        actionsRow.Controls.Add(openPreviewButton);

        var rangeStatusLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Current range: -",
            Font = ClinicTheme.Caption,
            ForeColor = ClinicTheme.TextSecondary,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var workspaceHintLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Preview opens in a dedicated modal with section cards, tables, refresh, and export.",
            Font = ClinicTheme.Caption,
            ForeColor = ClinicTheme.TextSecondary,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var workspaceSummaryLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = ClinicTheme.Body,
            ForeColor = ClinicTheme.TextSecondary,
            Padding = new Padding(0, 8, 0, 0)
        };

        controlsRow.Controls.Add(filterRow, 0, 0);
        controlsRow.Controls.Add(actionsRow, 1, 0);
        workspaceLayout.Controls.Add(controlsRow, 0, 0);
        workspaceLayout.Controls.Add(rangeStatusLabel, 0, 1);
        workspaceLayout.Controls.Add(workspaceHintLabel, 0, 2);
        workspaceLayout.Controls.Add(workspaceSummaryLabel, 0, 3);
        workspaceCard.Controls.Add(workspaceLayout);

        void RefreshReportSummary()
        {
            var snapshot = BuildReportSnapshot(fromPicker.Value.Date, toPicker.Value.Date);
            consultationsCard.ValueLabel.Text = snapshot.ConsultationsCount.ToString(CultureInfo.InvariantCulture);
            appointmentsCard.ValueLabel.Text = snapshot.AppointmentsCount.ToString(CultureInfo.InvariantCulture);
            openBillsCard.ValueLabel.Text = snapshot.OpenBillsCount.ToString(CultureInfo.InvariantCulture);
            lowStockCard.ValueLabel.Text = snapshot.LowStockItems.Count.ToString(CultureInfo.InvariantCulture);

            rangeStatusLabel.Text = snapshot.PeriodStart == snapshot.PeriodEnd
                ? $"Current range: {snapshot.PeriodStart:MMM dd, yyyy}"
                : $"Current range: {snapshot.PeriodStart:MMM dd, yyyy} - {snapshot.PeriodEnd:MMM dd, yyyy}";

            var rangeDays = (snapshot.PeriodEnd - snapshot.PeriodStart).Days + 1;
            var topWatchlist = snapshot.LowStockItems
                .Take(3)
                .Select(medicine => $"{medicine.MedicineName} ({medicine.Quantity} left)")
                .ToList();
            var watchlistText = topWatchlist.Count == 0
                ? "No low-stock medicines in the current inventory snapshot."
                : $"Low-stock watchlist: {string.Join(", ", topWatchlist)}";

            workspaceSummaryLabel.Text = string.Join(
                Environment.NewLine,
                new[]
                {
                    $"Range span: {rangeDays} day(s) | Completed visits: {snapshot.CompletedConsultationsCount} consult / {snapshot.CompletedAppointmentsCount} appointments.",
                    $"Billing in range: {snapshot.OpenBillsCount} open, paid revenue {snapshot.PaidRevenue:C}.",
                    watchlistText,
                    "Use Open Report Preview for section cards, detailed tables, refresh, and CSV export."
                });
        }

        void HandleRangeChanged(DateTimePicker changedPicker)
        {
            if (fromPicker.Value.Date > toPicker.Value.Date)
            {
                if (ReferenceEquals(changedPicker, fromPicker))
                {
                    toPicker.Value = fromPicker.Value.Date;
                }
                else
                {
                    fromPicker.Value = toPicker.Value.Date;
                }

                return;
            }

            RefreshReportSummary();
        }

        fromPicker.ValueChanged += (_, _) => HandleRangeChanged(fromPicker);
        toPicker.ValueChanged += (_, _) => HandleRangeChanged(toPicker);
        openPreviewButton.Click += (_, _) => ShowReportPreviewModal(fromPicker.Value.Date, toPicker.Value.Date);
        refreshReports.Add(RefreshReportSummary);
        RefreshReportSummary();

        root.Controls.Add(heroCard, 0, 0);
        root.Controls.Add(workspaceCard, 0, 1);
        page.Controls.Add(root);
        return page;
    }

    private void ShowReportPreviewModal(DateTime initialStart, DateTime initialEnd)
    {
        if (initialStart.Date > initialEnd.Date)
        {
            (initialStart, initialEnd) = (initialEnd, initialStart);
        }

        using var modal = new Form
        {
            Text = "Report Preview",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.Sizable,
            MinimizeBox = false,
            MaximizeBox = true,
            ShowInTaskbar = false,
            Size = new Size(1180, 760),
            MinimumSize = new Size(980, 680)
        };
        ClinicTheme.StyleSurface(modal);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = ClinicTheme.AppBackground,
            Padding = new Padding(14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var headerCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 14, 16, 12)
        };
        ClinicTheme.StyleCard(headerCard, ClinicTheme.Surface, 22);

        var headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = ClinicTheme.Surface
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var headerTextPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ClinicTheme.Surface
        };
        var titleLabel = new Label
        {
            AutoSize = true,
            Text = "Report Preview",
            Font = ClinicTheme.Heading,
            ForeColor = ClinicTheme.TextPrimary,
            Location = new Point(0, 0)
        };
        var subtitleLabel = new Label
        {
            AutoSize = true,
            Text = "Section cards + operational tables from the selected date range.",
            Font = ClinicTheme.Caption,
            ForeColor = ClinicTheme.TextSecondary,
            Location = new Point(0, 28)
        };
        headerTextPanel.Controls.Add(titleLabel);
        headerTextPanel.Controls.Add(subtitleLabel);

        var rangePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            BackColor = ClinicTheme.Surface,
            Margin = new Padding(0),
            Padding = new Padding(0, 2, 0, 0)
        };
        var fromLabel = new Label
        {
            AutoSize = true,
            Text = "From",
            Font = ClinicTheme.BodyBold,
            ForeColor = ClinicTheme.TextPrimary,
            Margin = new Padding(0, 9, 8, 0)
        };
        var fromPicker = new DateTimePicker
        {
            Width = 138,
            Font = ClinicTheme.Body,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "MMM dd, yyyy",
            Value = initialStart.Date,
            Margin = new Padding(0, 4, 12, 0)
        };
        var toLabel = new Label
        {
            AutoSize = true,
            Text = "To",
            Font = ClinicTheme.BodyBold,
            ForeColor = ClinicTheme.TextPrimary,
            Margin = new Padding(0, 9, 8, 0)
        };
        var toPicker = new DateTimePicker
        {
            Width = 138,
            Font = ClinicTheme.Body,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "MMM dd, yyyy",
            Value = initialEnd.Date,
            Margin = new Padding(0, 4, 0, 0)
        };
        rangePanel.Controls.Add(fromLabel);
        rangePanel.Controls.Add(fromPicker);
        rangePanel.Controls.Add(toLabel);
        rangePanel.Controls.Add(toPicker);

        var actionsPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.None,
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = ClinicTheme.Surface,
            Margin = new Padding(0),
            Padding = new Padding(6, 2, 0, 0)
        };
        var closeButton = new Button
        {
            Text = "Close",
            Width = 110,
            Height = 38,
            Margin = new Padding(8, 0, 0, 0)
        };
        ClinicTheme.StyleSecondaryButton(closeButton);
        var exportButton = new Button
        {
            Text = "Export CSV",
            Width = 128,
            Height = 38,
            Margin = new Padding(8, 0, 0, 0)
        };
        ClinicTheme.StyleSecondaryButton(exportButton);
        var refreshButton = new Button
        {
            Text = "Refresh",
            Width = 110,
            Height = 38,
            Margin = new Padding(0)
        };
        ClinicTheme.StylePrimaryButton(refreshButton);
        actionsPanel.Controls.Add(closeButton);
        actionsPanel.Controls.Add(exportButton);
        actionsPanel.Controls.Add(refreshButton);

        headerLayout.Controls.Add(headerTextPanel, 0, 0);
        headerLayout.Controls.Add(rangePanel, 1, 0);
        headerLayout.Controls.Add(actionsPanel, 2, 0);
        headerCard.Controls.Add(headerLayout);

        var metricCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 8, 12, 8)
        };
        ClinicTheme.StyleCard(metricCard, ClinicTheme.Surface, 22);

        var metricGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = ClinicTheme.Surface
        };
        metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        var consultCard = CreateModuleMetricCard("Consultations", "Range total", ClinicTheme.Brand);
        var appointmentCard = CreateModuleMetricCard("Appointments", "Range total", ClinicTheme.Accent);
        var openBillCard = CreateModuleMetricCard("Open Bills", "Unpaid or partial", ClinicTheme.Danger);
        var revenueCard = CreateModuleMetricCard("Paid Revenue", "Linked in range", ClinicTheme.Success);
        metricGrid.Controls.Add(consultCard.Panel, 0, 0);
        metricGrid.Controls.Add(appointmentCard.Panel, 1, 0);
        metricGrid.Controls.Add(openBillCard.Panel, 2, 0);
        metricGrid.Controls.Add(revenueCard.Panel, 3, 0);
        metricCard.Controls.Add(metricGrid);

        var bodyCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 12, 12, 12)
        };
        ClinicTheme.StyleCard(bodyCard, ClinicTheme.Surface, 22);

        DataGridView CreateReportGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = ClinicTheme.Surface,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false
            };
            ClinicTheme.StyleGrid(grid);
            return grid;
        }

        var appointmentsGrid = CreateReportGrid();
        var consultationsGrid = CreateReportGrid();
        var billingGrid = CreateReportGrid();
        var prescriptionsGrid = CreateReportGrid();
        var lowStockGrid = CreateReportGrid();

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Appearance = TabAppearance.Normal
        };

        var appointmentsPage = new TabPage("Appointments") { BackColor = ClinicTheme.Surface };
        var consultationsPage = new TabPage("Consultations") { BackColor = ClinicTheme.Surface };
        var billingPage = new TabPage("Billing") { BackColor = ClinicTheme.Surface };
        var prescriptionsPage = new TabPage("Prescriptions") { BackColor = ClinicTheme.Surface };
        var inventoryPage = new TabPage("Low Stock") { BackColor = ClinicTheme.Surface };

        appointmentsPage.Controls.Add(appointmentsGrid);
        consultationsPage.Controls.Add(consultationsGrid);
        billingPage.Controls.Add(billingGrid);
        prescriptionsPage.Controls.Add(prescriptionsGrid);
        inventoryPage.Controls.Add(lowStockGrid);
        tabs.TabPages.Add(appointmentsPage);
        tabs.TabPages.Add(consultationsPage);
        tabs.TabPages.Add(billingPage);
        tabs.TabPages.Add(prescriptionsPage);
        tabs.TabPages.Add(inventoryPage);
        bodyCard.Controls.Add(tabs);

        void RefreshPreview()
        {
            if (fromPicker.Value.Date > toPicker.Value.Date)
            {
                toPicker.Value = fromPicker.Value.Date;
                return;
            }

            var snapshot = BuildReportSnapshot(fromPicker.Value.Date, toPicker.Value.Date);

            subtitleLabel.Text = snapshot.PeriodStart == snapshot.PeriodEnd
                ? $"Snapshot date: {snapshot.PeriodStart:MMM dd, yyyy}"
                : $"Snapshot range: {snapshot.PeriodStart:MMM dd, yyyy} - {snapshot.PeriodEnd:MMM dd, yyyy}";

            consultCard.ValueLabel.Text = snapshot.ConsultationsCount.ToString(CultureInfo.InvariantCulture);
            appointmentCard.ValueLabel.Text = snapshot.AppointmentsCount.ToString(CultureInfo.InvariantCulture);
            openBillCard.ValueLabel.Text = snapshot.OpenBillsCount.ToString(CultureInfo.InvariantCulture);
            revenueCard.ValueLabel.Text = snapshot.PaidRevenue.ToString("0.00", CultureInfo.InvariantCulture);

            appointmentsGrid.DataSource = snapshot.Appointments
                .Select(
                    appointment => new
                    {
                        appointment.AppointmentId,
                        appointment.PatientName,
                        appointment.DoctorAssigned,
                        Date = appointment.AppointmentDate.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture),
                        Time = appointment.AppointmentTime,
                        Status = appointment.Status.ToString()
                    })
                .ToList();

            consultationsGrid.DataSource = snapshot.Consultations
                .Select(
                    consultation => new
                    {
                        consultation.ConsultationId,
                        consultation.PatientName,
                        consultation.Doctor,
                        VisitDate = consultation.DateOfVisit.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture),
                        Status = consultation.Status.ToString(),
                        Diagnosis = TextOrFallback(consultation.Diagnosis, "Not recorded")
                    })
                .ToList();

            billingGrid.DataSource = snapshot.BillingRecords
                .Select(
                    billing => new
                    {
                        billing.BillingId,
                        billing.PatientName,
                        billing.ConsultationId,
                        Service = billing.ServiceCharges.ToString("0.00", CultureInfo.InvariantCulture),
                        Medicine = billing.MedicineCharges.ToString("0.00", CultureInfo.InvariantCulture),
                        Total = billing.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture),
                        Status = billing.PaymentStatus.ToString()
                    })
                .ToList();

            prescriptionsGrid.DataSource = snapshot.PrescriptionItems
                .Select(
                    item => new
                    {
                        item.PrescriptionItemId,
                        item.ConsultationId,
                        item.MedicineName,
                        item.Dosage,
                        item.Quantity,
                        UnitPrice = item.UnitPrice.ToString("0.00", CultureInfo.InvariantCulture),
                        TotalCost = item.TotalCost.ToString("0.00", CultureInfo.InvariantCulture)
                    })
                .ToList();

            lowStockGrid.DataSource = snapshot.LowStockItems
                .Select(
                    medicine => new
                    {
                        medicine.MedicineId,
                        medicine.MedicineName,
                        medicine.Category,
                        medicine.Quantity,
                        UnitPrice = medicine.UnitPrice.ToString("0.00", CultureInfo.InvariantCulture),
                        Expiration = medicine.ExpirationDate.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture)
                    })
                .ToList();
        }

        refreshButton.Click += (_, _) => RefreshPreview();
        exportButton.Click += (_, _) => ExportReportsCsv(fromPicker.Value.Date, toPicker.Value.Date);
        closeButton.Click += (_, _) => modal.Close();
        fromPicker.ValueChanged += (_, _) => RefreshPreview();
        toPicker.ValueChanged += (_, _) => RefreshPreview();
        RefreshPreview();

        root.Controls.Add(headerCard, 0, 0);
        root.Controls.Add(metricCard, 0, 1);
        root.Controls.Add(bodyCard, 0, 2);
        modal.Controls.Add(root);
        modal.ShowDialog(this);
    }

    private sealed class ReportSnapshot
    {
        public DateTime PeriodStart { get; init; }
        public DateTime PeriodEnd { get; init; }
        public List<Appointment> Appointments { get; init; } = new();
        public List<Consultation> Consultations { get; init; } = new();
        public List<BillingRecord> BillingRecords { get; init; } = new();
        public List<PrescriptionItem> PrescriptionItems { get; init; } = new();
        public List<Medicine> LowStockItems { get; init; } = new();
        public int AppointmentsCount { get; init; }
        public int CompletedAppointmentsCount { get; init; }
        public int PendingAppointmentsCount { get; init; }
        public int CancelledAppointmentsCount { get; init; }
        public int ConsultationsCount { get; init; }
        public int CompletedConsultationsCount { get; init; }
        public int OpenBillsCount { get; init; }
        public int UnpaidCount { get; init; }
        public int PartiallyPaidCount { get; init; }
        public int PaidCount { get; init; }
        public decimal PaidRevenue { get; init; }
        public int PrescriptionItemCount { get; init; }
        public decimal PrescriptionValue { get; init; }
        public int TrackedMedicineCount { get; init; }
    }

    private ReportSnapshot BuildReportSnapshot(DateTime requestedStart, DateTime requestedEnd)
    {
        var periodStart = requestedStart.Date;
        var periodEnd = requestedEnd.Date;
        if (periodStart > periodEnd)
        {
            (periodStart, periodEnd) = (periodEnd, periodStart);
        }

        var appointments = context.Data.Appointments
            .Where(appointment => appointment.AppointmentDate.Date >= periodStart && appointment.AppointmentDate.Date <= periodEnd)
            .OrderBy(appointment => appointment.AppointmentDate)
            .ThenBy(appointment => appointment.AppointmentTime)
            .ToList();

        var consultations = context.Data.Consultations
            .Where(consultation => consultation.DateOfVisit.Date >= periodStart && consultation.DateOfVisit.Date <= periodEnd)
            .OrderBy(consultation => consultation.DateOfVisit)
            .ThenBy(consultation => consultation.ConsultationId)
            .ToList();

        var consultationIds = consultations
            .Select(consultation => consultation.ConsultationId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var billingRecords = context.Data.BillingRecords
            .Where(record => consultationIds.Contains(record.ConsultationId))
            .OrderBy(record => record.BillingId)
            .ToList();

        var prescriptionItems = context.Data.PrescriptionItems
            .Where(item => consultationIds.Contains(item.ConsultationId))
            .OrderBy(item => item.ConsultationId)
            .ThenBy(item => item.MedicineName)
            .ToList();

        var lowStockItems = context.Data.Medicines
            .Where(medicine => medicine.IsLowStock)
            .OrderBy(medicine => medicine.Quantity)
            .ThenBy(medicine => medicine.MedicineName)
            .ToList();

        return new ReportSnapshot
        {
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Appointments = appointments,
            Consultations = consultations,
            BillingRecords = billingRecords,
            PrescriptionItems = prescriptionItems,
            LowStockItems = lowStockItems,
            AppointmentsCount = appointments.Count,
            CompletedAppointmentsCount = appointments.Count(appointment => appointment.Status == AppointmentStatus.Completed),
            PendingAppointmentsCount = appointments.Count(appointment => appointment.Status == AppointmentStatus.Pending),
            CancelledAppointmentsCount = appointments.Count(appointment => appointment.Status == AppointmentStatus.Cancelled),
            ConsultationsCount = consultations.Count,
            CompletedConsultationsCount = consultations.Count(consultation => consultation.Status == ConsultationStatus.Completed),
            OpenBillsCount = billingRecords.Count(record => record.PaymentStatus != PaymentStatus.Paid),
            UnpaidCount = billingRecords.Count(record => record.PaymentStatus == PaymentStatus.Unpaid),
            PartiallyPaidCount = billingRecords.Count(record => record.PaymentStatus == PaymentStatus.PartiallyPaid),
            PaidCount = billingRecords.Count(record => record.PaymentStatus == PaymentStatus.Paid),
            PaidRevenue = billingRecords.Where(record => record.PaymentStatus == PaymentStatus.Paid).Sum(record => record.TotalAmount),
            PrescriptionItemCount = prescriptionItems.Count,
            PrescriptionValue = prescriptionItems.Sum(item => item.TotalCost),
            TrackedMedicineCount = context.Data.Medicines.Count
        };
    }

    private void ExportReportsCsv(DateTime? requestedStart = null, DateTime? requestedEnd = null)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"clinic-report-{DateTime.Now:yyyyMMdd-HHmm}-summary.csv",
            Title = "Export Reports to CSV (Summary + Details)"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var generatedAt = DateTime.Now;
        var generatedAtIso = generatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        var generatedBy = context.CurrentUser is null
            ? "System"
            : $"{context.CurrentUser.FullName} ({context.CurrentUser.Username})";
        var snapshot = BuildReportSnapshot(requestedStart ?? DateTime.Today, requestedEnd ?? requestedStart ?? DateTime.Today);
        var periodStart = snapshot.PeriodStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var periodEnd = snapshot.PeriodEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        const string reportName = "Clinic Operational Snapshot";
        const string dataSource = "Local clinic database";
        var periodType = snapshot.PeriodStart == snapshot.PeriodEnd ? "daily" : "custom_range";
        const string currencyCode = "PHP";

        var summaryRows = new List<string[]>
        {
            new[]
            {
                "ReportName",
                "GeneratedAt",
                "GeneratedBy",
                "DataSource",
                "PeriodType",
                "PeriodStart",
                "PeriodEnd",
                "Section",
                "MetricKey",
                "MetricLabel",
                "Unit",
                "Value",
                "Currency"
            }
        };

        void AddSummaryMetric(string section, string key, string label, string unit, string value, string currency = "")
        {
            summaryRows.Add(
                new[]
                {
                    reportName,
                    generatedAtIso,
                    generatedBy,
                    dataSource,
                    periodType,
                    periodStart,
                    periodEnd,
                    section,
                    key,
                    label,
                    unit,
                    value,
                    currency
                });
        }

        AddSummaryMetric("consultations", "consultations_in_range", "Consultations In Range", "count", snapshot.ConsultationsCount.ToString(CultureInfo.InvariantCulture));
        AddSummaryMetric("consultations", "consultations_completed", "Consultations Completed", "count", snapshot.CompletedConsultationsCount.ToString(CultureInfo.InvariantCulture));
        AddSummaryMetric("appointments", "appointments_in_range", "Appointments In Range", "count", snapshot.AppointmentsCount.ToString(CultureInfo.InvariantCulture));
        AddSummaryMetric("appointments", "appointments_completed", "Appointments Completed", "count", snapshot.CompletedAppointmentsCount.ToString(CultureInfo.InvariantCulture));
        AddSummaryMetric("appointments", "appointments_pending", "Appointments Pending", "count", snapshot.PendingAppointmentsCount.ToString(CultureInfo.InvariantCulture));
        AddSummaryMetric("appointments", "appointments_cancelled", "Appointments Cancelled", "count", snapshot.CancelledAppointmentsCount.ToString(CultureInfo.InvariantCulture));
        AddSummaryMetric("billing", "billing_open_count", "Open Billing Records", "count", snapshot.OpenBillsCount.ToString(CultureInfo.InvariantCulture));
        AddSummaryMetric("billing", "billing_unpaid_count", "Unpaid Billing Records", "count", snapshot.UnpaidCount.ToString(CultureInfo.InvariantCulture));
        AddSummaryMetric("billing", "billing_partial_count", "Partially Paid Billing Records", "count", snapshot.PartiallyPaidCount.ToString(CultureInfo.InvariantCulture));
        AddSummaryMetric("billing", "billing_paid_count", "Paid Billing Records", "count", snapshot.PaidCount.ToString(CultureInfo.InvariantCulture));
        AddSummaryMetric("billing", "billing_paid_revenue", "Paid Revenue", "currency", snapshot.PaidRevenue.ToString("0.00", CultureInfo.InvariantCulture), currencyCode);
        AddSummaryMetric("prescriptions", "prescriptions_item_count", "Prescription Item Count", "count", snapshot.PrescriptionItemCount.ToString(CultureInfo.InvariantCulture));
        AddSummaryMetric("prescriptions", "prescriptions_dispensed_value", "Prescription Dispensed Value", "currency", snapshot.PrescriptionValue.ToString("0.00", CultureInfo.InvariantCulture), currencyCode);
        AddSummaryMetric("inventory", "inventory_tracked_medicines", "Tracked Medicines", "count", snapshot.TrackedMedicineCount.ToString(CultureInfo.InvariantCulture));
        AddSummaryMetric("inventory", "inventory_low_stock_items", "Low Stock Medicines", "count", snapshot.LowStockItems.Count.ToString(CultureInfo.InvariantCulture));

        var appointmentsRows = new List<string[]>
        {
            new[]
            {
                "ReportName",
                "GeneratedAt",
                "PeriodStart",
                "PeriodEnd",
                "AppointmentId",
                "PatientId",
                "PatientName",
                "DoctorAssigned",
                "AppointmentDate",
                "AppointmentTime",
                "Status"
            }
        };
        appointmentsRows.AddRange(
            snapshot.Appointments
                .Select(
                    appointment => new[]
                    {
                        reportName,
                        generatedAtIso,
                        periodStart,
                        periodEnd,
                        appointment.AppointmentId,
                        appointment.PatientId,
                        appointment.PatientName,
                        appointment.DoctorAssigned,
                        appointment.AppointmentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        appointment.AppointmentTime,
                        appointment.Status.ToString()
                    }));

        var consultationsRows = new List<string[]>
        {
            new[]
            {
                "ReportName",
                "GeneratedAt",
                "ConsultationId",
                "PatientId",
                "PatientName",
                "Doctor",
                "VisitDate",
                "Status",
                "ChiefComplaint",
                "Diagnosis",
                "TreatmentNotes",
                "PrescriptionSummary"
            }
        };
        consultationsRows.AddRange(
            snapshot.Consultations
                .Select(
                    consultation => new[]
                    {
                        reportName,
                        generatedAtIso,
                        consultation.ConsultationId,
                        consultation.PatientId,
                        consultation.PatientName,
                        consultation.Doctor,
                        consultation.DateOfVisit.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        consultation.Status.ToString(),
                        consultation.ChiefComplaint,
                        consultation.Diagnosis,
                        consultation.TreatmentNotes,
                        consultation.PrescribedMedicines
                    }));

        var billingRows = new List<string[]>
        {
            new[]
            {
                "ReportName",
                "GeneratedAt",
                "BillingId",
                "PatientId",
                "PatientName",
                "ConsultationId",
                "ServiceCharges",
                "MedicineCharges",
                "TotalAmount",
                "PaymentStatus",
                "Currency"
            }
        };
        billingRows.AddRange(
            snapshot.BillingRecords
                .Select(
                    record => new[]
                    {
                        reportName,
                        generatedAtIso,
                        record.BillingId,
                        record.PatientId,
                        record.PatientName,
                        record.ConsultationId,
                        record.ServiceCharges.ToString("0.00", CultureInfo.InvariantCulture),
                        record.MedicineCharges.ToString("0.00", CultureInfo.InvariantCulture),
                        record.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture),
                        record.PaymentStatus.ToString(),
                        currencyCode
                    }));

        var prescriptionsRows = new List<string[]>
        {
            new[]
            {
                "ReportName",
                "GeneratedAt",
                "PrescriptionItemId",
                "ConsultationId",
                "PatientId",
                "MedicineId",
                "MedicineName",
                "Dosage",
                "Quantity",
                "UnitPrice",
                "TotalCost",
                "Currency"
            }
        };
        prescriptionsRows.AddRange(
            snapshot.PrescriptionItems
                .Select(
                    item => new[]
                    {
                        reportName,
                        generatedAtIso,
                        item.PrescriptionItemId,
                        item.ConsultationId,
                        item.PatientId,
                        item.MedicineId,
                        item.MedicineName,
                        item.Dosage,
                        item.Quantity.ToString(CultureInfo.InvariantCulture),
                        item.UnitPrice.ToString("0.00", CultureInfo.InvariantCulture),
                        item.TotalCost.ToString("0.00", CultureInfo.InvariantCulture),
                        currencyCode
                    }));

        var lowStockRows = new List<string[]>
        {
            new[]
            {
                "ReportName",
                "GeneratedAt",
                "MedicineId",
                "MedicineName",
                "Category",
                "Quantity",
                "UnitPrice",
                "ExpirationDate",
                "IsLowStock",
                "Currency"
            }
        };
        lowStockRows.AddRange(
            snapshot.LowStockItems.Select(
                medicine => new[]
                {
                    reportName,
                    generatedAtIso,
                    medicine.MedicineId,
                    medicine.MedicineName,
                    medicine.Category,
                    medicine.Quantity.ToString(CultureInfo.InvariantCulture),
                    medicine.UnitPrice.ToString("0.00", CultureInfo.InvariantCulture),
                    medicine.ExpirationDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    medicine.IsLowStock ? "1" : "0",
                    currencyCode
                }));

        var exportDirectory = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
        var exportBaseName = Path.GetFileNameWithoutExtension(dialog.FileName);
        var exportStem = exportBaseName.EndsWith("-summary", StringComparison.OrdinalIgnoreCase)
            ? exportBaseName[..^"-summary".Length]
            : exportBaseName;
        var summaryPath = dialog.FileName;
        var appointmentsPath = Path.Combine(exportDirectory, $"{exportStem}-appointments.csv");
        var consultationsPath = Path.Combine(exportDirectory, $"{exportStem}-consultations.csv");
        var billingPath = Path.Combine(exportDirectory, $"{exportStem}-billing.csv");
        var prescriptionsPath = Path.Combine(exportDirectory, $"{exportStem}-prescriptions.csv");
        var lowStockPath = Path.Combine(exportDirectory, $"{exportStem}-low-stock.csv");

        WriteCsvRows(summaryPath, summaryRows);
        WriteCsvRows(appointmentsPath, appointmentsRows);
        WriteCsvRows(consultationsPath, consultationsRows);
        WriteCsvRows(billingPath, billingRows);
        WriteCsvRows(prescriptionsPath, prescriptionsRows);
        WriteCsvRows(lowStockPath, lowStockRows);

        statusLabel.Text = $"Reports exported for {periodStart} to {periodEnd}: {Path.GetFileName(summaryPath)} (+5 detail files) at {DateTime.Now:t}";
    }

    private static void WriteCsvRows(string path, IEnumerable<string[]> rows)
    {
        var csv = string.Join(Environment.NewLine, rows.Select(row => string.Join(",", row.Select(EscapeCsv))));
        File.WriteAllText(path, csv, new UTF8Encoding(false));
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private void Logout()
    {
        LoggedOut = true;
        context.ClearCurrentUser();
        Close();
    }

    private TabPage BuildCrudTab<T>(
        string title,
        BindingList<T> store,
        bool allowEdit,
        Func<T, string> searchText,
        Func<T, string> getId,
        Action<T, string> setId,
        Func<T> createNew,
        Func<T, bool> editEntity,
        Action<T> afterSave,
        Func<T, bool> canDelete,
        Func<T, string> deleteBlockedReason,
        string idPrefix) where T : class
    {
        var page = new TabPage(title) { BackColor = ClinicTheme.AppBackground };
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12),
            BackColor = ClinicTheme.AppBackground
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var toolbarModel = CreateFilterToolbar($"Search {title.ToLowerInvariant()}...", includeCrudButtons: true, allowEdit: allowEdit);
        var toolbar = toolbarModel.Host;
        var searchBox = toolbarModel.SearchBox;
        var newButton = toolbarModel.NewButton!;
        var editButton = toolbarModel.EditButton!;
        var deleteButton = toolbarModel.DeleteButton!;

        var bindingSource = new BindingSource();
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            DataSource = bindingSource,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false
        };
        ClinicTheme.StyleGrid(grid);

        void ApplyFilter()
        {
            var term = searchBox.Text.Trim();
            bindingSource.DataSource = string.IsNullOrWhiteSpace(term)
                ? store
                : store.Where(item => searchText(item).Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();

            if (grid.Rows.Count > 0)
            {
                grid.ClearSelection();
                grid.Rows[0].Selected = true;
                grid.CurrentCell = grid.Rows[0].Cells[0];
            }
        }

        T? SelectedItem()
        {
            return grid.CurrentRow?.DataBoundItem as T;
        }

        void SaveChanges(string message, T? selected = null)
        {
            PersistAndRefresh(message);
            if (selected is not null)
            {
                SelectItem(bindingSource, grid, searchBox, selected, ApplyFilter);
            }
        }

        refreshBindings.Add(ApplyFilter);
        searchBox.TextChanged += (_, _) => ApplyFilter();
        if (allowEdit)
        {
            grid.CellDoubleClick += (_, _) => EditSelected();
        }

        newButton.Click += (_, _) =>
        {
            var item = createNew();
            if (!editEntity(item))
            {
                return;
            }

            setId(item, IdGenerator.Next(idPrefix, store.Select(getId)));
            store.Add(item);
            afterSave(item);
            SaveChanges($"{title} record saved.", item);
        };

        editButton.Click += (_, _) => EditSelected();
        deleteButton.Click += (_, _) => DeleteSelected();

        void EditSelected()
        {
            if (!allowEdit)
            {
                return;
            }

            var item = SelectedItem();
            if (item is null)
            {
                return;
            }

            if (!editEntity(item))
            {
                return;
            }

            afterSave(item);
            SaveChanges($"{title} record saved.", item);
        }

        void DeleteSelected()
        {
            var item = SelectedItem();
            if (item is null)
            {
                return;
            }

            if (!canDelete(item))
            {
                MessageBox.Show(this, deleteBlockedReason(item), "Delete Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show(this, $"Delete selected record from {title}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            store.Remove(item);
            SaveChanges($"{title} record deleted.");
        }

        ApplyFilter();
        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(grid, 0, 1);
        page.Controls.Add(root);
        return page;
    }

    private static (TableLayoutPanel Host, TextBox SearchBox, Button? NewButton, Button? EditButton, Button? DeleteButton) CreateFilterToolbar(
        string placeholder,
        bool includeCrudButtons,
        bool allowEdit)
    {
        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        host.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var searchShell = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 38,
            Margin = new Padding(0, 7, 0, 7),
            Padding = new Padding(12, 0, 12, 0)
        };
        ClinicTheme.StyleCard(searchShell, ClinicTheme.SurfaceRaised, 16);

        var searchLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = ClinicTheme.SurfaceRaised,
            Margin = new Padding(0)
        };
        searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58));
        searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var searchLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Search",
            Font = ClinicTheme.Caption,
            ForeColor = ClinicTheme.TextSecondary,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var searchBox = new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackColor = ClinicTheme.SurfaceRaised,
            ForeColor = ClinicTheme.TextPrimary,
            Font = ClinicTheme.BodyBold,
            PlaceholderText = placeholder,
            Margin = new Padding(0, 9, 0, 6)
        };
        searchLayout.Controls.Add(searchLabel, 0, 0);
        searchLayout.Controls.Add(searchBox, 1, 0);
        searchShell.Controls.Add(searchLayout);
        host.Controls.Add(searchShell, 0, 0);

        if (!includeCrudButtons)
        {
            return (host, searchBox, null, null, null);
        }

        var actionButtons = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Margin = new Padding(12, 3, 0, 3)
        };

        var newButton = CreateActionButton("New", allowEdit);
        var editButton = CreateActionButton("Edit", allowEdit);
        var deleteButton = CreateActionButton("Delete", allowEdit);
        actionButtons.Controls.Add(newButton);
        actionButtons.Controls.Add(editButton);
        actionButtons.Controls.Add(deleteButton);
        host.Controls.Add(actionButtons, 1, 0);
        return (host, searchBox, newButton, editButton, deleteButton);
    }

    private static Button CreateActionButton(string text, bool enabled)
    {
        var button = new Button
        {
            Text = text,
            Width = 100,
            Height = 38,
            Enabled = enabled,
            Margin = new Padding(8, 0, 0, 0)
        };

        if (text == "Delete")
        {
            ClinicTheme.StyleDangerButton(button);
        }
        else if (text == "Edit")
        {
            ClinicTheme.StyleSecondaryButton(button);
        }
        else
        {
            ClinicTheme.StylePrimaryButton(button);
        }

        if (!enabled)
        {
            button.BackColor = Color.Gainsboro;
            button.ForeColor = Color.DimGray;
            button.Cursor = Cursors.Default;
        }

        return button;
    }

    private void PersistAndRefresh(string message)
    {
        context.Save();
        foreach (var refresh in refreshBindings)
        {
            refresh();
        }

        foreach (var refresh in refreshReports)
        {
            refresh();
        }

        RefreshDashboard();
        statusLabel.Text = $"{message} Last saved {DateTime.Now:t}";
    }

    private void RefreshDashboard()
    {
        dashboardMetrics["Patients"].Text = context.Data.Patients.Count.ToString();
        dashboardMetrics["Appointments Today"].Text = context.Data.Appointments.Count(appointment => appointment.AppointmentDate.Date == DateTime.Today).ToString();
        dashboardMetrics["Consultations"].Text = context.Data.Consultations.Count.ToString();
        dashboardMetrics["Unpaid Bills"].Text = context.Data.BillingRecords.Count(record => record.PaymentStatus != PaymentStatus.Paid).ToString();
        dashboardMetrics["Medicines"].Text = context.Data.Medicines.Count.ToString();
        dashboardMetrics["Low Stock Items"].Text = context.Data.Medicines.Count(medicine => medicine.IsLowStock).ToString();
    }

    private bool HasAnyRole(params UserRole[] roles)
    {
        return context.CurrentUser is not null && roles.Contains(context.CurrentUser.Role);
    }

    private static void SelectItem<T>(BindingSource source, DataGridView grid, TextBox searchBox, T item, Action applyFilter) where T : class
    {
        if (!string.IsNullOrWhiteSpace(searchBox.Text))
        {
            searchBox.Text = string.Empty;
        }

        applyFilter();
        for (var index = 0; index < source.Count; index++)
        {
            if (!ReferenceEquals(source[index], item) || grid.Rows.Count <= index || grid.Rows[index].Cells.Count == 0)
            {
                continue;
            }

            grid.ClearSelection();
            grid.Rows[index].Selected = true;
            grid.CurrentCell = grid.Rows[index].Cells[0];
            break;
        }
    }

    private static Consultation CloneConsultation(Consultation consultation)
    {
        return new Consultation
        {
            ConsultationId = consultation.ConsultationId,
            PatientId = consultation.PatientId,
            PatientName = consultation.PatientName,
            Doctor = consultation.Doctor,
            DateOfVisit = consultation.DateOfVisit,
            Status = consultation.Status,
            ChiefComplaint = consultation.ChiefComplaint,
            Diagnosis = consultation.Diagnosis,
            TreatmentNotes = consultation.TreatmentNotes,
            PrescribedMedicines = consultation.PrescribedMedicines
        };
    }

    private static string BuildPatientInitials(Patient patient)
    {
        var first = string.IsNullOrWhiteSpace(patient.FirstName) ? "-" : patient.FirstName[..1].ToUpperInvariant();
        var last = string.IsNullOrWhiteSpace(patient.LastName) ? "-" : patient.LastName[..1].ToUpperInvariant();
        return $"{first}{last}";
    }

    private static void RestoreConsultation(Consultation target, Consultation source)
    {
        target.PatientId = source.PatientId;
        target.PatientName = source.PatientName;
        target.Doctor = source.Doctor;
        target.DateOfVisit = source.DateOfVisit;
        target.Status = source.Status;
        target.ChiefComplaint = source.ChiefComplaint;
        target.Diagnosis = source.Diagnosis;
        target.TreatmentNotes = source.TreatmentNotes;
        target.PrescribedMedicines = source.PrescribedMedicines;
    }

    private void SyncMedicineReferences(Medicine medicine)
    {
        var affectedConsultationIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in context.Data.PrescriptionItems.Where(item => item.MedicineId == medicine.MedicineId))
        {
            item.MedicineName = medicine.MedicineName;
            affectedConsultationIds.Add(item.ConsultationId);
        }

        foreach (var consultationId in affectedConsultationIds)
        {
            var consultation = context.Data.Consultations.FirstOrDefault(entry => entry.ConsultationId == consultationId);
            if (consultation is null)
            {
                continue;
            }

            consultation.PrescribedMedicines = PrescriptionService.BuildSummary(context.Data.PrescriptionItems.Where(item => item.ConsultationId == consultationId));
        }
    }

    private static void AppendRichLine(RichTextBox box, string text, Font font, Color color, bool newLine = true)
    {
        box.SelectionStart = box.TextLength;
        box.SelectionLength = 0;
        box.SelectionFont = font;
        box.SelectionColor = color;
        box.SelectionBackColor = box.BackColor;
        box.SelectionIndent = 0;
        box.SelectionHangingIndent = 0;
        box.AppendText(text);
        if (newLine)
        {
            box.AppendText(Environment.NewLine);
        }
    }

    private static void AppendRichInlineMetric(RichTextBox box, string label, string value, Color? valueColor = null)
    {
        box.SelectionStart = box.TextLength;
        box.SelectionLength = 0;
        box.SelectionBackColor = box.BackColor;
        box.SelectionIndent = 0;
        box.SelectionHangingIndent = 0;
        box.SelectionFont = ClinicTheme.BodyBold;
        box.SelectionColor = ClinicTheme.TextPrimary;
        box.AppendText(label);
        box.SelectionFont = ClinicTheme.Body;
        box.SelectionColor = valueColor ?? ClinicTheme.TextSecondary;
        box.AppendText(value);
        box.AppendText(Environment.NewLine);
    }

    private static void AppendRichSectionTag(RichTextBox box, string text, Color foreground, Color background)
    {
        box.SelectionStart = box.TextLength;
        box.SelectionLength = 0;
        box.SelectionIndent = 0;
        box.SelectionHangingIndent = 0;
        box.SelectionFont = ClinicTheme.BodyBold;
        box.SelectionColor = foreground;
        box.SelectionBackColor = background;
        box.AppendText($"  {text}  ");
        box.SelectionBackColor = box.BackColor;
        box.AppendText(Environment.NewLine);
    }

    private static void AppendRichBullet(RichTextBox box, string text, Font font, Color color)
    {
        AppendRichLine(box, $"  - {text}", font, color);
    }

    private static void AppendRichDivider(RichTextBox box)
    {
        AppendRichLine(box, "----------------------------------------------", ClinicTheme.Caption, ClinicTheme.Border);
    }

    private static string TextOrFallback(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private void RenderHistoryPlaceholder(RichTextBox historyBox)
    {
        historyBox.SuspendLayout();
        historyBox.Clear();
        AppendRichSectionTag(historyBox, "PATIENT TIMELINE", ClinicTheme.BrandDark, ClinicTheme.SurfaceMuted);
        historyBox.AppendText(Environment.NewLine);
        AppendRichLine(historyBox, "Select a patient to review appointments, consultations, prescriptions, and billing entries.", ClinicTheme.Body, ClinicTheme.TextSecondary);
        historyBox.SelectionStart = 0;
        historyBox.ScrollToCaret();
        historyBox.ResumeLayout();
    }

    private void RenderPatientHistoryViewer(RichTextBox historyBox, Patient patient)
    {
        var appointments = context.Data.Appointments
            .Where(appointment => appointment.PatientId == patient.PatientId)
            .OrderByDescending(appointment => appointment.AppointmentDate)
            .ThenBy(appointment => appointment.AppointmentTime)
            .ToList();
        var consultations = context.Data.Consultations
            .Where(consultation => consultation.PatientId == patient.PatientId)
            .OrderByDescending(consultation => consultation.DateOfVisit)
            .ToList();
        var billingRecords = context.Data.BillingRecords
            .Where(record => record.PatientId == patient.PatientId)
            .OrderByDescending(record => record.BillingId)
            .ToList();
        var allPrescriptionItems = context.Data.PrescriptionItems
            .Where(item => item.PatientId == patient.PatientId)
            .ToList();
        var totalPrescriptionValue = allPrescriptionItems.Sum(item => item.TotalCost);
        var topPrescriptions = allPrescriptionItems
            .GroupBy(item => TextOrFallback(item.MedicineName, "Unnamed medicine"), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Sum(item => item.Quantity))
            .ThenBy(group => group.Key)
            .Take(3)
            .Select(group => $"{group.Key} ({group.Sum(item => item.Quantity)} unit(s))")
            .ToList();

        historyBox.SuspendLayout();
        historyBox.Clear();

        AppendRichSectionTag(historyBox, "PATIENT SNAPSHOT", ClinicTheme.BrandDark, ClinicTheme.SurfaceMuted);
        historyBox.AppendText(Environment.NewLine);
        AppendRichInlineMetric(historyBox, "Patient: ", patient.FullName);
        AppendRichInlineMetric(historyBox, "Patient ID: ", patient.PatientId);
        AppendRichInlineMetric(historyBox, "Birthdate / Age: ", $"{patient.Birthdate:d} / {patient.Age}");
        AppendRichInlineMetric(historyBox, "Sex: ", TextOrFallback(patient.Sex, "-"));
        AppendRichInlineMetric(historyBox, "Contact: ", TextOrFallback(patient.ContactNumber, "No contact number"));
        AppendRichInlineMetric(historyBox, "Address: ", TextOrFallback(patient.Address, "No address recorded"));
        AppendRichInlineMetric(historyBox, "Registered: ", patient.DateRegistered.ToString("MMM d, yyyy"));
        historyBox.AppendText(Environment.NewLine);

        AppendRichSectionTag(historyBox, "PRESCRIPTION SUMMARY", ClinicTheme.Success, ClinicTheme.SuccessSoft);
        historyBox.AppendText(Environment.NewLine);
        AppendRichInlineMetric(historyBox, "Items dispensed: ", allPrescriptionItems.Count.ToString(), ClinicTheme.TextPrimary);
        AppendRichInlineMetric(historyBox, "Dispensed value: ", totalPrescriptionValue.ToString("C"), ClinicTheme.TextPrimary);
        AppendRichInlineMetric(
            historyBox,
            "Most used medicines: ",
            topPrescriptions.Count == 0 ? "No prescriptions recorded" : string.Join(", ", topPrescriptions));
        historyBox.AppendText(Environment.NewLine);

        AppendRichSectionTag(historyBox, $"CARE TIMELINE ({consultations.Count})", ClinicTheme.BrandDark, ClinicTheme.SurfaceMuted);
        historyBox.AppendText(Environment.NewLine);
        if (consultations.Count == 0)
        {
            AppendRichBullet(historyBox, "No consultations recorded.", ClinicTheme.Body, ClinicTheme.TextSecondary);
        }
        else
        {
            foreach (var consultation in consultations.Take(8))
            {
                AppendRichLine(
                    historyBox,
                    $"{consultation.DateOfVisit:MMM d, yyyy}  |  {consultation.ConsultationId}  |  {consultation.Status}",
                    ClinicTheme.BodyBold,
                    ClinicTheme.BrandDark);
                AppendRichInlineMetric(historyBox, "Doctor: ", TextOrFallback(consultation.Doctor, "Unassigned"));
                AppendRichInlineMetric(historyBox, "Chief complaint: ", TextOrFallback(consultation.ChiefComplaint, "Not recorded"));
                AppendRichInlineMetric(historyBox, "Diagnosis: ", TextOrFallback(consultation.Diagnosis, "Not recorded"));
                AppendRichInlineMetric(historyBox, "Treatment: ", TextOrFallback(consultation.TreatmentNotes, "Not recorded"));

                var prescriptionItems = context.Data.PrescriptionItems
                    .Where(item => item.ConsultationId == consultation.ConsultationId)
                    .OrderBy(item => item.MedicineName)
                    .ToList();
                if (prescriptionItems.Count == 0)
                {
                    AppendRichBullet(historyBox, "No medicines prescribed.", ClinicTheme.Body, ClinicTheme.TextSecondary);
                }
                else
                {
                    foreach (var item in prescriptionItems.Take(5))
                    {
                        AppendRichBullet(
                            historyBox,
                            $"{TextOrFallback(item.MedicineName, "Medicine")} | {TextOrFallback(item.Dosage, "No dosage")} | Qty {item.Quantity} | {item.TotalCost:C}",
                            ClinicTheme.Body,
                            ClinicTheme.TextPrimary);
                    }

                    if (prescriptionItems.Count > 5)
                    {
                        AppendRichBullet(historyBox, $"... {prescriptionItems.Count - 5} more prescription item(s)", ClinicTheme.Caption, ClinicTheme.TextSecondary);
                    }
                }

                var linkedBilling = billingRecords.FirstOrDefault(record => record.ConsultationId == consultation.ConsultationId);
                if (linkedBilling is not null)
                {
                    AppendRichInlineMetric(historyBox, "Billing: ", $"{linkedBilling.BillingId} | {linkedBilling.PaymentStatus} | {linkedBilling.TotalAmount:C}");
                }

                AppendRichDivider(historyBox);
            }

            if (consultations.Count > 8)
            {
                AppendRichBullet(historyBox, $"... {consultations.Count - 8} more consultation entries", ClinicTheme.Caption, ClinicTheme.TextSecondary);
            }
        }
        historyBox.AppendText(Environment.NewLine);

        AppendRichSectionTag(historyBox, $"APPOINTMENTS ({appointments.Count})", ClinicTheme.Accent, ClinicTheme.AccentSoft);
        historyBox.AppendText(Environment.NewLine);
        if (appointments.Count == 0)
        {
            AppendRichBullet(historyBox, "No appointments recorded.", ClinicTheme.Body, ClinicTheme.TextSecondary);
        }
        else
        {
            foreach (var appointment in appointments.Take(8))
            {
                var appointmentStatusColor = appointment.Status switch
                {
                    AppointmentStatus.Completed => ClinicTheme.Success,
                    AppointmentStatus.Cancelled => ClinicTheme.Danger,
                    _ => ClinicTheme.Accent
                };

                AppendRichLine(
                    historyBox,
                    $"Appointment {appointment.AppointmentId}",
                    ClinicTheme.BodyBold,
                    ClinicTheme.BrandDark);
                AppendRichInlineMetric(historyBox, "Schedule: ", $"{appointment.AppointmentDate:MMM d, yyyy} at {appointment.AppointmentTime}");
                AppendRichInlineMetric(historyBox, "Doctor: ", TextOrFallback(appointment.DoctorAssigned, "Doctor pending"));
                AppendRichInlineMetric(historyBox, "Status: ", appointment.Status.ToString(), appointmentStatusColor);
                AppendRichDivider(historyBox);
            }

            if (appointments.Count > 8)
            {
                AppendRichBullet(historyBox, $"... {appointments.Count - 8} more appointment entries", ClinicTheme.Caption, ClinicTheme.TextSecondary);
            }
        }
        historyBox.AppendText(Environment.NewLine);
        AppendRichSectionTag(historyBox, $"BILLING ({billingRecords.Count})", ClinicTheme.Danger, ClinicTheme.DangerSoft);
        historyBox.AppendText(Environment.NewLine);
        if (billingRecords.Count == 0)
        {
            AppendRichBullet(historyBox, "No billing records recorded.", ClinicTheme.Body, ClinicTheme.TextSecondary);
        }
        else
        {
            foreach (var billing in billingRecords.Take(8))
            {
                AppendRichBullet(
                    historyBox,
                    $"{billing.BillingId} | Consultation {billing.ConsultationId} | Service {billing.ServiceCharges:C} | Medicine {billing.MedicineCharges:C} | Total {billing.TotalAmount:C} | {billing.PaymentStatus}",
                    ClinicTheme.Body,
                    ClinicTheme.TextPrimary);
            }

            if (billingRecords.Count > 8)
            {
                AppendRichBullet(historyBox, $"... {billingRecords.Count - 8} more billing entries", ClinicTheme.Caption, ClinicTheme.TextSecondary);
            }
        }

        historyBox.SelectionStart = 0;
        historyBox.ScrollToCaret();
        historyBox.ResumeLayout();
    }

    private string BuildConsultationDetailText(Consultation consultation)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Consultation ID: {consultation.ConsultationId}");
        builder.AppendLine($"Patient: {consultation.PatientName} ({consultation.PatientId})");
        builder.AppendLine($"Doctor: {consultation.Doctor}");
        builder.AppendLine($"Visit Date: {consultation.DateOfVisit:d}");
        builder.AppendLine($"Status: {consultation.Status}");
        builder.AppendLine();
        builder.AppendLine($"Chief Complaint: {consultation.ChiefComplaint}");
        builder.AppendLine($"Diagnosis: {consultation.Diagnosis}");
        builder.AppendLine($"Treatment: {consultation.TreatmentNotes}");
        builder.AppendLine();

        var prescriptionItems = context.Data.PrescriptionItems
            .Where(item => item.ConsultationId == consultation.ConsultationId)
            .OrderBy(item => item.MedicineName)
            .ToList();
        builder.AppendLine($"Prescription Items ({prescriptionItems.Count})");
        if (prescriptionItems.Count == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (var item in prescriptionItems)
            {
                builder.AppendLine($"- {item.MedicineName} | {item.Dosage} | Qty {item.Quantity} | {item.TotalCost:C}");
            }
        }

        builder.AppendLine();
        builder.AppendLine($"Prescription Value: {prescriptionService.GetTotalCostForConsultation(consultation.ConsultationId):C}");

        var linkedBilling = context.Data.BillingRecords
            .Where(record => record.ConsultationId == consultation.ConsultationId)
            .OrderBy(record => record.BillingId)
            .ToList();
        builder.AppendLine();
        builder.AppendLine($"Billing Records ({linkedBilling.Count})");
        if (linkedBilling.Count == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (var billing in linkedBilling)
            {
                builder.AppendLine($"- {billing.BillingId} | Service {billing.ServiceCharges:C} | Medicine {billing.MedicineCharges:C} | Total {billing.TotalAmount:C} | {billing.PaymentStatus}");
            }
        }

        return builder.ToString();
    }
}
