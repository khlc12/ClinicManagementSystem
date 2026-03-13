using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace ClinicManagementSystem.Ui;

internal static class ClinicTheme
{
    private static readonly Dictionary<string, Image?> overlayImageCache = new(StringComparer.OrdinalIgnoreCase);

    public static Color AppBackground => Color.FromArgb(243, 238, 231);
    public static Color Surface => Color.FromArgb(255, 252, 247);
    public static Color SurfaceRaised => Color.FromArgb(250, 246, 240);
    public static Color SurfaceMuted => Color.FromArgb(229, 238, 235);
    public static Color Brand => Color.FromArgb(22, 86, 94);
    public static Color BrandDark => Color.FromArgb(12, 53, 60);
    public static Color Accent => Color.FromArgb(212, 143, 82);
    public static Color AccentSoft => Color.FromArgb(247, 231, 214);
    public static Color Success => Color.FromArgb(58, 120, 92);
    public static Color SuccessSoft => Color.FromArgb(224, 239, 231);
    public static Color Danger => Color.FromArgb(154, 77, 72);
    public static Color DangerSoft => Color.FromArgb(245, 227, 224);
    public static Color TextPrimary => Color.FromArgb(29, 36, 42);
    public static Color TextSecondary => Color.FromArgb(96, 106, 114);
    public static Color Border => Color.FromArgb(218, 221, 217);

    public static Font DisplayLarge => new("Bahnschrift SemiBold", 28f, FontStyle.Bold);
    public static Font DisplayMedium => new("Bahnschrift SemiBold", 20f, FontStyle.Bold);
    public static Font Heading => new("Bahnschrift SemiBold", 14f, FontStyle.Bold);
    public static Font Navigation => new("Bahnschrift SemiBold", 10f, FontStyle.Bold);
    public static Font Body => new("Segoe UI", 10f, FontStyle.Regular);
    public static Font BodyBold => new("Segoe UI Semibold", 10f, FontStyle.Bold);
    public static Font Caption => new("Segoe UI", 9f, FontStyle.Regular);
    public static Font Mono => new("Consolas", 10.5f, FontStyle.Regular);

    public static void StyleSurface(Form form)
    {
        form.BackColor = AppBackground;
        form.Font = Body;
        form.ForeColor = TextPrimary;
    }

    public static void StyleCard(Panel panel, Color? backColor = null, int radius = 24)
    {
        panel.BackColor = backColor ?? Surface;
        RoundControl(panel, radius);
        panel.Paint += (_, e) => DrawRoundedBorder(e.Graphics, panel.ClientRectangle, radius, Border);
    }

