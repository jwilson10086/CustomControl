using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shell;
using System.Windows.Threading;
using GeneralControl.Effects;
using GeneralControl.Interop;

namespace GeneralControl.Controls;

/// <summary>
/// 毛玻璃质感窗体：无边框、自定义标题栏（拖动/最小化/最大化/关闭/缩放）、
/// 圆角阴影与多种毛玻璃背景模式。
/// </summary>
public class GcWindow : System.Windows.Window
{
    private GcBackdropCapture? _capture;
    private GcGlassyEffect? _glassyEffect;
    private Border? _glassyLayer;
    private Border? _windowFrame;
    private Border? _shadowHost;
    private ContentPresenter? _contentHost;
    private WindowChrome? _chrome;
    private bool _backdropApplied;
    private readonly bool _isSoftwareRender;

    static GcWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcWindow), new FrameworkPropertyMetadata(typeof(GcWindow)));
    }

    public GcWindow()
    {
        // 软件渲染 (WARP) 下：分层窗口 (AllowsTransparency) 中只要存在任何 Effect
        // （DropShadowEffect / 自定义着色器）整个窗口就会不呈现；非分层窗口则一切正常
        // （Effect 被安全忽略）。因此软件渲染时改用非分层 + WindowChrome（圆角/阴影/缩放）。
        _isSoftwareRender = RenderOptions.ProcessRenderMode == RenderMode.SoftwareOnly;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = !_isSoftwareRender;
        ResizeMode = ResizeMode.CanResize;
        Background = Brushes.Transparent;
        SnapsToDevicePixels = true;

        // 某些环境（虚拟显示适配器/远程会话）下隐式样式查找会静默失败导致模板不应用，
        // 显式赋值保证模板一定被应用。
        if (Style is null && TryFindResource("GcWindowStyle") is Style themeStyle)
        {
            Style = themeStyle;
        }

        Loaded += Window_Loaded;
        SizeChanged += Window_SizeChanged;
    }

    #region 依赖属性

    public static readonly DependencyProperty BackdropProperty =
        DependencyProperty.Register(nameof(Backdrop), typeof(GcBackdropMode), typeof(GcWindow),
            new PropertyMetadata(GcBackdropMode.Auto, OnBackdropChanged));

    /// <summary>毛玻璃背景模式。</summary>
    public GcBackdropMode Backdrop
    {
        get => (GcBackdropMode)GetValue(BackdropProperty);
        set => SetValue(BackdropProperty, value);
    }

    public static readonly DependencyProperty CaptionHeightProperty =
        DependencyProperty.Register(nameof(CaptionHeight), typeof(double), typeof(GcWindow),
            new PropertyMetadata(42.0, OnVisualPropertyChanged));

    /// <summary>自定义标题栏高度。</summary>
    public double CaptionHeight
    {
        get => (double)GetValue(CaptionHeightProperty);
        set => SetValue(CaptionHeightProperty, value);
    }

    public static readonly DependencyProperty IsCaptionVisibleProperty =
        DependencyProperty.Register(nameof(IsCaptionVisible), typeof(bool), typeof(GcWindow),
            new PropertyMetadata(true, OnVisualPropertyChanged));

    /// <summary>是否显示自定义标题栏。</summary>
    public bool IsCaptionVisible
    {
        get => (bool)GetValue(IsCaptionVisibleProperty);
        set => SetValue(IsCaptionVisibleProperty, value);
    }

    public static readonly DependencyProperty IsTitleBarButtonsVisibleProperty =
        DependencyProperty.Register(nameof(IsTitleBarButtonsVisible), typeof(bool), typeof(GcWindow),
            new PropertyMetadata(true, OnVisualPropertyChanged));

    /// <summary>是否显示最小化/最大化/关闭按钮。</summary>
    public bool IsTitleBarButtonsVisible
    {
        get => (bool)GetValue(IsTitleBarButtonsVisibleProperty);
        set => SetValue(IsTitleBarButtonsVisibleProperty, value);
    }

    public static readonly DependencyProperty GlassOpacityProperty =
        DependencyProperty.Register(nameof(GlassOpacity), typeof(double), typeof(GcWindow),
            new PropertyMetadata(0.22, OnVisualPropertyChanged));

    /// <summary>模拟玻璃的透明度（0~1，越小越透）。</summary>
    public double GlassOpacity
    {
        get => (double)GetValue(GlassOpacityProperty);
        set => SetValue(GlassOpacityProperty, value);
    }

    public static readonly DependencyProperty GlassTintProperty =
        DependencyProperty.Register(nameof(GlassTint), typeof(Color), typeof(GcWindow),
            new PropertyMetadata(Colors.White, OnVisualPropertyChanged));

    /// <summary>模拟玻璃的着色（影响透过玻璃看到的色彩偏移）。</summary>
    public Color GlassTint
    {
        get => (Color)GetValue(GlassTintProperty);
        set => SetValue(GlassTintProperty, value);
    }

    public static readonly DependencyProperty ShadowOpacityProperty =
        DependencyProperty.Register(nameof(ShadowOpacity), typeof(double), typeof(GcWindow),
            new PropertyMetadata(0.35, OnVisualPropertyChanged));

    /// <summary>窗体投影强度。</summary>
    public double ShadowOpacity
    {
        get => (double)GetValue(ShadowOpacityProperty);
        set => SetValue(ShadowOpacityProperty, value);
    }

    #endregion

    #region 模板与状态

    private static void OnBackdropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GcWindow window)
        {
            window._backdropApplied = false;
            if (window.IsLoaded)
            {
                window.ApplyBackdrop();
            }
        }
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GcWindow { IsLoaded: true } window)
        {
            window.UpdateVisuals();
        }
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _glassyLayer = GetTemplateChild("GlassyLayer") as Border;
        _windowFrame = GetTemplateChild("WindowFrame") as Border;
        _shadowHost = GetTemplateChild("ShadowHost") as Border;
        _contentHost = GetTemplateChild("PART_ContentHost") as ContentPresenter;

        // 软件渲染 (WARP) 下在分层窗口根元素挂 DropShadowEffect 会导致整个窗口不呈现，
        // 移除窗体投影以保证窗口可见（内容不受影响）。
        if (_shadowHost is not null && RenderOptions.ProcessRenderMode == RenderMode.SoftwareOnly)
        {
            _shadowHost.Effect = null;
            _shadowHost.Margin = new Thickness(2);
        }
        if (_isSoftwareRender && _shadowHost is not null)
        {
            // 非分层窗口没有每像素透明：阴影边距区域会呈现黑色，必须去掉。
            _shadowHost.Effect = null;
            _shadowHost.Margin = new Thickness(0);
        }

        if (GetTemplateChild("PART_CaptionBar") is Border captionBar)
        {
            captionBar.MouseLeftButtonDown += OnCaptionMouseDown;
        }

        if (GetTemplateChild("PART_MinButton") is System.Windows.Controls.Button minButton)
        {
            minButton.Click += (_, _) => WindowState = WindowState.Minimized;
        }

        if (GetTemplateChild("PART_MaxButton") is System.Windows.Controls.Button maxButton)
        {
            maxButton.Click += (_, _) => ToggleMaximize();
        }

        if (GetTemplateChild("PART_CloseButton") is System.Windows.Controls.Button closeButton)
        {
            closeButton.Click += (_, _) => Close();
        }

        if (GetTemplateChild("PART_ResizeBorder") is Border resizeBorder)
        {
            AttachResize(resizeBorder);
        }

        UpdateVisuals();
    }

    private void OnCaptionMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }

        if (WindowState == WindowState.Maximized)
        {
            var point = e.GetPosition(this);
            var ratioX = point.X / ActualWidth;
            if (RestoreBounds.Width > 0)
            {
                WindowState = WindowState.Normal;
                Left = point.X - RestoreBounds.Width * ratioX;
                Top = point.Y - 8;
            }
        }

        NativeMethods.DragMove(this);
        e.Handled = true;
    }

    private void ToggleMaximize()
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
        }
        else
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void AttachResize(Border border)
    {
        border.MouseLeftButtonDown += (_, e) =>
        {
            if (WindowState == WindowState.Maximized)
            {
                return;
            }

            var thickness = border.BorderThickness.Left;
            var pos = e.GetPosition(border);
            var direction = GetResizeDirection(pos, border.ActualWidth, border.ActualHeight, thickness);
            if (direction == 0)
            {
                return;
            }

            NativeMethods.BeginResize(this, direction);
            e.Handled = true;
        };

        border.MouseMove += (_, e) =>
        {
            if (WindowState == WindowState.Maximized)
            {
                border.Cursor = Cursors.Arrow;
                return;
            }

            var thickness = border.BorderThickness.Left;
            var pos = e.GetPosition(border);
            border.Cursor = GetResizeCursor(pos, border.ActualWidth, border.ActualHeight, thickness);
        };
    }

    private static int GetResizeDirection(Point mousePos, double width, double height, double thickness)
    {
        var isLeft = mousePos.X < thickness;
        var isRight = mousePos.X > width - thickness;
        var isTop = mousePos.Y < thickness;
        var isBottom = mousePos.Y > height - thickness;

        if (isTop && isLeft) return NativeMethods.WMSZ_TOPLEFT;
        if (isTop && isRight) return NativeMethods.WMSZ_TOPRIGHT;
        if (isBottom && isLeft) return NativeMethods.WMSZ_BOTTOMLEFT;
        if (isBottom && isRight) return NativeMethods.WMSZ_BOTTOMRIGHT;
        if (isLeft) return NativeMethods.WMSZ_LEFT;
        if (isRight) return NativeMethods.WMSZ_RIGHT;
        if (isTop) return NativeMethods.WMSZ_TOP;
        if (isBottom) return NativeMethods.WMSZ_BOTTOM;
        return 0;
    }

    private static Cursor GetResizeCursor(Point mousePos, double width, double height, double thickness)
    {
        var isLeft = mousePos.X < thickness;
        var isRight = mousePos.X > width - thickness;
        var isTop = mousePos.Y < thickness;
        var isBottom = mousePos.Y > height - thickness;

        if ((isTop && isLeft) || (isBottom && isRight)) return Cursors.SizeNWSE;
        if ((isTop && isRight) || (isBottom && isLeft)) return Cursors.SizeNESW;
        if (isLeft || isRight) return Cursors.SizeWE;
        if (isTop || isBottom) return Cursors.SizeNS;
        return Cursors.Arrow;
    }

    #endregion

    #region 背景应用

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        if (_isSoftwareRender)
        {
            // WindowChrome：DWM 缩放边框 + 阴影（仅非分层窗口可用）。
            _chrome = new WindowChrome
            {
                CaptionHeight = 0,
                CornerRadius = new CornerRadius(0),
                GlassFrameThickness = new Thickness(0, 0, 0, 1),
                ResizeBorderThickness = new Thickness(8),
                UseAeroCaptionButtons = false,
            };
            WindowChrome.SetWindowChrome(this, _chrome);
        }

        ApplyBackdrop();
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);

        if (WindowState == WindowState.Maximized)
        {
            var area = SystemParameters.WorkArea;
            MaxWidth = area.Width;
            MaxHeight = area.Height;
            Left = area.Left;
            Top = area.Top;
        }

        if (_shadowHost is not null)
        {
            // 非分层（软件渲染）窗口没有投影边距的空间。
            _shadowHost.Margin = _isSoftwareRender
                ? new Thickness(0)
                : WindowState == WindowState.Maximized
                    ? new Thickness(0)
                    : new Thickness(16);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _capture?.Dispose();
        _capture = null;
        base.OnClosed(e);
    }

    /// <summary>根据 Backdrop 模式应用背景效果（含 OS 能力探测与回退）。</summary>
    private void ApplyBackdrop()
    {
        if (_backdropApplied)
        {
            return;
        }

        var mode = Backdrop;
        if (mode == GcBackdropMode.Auto)
        {
            mode = IsSystemBackdropSupported() ? GcBackdropMode.Acrylic : GcBackdropMode.Simulated;
        }

        _backdropApplied = true;

        switch (mode)
        {
            case GcBackdropMode.Glassy:
                SetupGlassy();
                break;

            case GcBackdropMode.Acrylic:
            case GcBackdropMode.Mica:
                SetupSystemBackdrop(mode == GcBackdropMode.Mica ? SystemBackdropType.Mica : SystemBackdropType.Acrylic);
                break;

            default:
                // 模拟玻璃：模板中的半透明渐变 + 圆角 + 阴影即可。
                _windowFrame?.SetValue(BackgroundProperty, CreateSimulatedGlassBrush());
                break;
        }

        UpdateVisuals();
    }

    private void SetupSystemBackdrop(SystemBackdropType type)
    {
        try
        {
            // 真实系统材质需要非分层窗口（窗口显示后无法修改，失败时回退模拟玻璃）。
            AllowsTransparency = false;
        }
        catch (InvalidOperationException)
        {
            _backdropApplied = true;
            if (_windowFrame is not null)
            {
                _windowFrame.Background = CreateSimulatedGlassBrush();
            }

            return;
        }

        WindowStyle = WindowStyle.None;

        // Windows 11 系统背景材质。
        if (!NativeMethods.SetSystemBackdrop(this, type))
        {
            // 回退到 Windows 10 亚克力。
            _ = NativeMethods.SetAcrylic(this, true, 180, 0xF0, 0xF0, 0xF0);
        }

        // 让 DWM 材质透出：窗体背景透明、内容无阴影边距。
        if (_windowFrame is not null)
        {
            _windowFrame.Background = null;
            _windowFrame.CornerRadius = new CornerRadius(0);
        }

        if (_shadowHost is not null)
        {
            _shadowHost.Effect = null;
            _shadowHost.Margin = new Thickness(0);
        }
    }

    private void SetupGlassy()
    {
        try
        {
            _capture ??= new GcBackdropCapture(this);
            _glassyEffect ??= new GcGlassyEffect();
        }
        catch
        {
            // 着色器加载失败（例如资源缺失）时回退到模拟玻璃。
            _backdropApplied = true;
            if (_windowFrame is not null)
            {
                _windowFrame.Background = CreateSimulatedGlassBrush();
            }

            return;
        }

        if (_windowFrame is not null)
        {
            _windowFrame.Background = _capture.Brush;
        }

        if (_glassyLayer is not null)
        {
            _glassyLayer.Background = _capture.Brush;
            _glassyLayer.Effect = _glassyEffect;
        }

        UpdateGlassyParameters();

        SizeChanged -= OnSizeChangedForGlassy;
        SizeChanged += OnSizeChangedForGlassy;

        Loaded -= OnLoadedForGlassy;
        Loaded += OnLoadedForGlassy;

        Activated -= OnActivatedForGlassy;
        Activated += OnActivatedForGlassy;

        Deactivated -= OnDeactivatedForGlassy;
        Deactivated += OnDeactivatedForGlassy;

        LocationChanged -= OnLocationChangedForGlassy;
        LocationChanged += OnLocationChangedForGlassy;
    }

    private void OnSizeChangedForGlassy(object sender, SizeChangedEventArgs e)
    {
        UpdateGlassyParameters();
        _capture?.UpdateViewport();
    }

    private void OnLocationChangedForGlassy(object? sender, EventArgs e) => _capture?.UpdateViewport();

    private void OnLoadedForGlassy(object sender, RoutedEventArgs e)
    {
        if (GcBackdropCapture.FullScreenSnapshot is null)
        {
            _capture?.CaptureBehindWindow();
        }

        _capture?.UpdateViewport();
        UpdateGlassyParameters();
    }

    private void OnActivatedForGlassy(object? sender, EventArgs e) => _capture?.UpdateViewport();

    private void OnDeactivatedForGlassy(object? sender, EventArgs e) => _capture?.CaptureBehindWindow();

    private void UpdateGlassyParameters()
    {
        if (_glassyEffect is null)
        {
            return;
        }

        var width = Math.Max(1.0, ActualWidth);
        var height = Math.Max(1.0, ActualHeight);

        _glassyEffect.TextureSize = new Point(width, height);
        _glassyEffect.GlassCenter = new Point(width * 0.5, height * 0.5);
        _glassyEffect.GlassSize = new Point(width, height);
        _glassyEffect.BlurIntensity = 0.2f;
    }

    #endregion

    #region 视觉刷新

    private void UpdateVisuals()
    {
        if (_shadowHost is not null && _shadowHost.Effect is DropShadowEffect shadow)
        {
            shadow.Opacity = ShadowOpacity;
        }
    }

    /// <summary>构建模拟玻璃背景画刷：半透明着色 + 顶部高光渐变 + 边缘微光。</summary>
    private Brush CreateSimulatedGlassBrush()
    {
        var tint = GlassTint;
        var color = Color.FromArgb((byte)(255 * GlassOpacity), tint.R, tint.G, tint.B);

        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
        };
        brush.GradientStops.Add(new GradientStop(color, 0.0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(color.A * 0.75), tint.R, tint.G, tint.B), 0.45));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(color.A * 0.55), tint.R, tint.G, tint.B), 1.0));
        brush.Freeze();
        return brush;
    }

    #endregion

    private static bool IsSystemBackdropSupported()
    {
        var os = Environment.OSVersion.Version;
        return os.Major > 10 || (os.Major == 10 && os.Build >= 22000);
    }


    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            PrintLayoutInfo();
        }, DispatcherPriority.Render);
    }


    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        PrintLayoutInfo();
    }


    private void PrintLayoutInfo()
    {
        Debug.WriteLine("========== GcWindow Layout ==========");

        Debug.WriteLine(
            $"GcWindow Actual : {ActualWidth} x {ActualHeight}");

        Debug.WriteLine(
            $"GcWindow Pos : {Left},{Top}");


        if (_shadowHost != null)
        {
            Debug.WriteLine(
                $"ShadowHost Actual : {_shadowHost.ActualWidth} x {_shadowHost.ActualHeight}");

            Debug.WriteLine(
                $"ShadowHost Margin : {_shadowHost.Margin}");
        }


        if (_windowFrame != null)
        {
            Debug.WriteLine(
                $"WindowFrame Actual : {_windowFrame.ActualWidth} x {_windowFrame.ActualHeight}");

            Debug.WriteLine(
                $"WindowFrame Margin : {_windowFrame.Margin}");
        }


        var presenter = _contentHost;


        if (presenter != null)
        {
            Debug.WriteLine(
                $"ContentPresenter Actual : {presenter.ActualWidth} x {presenter.ActualHeight}");

            Debug.WriteLine(
                $"ContentPresenter Alignment : {presenter.HorizontalAlignment} / {presenter.VerticalAlignment}");

            Debug.WriteLine(
                $"ContentPresenter Margin : {presenter.Margin}");
        }


        Debug.WriteLine(
            $"GcWindow Content Align : {HorizontalContentAlignment} / {VerticalContentAlignment}");

        DumpVisualTree(this, 0);

        Debug.WriteLine("======================================");
    }

    private static void DumpVisualTree(DependencyObject node, int depth)
    {
        if (node is not Visual || depth > 14)
        {
            return;
        }

        var indent = new string(' ', depth * 2);
        string info = node.GetType().Name;

        if (node is FrameworkElement fe)
        {
            if (!string.IsNullOrEmpty(fe.Name))
            {
                info += $" #{fe.Name}";
            }

            try
            {
                var slot = System.Windows.Controls.Primitives.LayoutInformation.GetLayoutSlot(fe);
                info += $" slot={slot.X:F2},{slot.Y:F2} {slot.Width:F2}x{slot.Height:F2}";
            }
            catch
            {
                // 尚未完成布局时忽略
            }

            info += $" actual={fe.ActualWidth:F2}x{fe.ActualHeight:F2}";
            info += $" margin={fe.Margin}";
            info += $" align={fe.HorizontalAlignment}/{fe.VerticalAlignment}";
        }

        Debug.WriteLine($"{indent}{info}");

        int count = VisualTreeHelper.GetChildrenCount(node);
        for (int i = 0; i < count; i++)
        {
            DumpVisualTree(VisualTreeHelper.GetChild(node, i), depth + 1);
        }
    }



    private static T? FindVisualChild<T>(DependencyObject obj)
        where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var child = VisualTreeHelper.GetChild(obj, i);

            if (child is T result)
                return result;

            var sub = FindVisualChild<T>(child);

            if (sub != null)
                return sub;
        }

        return null;
    }
}











