using ClinicManagementSystem.Services;
using ClinicManagementSystem.Ui;

namespace ClinicManagementSystem;

public class LoginForm : Form
{
    private readonly ClinicContext context;
    private readonly AuthenticationService authenticationService;
    private readonly TextBox usernameTextBox = new() { PlaceholderText = "Enter username", Width = 280 };
    private readonly TextBox passwordTextBox = new() { PlaceholderText = "Enter password", Width = 280, UseSystemPasswordChar = true };
    private readonly CheckBox showPasswordCheckBox = new()
    {
        AutoSize = true,
        Text = "Show password",
        ForeColor = ClinicTheme.TextSecondary,
        Font = ClinicTheme.Caption,
        Margin = new Padding(0, 6, 0, 0)
    };
    private readonly Label messageLabel = new() { AutoSize = true, ForeColor = ClinicTheme.Danger, MaximumSize = new Size(320, 0) };

    public LoginForm(ClinicContext context, AuthenticationService authenticationService)
    {
        this.context = context;
        this.authenticationService = authenticationService;

        BuildLayout();
    }

    private void BuildLayout()
    {
        Text = "Clinic Management System - Login";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(980, 620);
        ClinicTheme.StyleSurface(this);

        ClinicTheme.StyleTextBox(usernameTextBox);
        ClinicTheme.StyleTextBox(passwordTextBox);
        usernameTextBox.Font = ClinicTheme.BodyBold;
        passwordTextBox.Font = ClinicTheme.BodyBold;
        usernameTextBox.BorderStyle = BorderStyle.None;
        passwordTextBox.BorderStyle = BorderStyle.None;
        usernameTextBox.BackColor = ClinicTheme.SurfaceRaised;
        passwordTextBox.BackColor = ClinicTheme.SurfaceRaised;
        usernameTextBox.Dock = DockStyle.Fill;
        passwordTextBox.Dock = DockStyle.Fill;
        usernameTextBox.Margin = new Padding(0);
        passwordTextBox.Margin = new Padding(0);
        showPasswordCheckBox.CheckedChanged += (_, _) => passwordTextBox.UseSystemPasswordChar = !showPasswordCheckBox.Checked;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = ClinicTheme.AppBackground,
            Padding = new Padding(18)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));

        var brandPanel = new GradientPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(44, 44, 44, 44),
            StartColor = ClinicTheme.BrandDark,
            EndColor = ClinicTheme.Brand,
            ShapeColor = Color.FromArgb(26, 255, 255, 255),
            OverlayImage = ClinicTheme.GetOverlayImage("login-hero-overlay.png", "hero-overlay.png"),
            DrawDecorativeShapes = false,
            ScrimColor = Color.FromArgb(108, 8, 40, 47)
        };
        ClinicTheme.RoundControl(brandPanel, 28);

        var brandTitle = new Label
        {
            AutoSize = true,
            Text = "Clinic operations, finally readable.",
            Font = ClinicTheme.DisplayLarge,
            ForeColor = Color.White,
            MaximumSize = new Size(420, 0)
        };

        var brandSubtitle = new Label
        {
            AutoSize = true,
            Text = "An offline desktop workspace for patient records, appointments, consultations, billing, and medicine stock in one place.",
            Font = ClinicTheme.Body,
            ForeColor = Color.FromArgb(244, 250, 251),
            MaximumSize = new Size(440, 0)
        };

        var featureRow = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 0)
        };
        featureRow.Controls.Add(ClinicTheme.CreatePill("Offline-ready", ClinicTheme.AccentSoft, ClinicTheme.BrandDark));
        featureRow.Controls.Add(ClinicTheme.CreatePill("Role-based access", Color.FromArgb(55, 111, 118), Color.White));
        featureRow.Controls.Add(ClinicTheme.CreatePill("Local storage", Color.FromArgb(55, 111, 118), Color.White));

        var metricPatients = CreateBrandMetric("Patients", "Registration, history, and linked visits");
        var metricConsultations = CreateBrandMetric("Consultations", "Diagnosis, treatments, prescriptions, and billing");
        var metricInventory = CreateBrandMetric("Inventory", "Medicine stock that moves with prescriptions");

        var brandFooter = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            ColumnCount = 3,
            RowCount = 4,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0, 8, 0, 0)
        };
        brandFooter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        brandFooter.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        brandFooter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        brandFooter.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        brandFooter.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        brandFooter.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        brandFooter.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        brandFooter.Controls.Add(metricPatients, 1, 1);
        brandFooter.Controls.Add(metricConsultations, 1, 2);
        brandFooter.Controls.Add(metricInventory, 1, 3);

        var brandLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent
        };
        brandLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        brandLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        brandLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        brandLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        brandLayout.Controls.Add(brandTitle, 0, 0);
        brandLayout.Controls.Add(brandSubtitle, 0, 1);
        brandLayout.Controls.Add(featureRow, 0, 2);
        brandLayout.Controls.Add(brandFooter, 0, 3);
        brandPanel.Controls.Add(brandLayout);

        var cardHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(26, 24, 12, 24),
            BackColor = ClinicTheme.AppBackground
        };

        var loginCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(34),
            BackColor = ClinicTheme.Surface
        };
        ClinicTheme.StyleCard(loginCard, ClinicTheme.Surface, 28);

        var loginButton = new Button
        {
            Text = "Enter Workspace",
            Width = 280,
            Height = 42,
            Margin = new Padding(0, 4, 0, 0)
        };
        ClinicTheme.StylePrimaryButton(loginButton);
        loginButton.Click += (_, _) => AttemptLogin();
        AcceptButton = loginButton;

        var loginLabel = new Label
        {
            AutoSize = true,
            Text = "Sign in",
            Font = ClinicTheme.DisplayMedium,
            ForeColor = ClinicTheme.TextPrimary
        };

        var subtitleLabel = new Label
        {
            AutoSize = true,
            Text = "Sign in with your assigned clinic account to enter the workspace.",
            Font = ClinicTheme.Body,
            ForeColor = ClinicTheme.TextSecondary,
            MaximumSize = new Size(320, 0)
        };

        var usernameLabel = new Label
        {
            AutoSize = true,
            Text = "Username",
            Font = ClinicTheme.BodyBold,
            ForeColor = ClinicTheme.TextPrimary,
            Margin = new Padding(0, 8, 0, 2)
        };
        var passwordLabel = new Label
        {
            AutoSize = true,
            Text = "Password",
            Font = ClinicTheme.BodyBold,
            ForeColor = ClinicTheme.TextPrimary,
            Margin = new Padding(0, 8, 0, 2)
        };
        var helperLabel = new Label
        {
            AutoSize = true,
            Text = "Tip: Press Enter after typing your password.",
            Font = ClinicTheme.Caption,
            ForeColor = ClinicTheme.TextSecondary,
            Margin = new Padding(0, 8, 0, 0)
        };

        var usernameFieldHost = CreateInputFieldHost(usernameTextBox);
        var passwordFieldHost = CreateInputFieldHost(passwordTextBox);

        var formLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 12,
            BackColor = ClinicTheme.Surface
        };
        formLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        formLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));
        formLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        formLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        formLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        formLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        formLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        formLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        formLayout.Controls.Add(loginLabel, 0, 0);
        formLayout.Controls.Add(subtitleLabel, 0, 1);
        formLayout.Controls.Add(usernameLabel, 0, 3);
        formLayout.Controls.Add(usernameFieldHost, 0, 4);
        formLayout.Controls.Add(passwordLabel, 0, 5);
        formLayout.Controls.Add(passwordFieldHost, 0, 6);
        formLayout.Controls.Add(showPasswordCheckBox, 0, 7);
        formLayout.Controls.Add(loginButton, 0, 8);
        formLayout.Controls.Add(messageLabel, 0, 9);
        formLayout.Controls.Add(helperLabel, 0, 10);

        loginCard.Controls.Add(formLayout);
        cardHost.Controls.Add(loginCard);

        root.Controls.Add(brandPanel, 0, 0);
        root.Controls.Add(cardHost, 1, 0);
        Controls.Add(root);
    }

    private static Panel CreateInputFieldHost(TextBox textBox)
    {
        var host = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 40,
            Padding = new Padding(12, 9, 12, 8),
            Margin = new Padding(0, 0, 0, 2),
            BackColor = ClinicTheme.SurfaceRaised
        };
        ClinicTheme.StyleCard(host, ClinicTheme.SurfaceRaised, 12);
        host.Controls.Add(textBox);
        return host;
    }

    private static Panel CreateBrandMetric(string title, string description)
    {
        var panel = new Panel
        {
            Width = 400,
            Height = 78,
            BackColor = Color.FromArgb(46, 101, 108),
            Margin = new Padding(0, 0, 0, 14)
        };
        ClinicTheme.RoundControl(panel, 18);

        var titleLabel = new Label
        {
            AutoSize = true,
            Text = title,
            Font = ClinicTheme.BodyBold,
            ForeColor = Color.White,
            Margin = new Padding(0, 0, 0, 2)
        };
        var descriptionLabel = new Label
        {
            AutoSize = true,
            Text = description,
            Font = ClinicTheme.Caption,
            ForeColor = Color.FromArgb(225, 238, 240),
            MaximumSize = new Size(350, 0),
            Margin = new Padding(0)
        };

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Padding = new Padding(18, 12, 18, 12),
            Margin = new Padding(0)
        };
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.Controls.Add(titleLabel, 0, 0);
        content.Controls.Add(descriptionLabel, 0, 1);
        panel.Controls.Add(content);
        return panel;
    }

    private void AttemptLogin()
    {
        var username = usernameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            messageLabel.Text = "Enter your username.";
            return;
        }

        if (string.IsNullOrWhiteSpace(passwordTextBox.Text))
        {
            messageLabel.Text = "Enter your password.";
            return;
        }

        var existingUser = context.Data.Users.FirstOrDefault(
            user => string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase));
        if (existingUser is not null && !existingUser.IsActive)
        {
            messageLabel.Text = "This account is inactive. Ask an administrator to reactivate it.";
            return;
        }

        var user = authenticationService.Authenticate(username, passwordTextBox.Text);
        if (user is null)
        {
            messageLabel.Text = "Invalid username or password.";
            return;
        }

        messageLabel.Text = string.Empty;
        context.SetCurrentUser(user);
        DialogResult = DialogResult.OK;
        Close();
    }
}