    public static void StylePrimaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = Brand;
        button.ForeColor = Color.White;
        button.Font = BodyBold;
        button.Cursor = Cursors.Hand;
        RoundControl(button, 14);
        AttachHover(button, BrandDark, Brand);
    }

    public static void StyleSecondaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = SurfaceMuted;
        button.ForeColor = BrandDark;
        button.Font = BodyBold;
        button.Cursor = Cursors.Hand;
        RoundControl(button, 14);
        AttachHover(button, Color.FromArgb(207, 224, 219), SurfaceMuted);
    }

    public static void StyleDangerButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = DangerSoft;
        button.ForeColor = Danger;
        button.Font = BodyBold;
        button.Cursor = Cursors.Hand;
        RoundControl(button, 14);
        AttachHover(button, Color.FromArgb(238, 208, 203), DangerSoft);
    }

    public static void StyleGhostButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = Color.FromArgb(194, 224, 227);
        button.BackColor = Color.FromArgb(69, 117, 123);
        button.ForeColor = Color.White;
        button.Font = BodyBold;
        button.Cursor = Cursors.Hand;
        RoundControl(button, 14);
        var hoverBack = Color.FromArgb(87, 136, 141);
        var normalBack = button.BackColor;
        button.MouseEnter += (_, _) => button.BackColor = hoverBack;
        button.MouseLeave += (_, _) => button.BackColor = normalBack;
    }

    public static void StyleTextBox(TextBox textBox)
    {
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.BackColor = Surface;
        textBox.ForeColor = TextPrimary;
        textBox.Font = Body;
        textBox.Margin = new Padding(0, 0, 0, 12);
    }

    public static void StyleComboBox(ComboBox comboBox)
    {
        comboBox.FlatStyle = FlatStyle.Flat;
        comboBox.BackColor = Surface;
        comboBox.ForeColor = TextPrimary;
        comboBox.Font = Body;
        comboBox.Margin = new Padding(0, 0, 0, 12);
    }

    public static void StyleRichText(RichTextBox box)
    {
        box.BackColor = Surface;
        box.ForeColor = TextPrimary;
        box.BorderStyle = BorderStyle.None;
        box.Font = Mono;
    }

    public static void StyleGrid(DataGridView grid)
    {
        var rowSelectionBackColor = Color.FromArgb(223, 236, 233);

        grid.BackgroundColor = Surface;
        grid.BorderStyle = BorderStyle.None;
        grid.GridColor = Border;
        grid.EnableHeadersVisualStyles = false;
        grid.RowHeadersVisible = false;
        grid.AllowUserToResizeColumns = false;
        grid.AllowUserToResizeRows = false;
        grid.AllowUserToOrderColumns = false;
        grid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersHeight = 42;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        grid.RowTemplate.Height = 34;
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Surface,
            ForeColor = TextPrimary,
            SelectionBackColor = rowSelectionBackColor,
            SelectionForeColor = TextPrimary,
            Padding = new Padding(6, 0, 6, 0),
            Font = Body
        };
        grid.RowsDefaultCellStyle = new DataGridViewCellStyle(grid.DefaultCellStyle)
        {
            Font = Body
        };
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle(grid.RowsDefaultCellStyle)
        {
            BackColor = SurfaceRaised
        };
        grid.RowTemplate.DefaultCellStyle = new DataGridViewCellStyle(grid.RowsDefaultCellStyle);
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = BrandDark,
            ForeColor = Color.White,
            SelectionBackColor = BrandDark,
            SelectionForeColor = Color.White,
            Font = BodyBold,
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(8, 8, 8, 8)
        };

        grid.ColumnAdded += (_, eventArgs) =>
        {
            eventArgs.Column.Resizable = DataGridViewTriState.False;

            var style = eventArgs.Column.DefaultCellStyle;
            if (style.Font is null)
            {
                style.Font = Body;
            }

            if (style.SelectionBackColor == Color.Empty)
            {
                style.SelectionBackColor = rowSelectionBackColor;
            }

            if (style.SelectionForeColor == Color.Empty)
            {
                style.SelectionForeColor = TextPrimary;
            }

            eventArgs.Column.DefaultCellStyle = style;
        };
    }

    public static Label CreatePill(string text, Color backColor, Color foreColor)
    {
        var label = new Label
        {
            AutoSize = true,
            Text = text,
            BackColor = backColor,
            ForeColor = foreColor,
            Font = Caption,
            Padding = new Padding(12, 7, 12, 7),
            Margin = new Padding(0, 0, 10, 10)
        };
        RoundControl(label, 14);
        return label;
    }

    public static void RoundControl(Control control, int radius)
    {
        ApplyRoundedCorners(control, radius);
    }

    public static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return path;
        }

        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void DrawRoundedBorder(Graphics graphics, Rectangle bounds, int radius, Color color)
    {
        if (bounds.Width <= 1 || bounds.Height <= 1)
        {
            return;
        }

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var borderBounds = new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
        using var path = CreateRoundedPath(borderBounds, radius);
        using var pen = new Pen(color);
        graphics.DrawPath(pen, path);
    }

    private static void ApplyRoundedCorners(Control control, int radius)
    {
        void Apply()
        {
            if (control.Width <= 0 || control.Height <= 0)
            {
                return;
            }

            using var path = CreateRoundedPath(new Rectangle(0, 0, control.Width, control.Height), radius);
            control.Region?.Dispose();
            control.Region = new Region(path);
        }

        control.Resize += (_, _) => Apply();
        control.HandleCreated += (_, _) => Apply();
        Apply();
    }

    private static void AttachHover(Button button, Color hoverBackColor, Color defaultBackColor)
    {
        button.MouseEnter += (_, _) => button.BackColor = hoverBackColor;
        button.MouseLeave += (_, _) => button.BackColor = defaultBackColor;
    }

    public static Image? GetOverlayImage(params string[] fileNames)
    {
        foreach (var fileName in fileNames.Where(name => !string.IsNullOrWhiteSpace(name)))
        {
            var needsReload = !overlayImageCache.TryGetValue(fileName, out var image) || image is null;
            if (needsReload)
            {
                var imagePath = Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
                if (!File.Exists(imagePath))
                {
                    var rootFallbackPath = Path.Combine(AppContext.BaseDirectory, fileName);
                    if (File.Exists(rootFallbackPath))
                    {
                        imagePath = rootFallbackPath;
                    }
                    else
                    {
                        overlayImageCache[fileName] = null;
                        continue;
                    }
                }

                try
                {
                    using var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var loadedImage = Image.FromStream(fileStream);
                    image = new Bitmap(loadedImage);
                }
                catch
                {
                    image = null;
                }

                overlayImageCache[fileName] = image;
            }

            if (image is not null)
            {
                return image;
            }
        }

        return null;
    }

    public static Image? GetHeroOverlayImage()
    {
        return GetOverlayImage("hero-overlay.png");
    }

    public static Image? GetBrandLogoImage()
    {
        return GetOverlayImage("app-logo.png", "clinic-logo.png", "logo.png");
    }
}

internal sealed class GradientPanel : Panel
{
    private Color startColor = ClinicTheme.BrandDark;
    private Color endColor = ClinicTheme.Brand;
    private Color shapeColor = Color.FromArgb(28, 255, 255, 255);
    private Image? overlayImage;
    private bool drawDecorativeShapes = true;
    private Color scrimColor = Color.Empty;
    private Bitmap? backgroundCache;
    private Size backgroundCacheSize = Size.Empty;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color StartColor
    {
        get => startColor;
        set
        {
            if (startColor == value)
            {
                return;
            }

            startColor = value;
            InvalidateBackgroundCache();
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color EndColor
    {
        get => endColor;
        set
        {
            if (endColor == value)
            {
                return;
            }

            endColor = value;
            InvalidateBackgroundCache();
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ShapeColor
    {
        get => shapeColor;
        set
        {
            if (shapeColor == value)
            {
                return;
            }

            shapeColor = value;
            InvalidateBackgroundCache();
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Image? OverlayImage
    {
        get => overlayImage;
        set
        {
            if (ReferenceEquals(overlayImage, value))
            {
                return;
            }

            overlayImage = value;
            InvalidateBackgroundCache();
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool DrawDecorativeShapes
    {
        get => drawDecorativeShapes;
        set
        {
            if (drawDecorativeShapes == value)
            {
                return;
            }

            drawDecorativeShapes = value;
            InvalidateBackgroundCache();
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ScrimColor
    {
        get => scrimColor;
        set
        {
            if (scrimColor == value)
            {
                return;
            }

            scrimColor = value;
            InvalidateBackgroundCache();
            Invalidate();
        }
    }

    public GradientPanel()
    {
        DoubleBuffered = true;
        SetStyle(
            ControlStyles.UserPaint
            | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw,
            true);
        UpdateStyles();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            InvalidateBackgroundCache();
        }

        base.Dispose(disposing);
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        if (backgroundCacheSize != ClientSize)
        {
            InvalidateBackgroundCache();
        }
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var cachedBackground = GetBackgroundCache();
        if (cachedBackground is not null)
        {
            e.Graphics.DrawImageUnscaled(cachedBackground, 0, 0);
            return;
        }

        base.OnPaintBackground(e);
    }

    private Bitmap? GetBackgroundCache()
    {
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return null;
        }

        if (backgroundCache is not null && backgroundCacheSize == ClientSize)
        {
            return backgroundCache;
        }

        InvalidateBackgroundCache();
        backgroundCache = new Bitmap(ClientSize.Width, ClientSize.Height);
        backgroundCacheSize = ClientSize;

        using var graphics = Graphics.FromImage(backgroundCache);
        graphics.CompositingMode = CompositingMode.SourceOver;
        graphics.CompositingQuality = CompositingQuality.HighSpeed;
        graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
        graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;

        using (var brush = new LinearGradientBrush(new Rectangle(Point.Empty, ClientSize), StartColor, EndColor, LinearGradientMode.ForwardDiagonal))
        {
            graphics.FillRectangle(brush, new Rectangle(Point.Empty, ClientSize));
        }

        if (overlayImage is not null)
        {
            graphics.DrawImage(overlayImage, new Rectangle(Point.Empty, ClientSize));
        }
        else if (DrawDecorativeShapes)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var shapeBrush = new SolidBrush(ShapeColor);
            graphics.FillEllipse(shapeBrush, Width - 220, -40, 240, 240);
            graphics.FillEllipse(shapeBrush, -70, Height - 120, 180, 180);
            graphics.FillEllipse(shapeBrush, Width - 380, Height - 110, 140, 140);
        }

        DrawScrim(graphics);
        return backgroundCache;
    }

    private void InvalidateBackgroundCache()
    {
        backgroundCache?.Dispose();
        backgroundCache = null;
        backgroundCacheSize = Size.Empty;
    }

    private void DrawScrim(Graphics graphics)
    {
        if (ScrimColor == Color.Empty || ScrimColor.A <= 0 || ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        using var scrimBrush = new SolidBrush(ScrimColor);
        graphics.FillRectangle(scrimBrush, new Rectangle(Point.Empty, ClientSize));
    }
}

internal sealed class NavigationTabControl : TabControl
{
    public NavigationTabControl()
    {
        Appearance = TabAppearance.FlatButtons;
        SizeMode = TabSizeMode.Fixed;
        ItemSize = new Size(1, 1);
        Multiline = true;
        Padding = new Point(0, 0);
        Font = ClinicTheme.Navigation;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    public override Rectangle DisplayRectangle => new(0, 0, Width, Height);

    protected override void WndProc(ref Message m)
    {
        const int TcmAdjustRect = 0x1328;

        if (m.Msg == TcmAdjustRect && !DesignMode)
        {
            m.Result = (IntPtr)1;
            return;
        }

        base.WndProc(ref m);
    }
}
