using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GeneralControl.Controls;

/// <summary>
/// 水平电镀腔控件（半导体水平电镀 cell 正视图横截，参考 LAM ECD 电镀腔）。
///
/// 【结构（自上而下）】
/// ① 可升降腔头（Chamber Head）：顶部左右各一气缸（气缸开合），
///    腔头顶盖 + 可旋转晶圆卡盘（IsRotating 驱动，模拟晶圆旋转）。
/// ② 蓝色透明回收罩（Recovery Hood）。
/// ③ 阳极塔（Anode Tower）：竖直塔体 + 高位/低位两位感应器（随液位自动点亮）。
/// ④ 阳极腔（Anode Chamber）：底部镀液填充（Level 0~100）。
///
/// 【状态模型】
/// - HeadOpen=true ：两侧夹爪张开（释放晶圆）；
/// - HeadLift 0~100：腔头升降行程（0=合严 / 100=完全升起）；
/// - IsRotating=true：晶圆卡盘持续旋转动画；
/// - Level  0~100   ：阳极腔液位；
/// - 高/低液位感应器：随液位到达阈值自动点亮（高≈Level≥72、低≈Level≥18）。
///
/// 【代码后置说明】
/// 升降行程与液位高度为区间映射，纯 XAML 无法完成乘法，
/// 由代码在 HeadLift / Level 变化与尺寸变化时同步
/// PART_Head（升降平移）、PART_Liquid（液位高度）与感应器点亮（PART_High/LowLevel）。
/// </summary>

[TemplatePart(Name = PartHead, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartOpenGroup, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartLiquid, Type = typeof(Border))]
    [TemplatePart(Name = PartHighLevel, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartLowLevel, Type = typeof(FrameworkElement))]
    public class GcPlatingChamber : Control
    {
   
    private const string PartHead = "PART_Head";
    private const string PartOpenGroup = "PART_OpenGroup";
    private const string PartLiquid = "PART_Liquid";
    private const string PartHighLevel = "PART_HighLevel";
    private const string PartLowLevel = "PART_LowLevel";

    static GcPlatingChamber()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcPlatingChamber), new FrameworkPropertyMetadata(typeof(GcPlatingChamber)));
    }

    public GcPlatingChamber()
    {
        // 控件尺寸变化（含首次布局）时重算升降与液位。
        // 在构造函数挂接保证只订阅一次（OnApplyTemplate 可能被多次调用）。
        SizeChanged += (_, _) => UpdateGeometry();
        _lidTimer.Tick += LidTick;
    }

    // ==================== 依赖属性 ====================

    public static readonly DependencyProperty HeadOpenProperty =
        DependencyProperty.Register(nameof(HeadOpen), typeof(bool), typeof(GcPlatingChamber),
            new PropertyMetadata(false, OnStateChanged));

    public static readonly DependencyProperty HeadLiftProperty =
        DependencyProperty.Register(nameof(HeadLift), typeof(double), typeof(GcPlatingChamber),
            new PropertyMetadata(0.0, OnStateChanged));

    public static readonly DependencyProperty TiltProperty =
        DependencyProperty.Register(nameof(Tilt), typeof(double), typeof(GcPlatingChamber),
            new PropertyMetadata(0.0, OnStateChanged));

    public static readonly DependencyProperty IsRotatingProperty =
        DependencyProperty.Register(nameof(IsRotating), typeof(bool), typeof(GcPlatingChamber),
            new PropertyMetadata(false, OnStateChanged));

    public static readonly DependencyProperty HasWaferProperty =
        DependencyProperty.Register(nameof(HasWafer), typeof(bool), typeof(GcPlatingChamber),
            new PropertyMetadata(false, OnStateChanged));

    public static readonly DependencyProperty LevelProperty =
        DependencyProperty.Register(nameof(Level), typeof(double), typeof(GcPlatingChamber),
            new PropertyMetadata(40.0, OnStateChanged));

    public static readonly DependencyProperty LiquidBrushProperty =
        DependencyProperty.Register(nameof(LiquidBrush), typeof(Brush), typeof(GcPlatingChamber),
            new PropertyMetadata(default(Brush)));

    /// <summary>腔头是否打开（开盖=盖板上移 80 / 关盖=盖板下移 110，代码后置动画）。</summary>
    public bool HeadOpen
    {
        get => (bool)GetValue(HeadOpenProperty);
        set => SetValue(HeadOpenProperty, value);
    }

    /// <summary>腔头升降行程百分比 0~100（0=合严 / 100=完全升起）。</summary>
    public double HeadLift
    {
        get => (double)GetValue(HeadLiftProperty);
        set => SetValue(HeadLiftProperty, value);
    }

    /// <summary>腔头倾斜角度 0~45（驱动模块化 RenderTransform 中的旋转）。</summary>
    public double Tilt
    {
        get => (double)GetValue(TiltProperty);
        set => SetValue(TiltProperty, value);
    }

    /// <summary>是否旋转：正视视角下腔头下部整体绕垂直轴(Y)伪 3D 旋转（角度 0~360 循环）。</summary>
    public bool IsRotating
    {
        get => (bool)GetValue(IsRotatingProperty);
        set => SetValue(IsRotatingProperty, value);
    }

    /// <summary>是否有晶圆：true=盖板盖下处显示晶圆（槽口上方搁置的硅片）/ false=隐藏。</summary>
    public bool HasWafer
    {
        get => (bool)GetValue(HasWaferProperty);
        set => SetValue(HasWaferProperty, value);
    }

    /// <summary>阳极腔液位 0~100。</summary>
    public double Level
    {
        get => (double)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    /// <summary>液体填充画刷。默认 null 时使用模板内置的镀液渐变（蓝色半透明）。</summary>
    public Brush? LiquidBrush
    {
        get => (Brush?)GetValue(LiquidBrushProperty);
        set => SetValue(LiquidBrushProperty, value);
    }

    // ==================== 模板部件 ====================

    private FrameworkElement? _head;
    private FrameworkElement? _openGroup;
    private Border? _liquid;
    private FrameworkElement? _highLevel;
    private FrameworkElement? _lowLevel;

    /// <summary>升降基准：模板 Viewbox 内腔头静止（合严沉入槽内）时的下移量（逻辑坐标）。</summary>
    private const double HeadTravelStart = 72;
    /// <summary>升降行程长度（逻辑坐标，HeadLift=100 时腔头在此基础上抬高该值）。</summary>
    private const double HeadTravelLength = 50;

    private TranslateTransform? _headTransform;

    // 开盖/关盖盖板滑动动画：用 DispatcherTimer 插值，不依赖合成时钟，
    // 在窗口内与离屏渲染下行为一致（0.4s smoothstep，80⇄110）。
    private const double LidOpenTop = 80;
    private const double LidCloseTop = 110;
    private const int LidDurationMs = 400;
    private readonly DispatcherTimer _lidTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private bool? _lidTarget;
    private double _lidFrom;
    private double _lidTo;
    private double _lidElapsedMs;

    // 正视 Y 轴旋转骗术：仅【连接柱×2】发生位置变化——始终保持 10px 宽度，只在水平方向
    // 对称内收/外放（面向视者 |cos|=1 时回到外侧原位，夹角 |cos|→0 时同步收向腔头中心线），
    // 幅度有界、永不越界/镜像乱飞；其余部件(宽压板/工艺条盖板/顶压板)位置、大小一律不动，
    // 仅渐变明暗呼吸模拟受光面转过（0°亮 → 90°/270°暗 → 180°回亮）。
    private const string PartSpinPost1 = "PART_SpinPost1";
    private const string PartSpinPost2 = "PART_SpinPost2";
    private const string PartSpinBoard = "PART_SpinBoard";
    private const string PartSpinBase = "PART_SpinBase";
    /// <summary>连接柱内收最大位移（左柱 +X 右柱 -X，逻辑坐标 36→94 / 150→92，
    /// 完全收拢时两柱在中心附近重叠约 8px ≈ 正对夹角时的柱体叠剪影）。</summary>
    private const double PostTravel = 58;
    private readonly System.Collections.Generic.List<(TranslateTransform T, int Sign)> _spinPostSlides = new();
    private readonly System.Collections.Generic.List<LitPart> _litParts = new();
    /// <summary>旋转速度（°/s）。亦作为高光带的匀速率：一整圈左→右扫一遍游标。</summary>
    private const double SpinDegreesPerSecond = 125.0;
    private long _spinStartTicks;
    /// <summary>仅在一次渲染循环中推进的相位（0~360°）。连续时间驱动 + 每帧一更，
    /// 取代旧版离散 16ms 定时器（其落帧会偶发 1×/2× 步进不均 → 观感顿挫）。</summary>
    private double _spinAngle;
    private EventHandler? _renderHandler;

    /// <summary>受光照部件：渐变刷已被接管为可变的“扫光”刷（水平基底 + 移动高光带），
    /// 并保存原始竖向渐变快照以便停转复位。L=顶亮 D=底暗 Mid=中调 Light=高光色。</summary>
    private readonly record struct LitPart(
        LinearGradientBrush Brush, Color L, Color D, Color Mid, Color Light,
        GradientStopCollection Snapshot);

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        
        _head = GetTemplateChild(PartHead) as FrameworkElement;
        _openGroup = GetTemplateChild(PartOpenGroup) as FrameworkElement;
        _liquid = GetTemplateChild(PartLiquid) as Border;
        _highLevel = GetTemplateChild(PartHighLevel) as FrameworkElement;
        _lowLevel = GetTemplateChild(PartLowLevel) as FrameworkElement;

        _spinPostSlides.Clear();
        foreach (var name in new[] { PartSpinPost1, PartSpinPost2 })
        {
            // 模板内联的变换应用模板后是只读(frozen)的：改成代码创建的未冻结平移变换接管，
            // 后续 SetPostShift 才能就地改 X。Sign=+1 左柱(内收 = +X)、-1 右柱(内收 = -X)。
            if (GetTemplateChild(name) is FrameworkElement fe)
            {
                var fresh = new TranslateTransform();
                fe.RenderTransform = fresh;
                _spinPostSlides.Add((fresh, _spinPostSlides.Count == 0 ? 1 : -1));
            }
        }

        // 光照部件：接管为可变“扫光”刷 —— 水平基底渐变(左亮→右暗)之上一条移动高光带，
        // 旋转期间几何不动，只让高光带随角度从左往右扫过（光追移动感）。
        _litParts.Clear();
        GradientStopCollection SnapshotOf(LinearGradientBrush b)
        {
            var s = new GradientStopCollection();
            foreach (var gs in b.GradientStops)
            {
                s.Add(new GradientStop(gs.Color, gs.Offset));
            }
            return s;
        }
        foreach (var name in new[] { PartSpinBoard, PartSpinBase })
        {
            if (GetTemplateChild(name) is Shape sh && sh.Fill is LinearGradientBrush b)
            {
                var l = b.GradientStops[0].Color;
                var d = b.GradientStops[^1].Color;
                var hot = new LinearGradientBrush { StartPoint = new Point(0, 0.5), EndPoint = new Point(1, 0.5) };
                hot.GradientStops.Add(new GradientStop(l, 0));
                hot.GradientStops.Add(new GradientStop(d, 1));
                sh.Fill = hot;
                _litParts.Add(new LitPart(hot, l, d, Mix(l, d, 0.5), Colors.White, SnapshotOf(b)));
            }
        }
        if (_openGroup is Shape og && og.Fill is LinearGradientBrush lid)
        {
            var l = lid.GradientStops[0].Color;
            var d = lid.GradientStops[^1].Color;
            var hot = new LinearGradientBrush { StartPoint = new Point(0, 0.5), EndPoint = new Point(1, 0.5) };
            hot.GradientStops.Add(new GradientStop(l, 0));
            hot.GradientStops.Add(new GradientStop(d, 1));
            og.Fill = hot;
            _litParts.Add(new LitPart(hot, l, d, Mix(l, d, 0.5), Color.FromRgb(0xFF, 0xEC, 0xB0), SnapshotOf(lid)));
        }

        UpdateGeometry();
        ApplySpinState();
    }

    private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var chamber = (GcPlatingChamber)d;
        chamber.UpdateGeometry();
        if (e.Property == IsRotatingProperty)
        {
            chamber.ApplySpinState();
        }
    }

    /// <summary>IsRotating ⇨ 挂接/摘除渲染循环：每帧精确更新一次，相位由墙钟连续推进
    /// （帧间恒速），连接柱与光带同相位联动画——无定时器 16ms 步进导致的顿挫。</summary>
    private void ApplySpinState()
    {
        if (IsRotating)
        {
            if (_renderHandler != null)
            {
                return;
            }
            _spinStartTicks = System.Diagnostics.Stopwatch.GetTimestamp();
            _renderHandler = (_, _) => AdvanceSpin();
            System.Windows.Media.CompositionTarget.Rendering += _renderHandler;
        }
        else
        {
            if (_renderHandler != null)
            {
                System.Windows.Media.CompositionTarget.Rendering -= _renderHandler;
                _renderHandler = null;
            }
            ResetSpin();
        }
    }

    /// <summary>按“内收量”（0=面向视者原位，1=完全收向中心）设置连接柱水平位移。</summary>
    private void SetPostShift(double inward)
    {
        var dx = inward * PostTravel;
        foreach (var (t, sign) in _spinPostSlides)
        {
            t.X = sign * dx;
        }
    }

    /// <summary>光带平滑运动廓线（一段循环 u=0..1）：左端驻留淡入 → 缓起 → 匀巡 →
    /// 缓停 → 右端驻留淡出 → 静默回绕。两端都慢下来且过渡全部经强度渐变衔接，
    /// 杜绝了旧版右→左“瞬间瞬移”的卡顿跳变。</summary>
    private static void SweepMotion(double u, out double pos, out double amp)
    {
        const double dIn = 0.06, eIn = 0.16, crs = 0.46, eOut = 0.16, dOut = 0.10;
        var mid = dIn + eIn + crs + eOut;
        if (u < dIn)
        {
            pos = 0.02;
            amp = u / dIn;
        }
        else if (u < dIn + eIn)
        {
            var k = (u - dIn) / eIn;
            pos = 0.02 + 0.16 * (k * k * (3 - 2 * k));
            amp = 1;
        }
        else if (u < dIn + eIn + crs)
        {
            var k = (u - dIn - eIn) / crs;
            pos = 0.18 + 0.64 * k;
            amp = 1;
        }
        else if (u < mid)
        {
            var k = (u - dIn - eIn - crs) / eOut;
            pos = 0.82 + 0.16 * (k * k * (3 - 2 * k));
            amp = 1;
        }
        else if (u < mid + dOut)
        {
            pos = 0.98;
            amp = 1 - (u - mid) / dOut;
        }
        else
        {
            pos = 0.98;
            amp = 0;
        }
    }

    /// <summary>光带重建：平滑高斯光廓（33 段），颜色=基底向高光色按高斯权重过渡，
    /// 无硬切段/暗沟/段带；amp 为下帧强度（两端驻留淡入淡出护栏）。</summary>
    private void SetLightSweep(double position, double amp)
    {
        const int samples = 33;
        const double sigma = 0.055;
        foreach (var p in _litParts)
        {
            if (p.Brush == null)
            {
                continue;
            }
            var gs = new GradientStopCollection();
            for (int i = 0; i < samples; i++)
            {
                var x = i / (double)(samples - 1);
                var rgb = (byte)(p.L.R * (1 - x) + p.D.R * x);
                var ggb = (byte)(p.L.G * (1 - x) + p.D.G * x);
                var bbl = (byte)(p.L.B * (1 - x) + p.D.B * x);
                var d = (x - position) / sigma;
                var gain = amp * Math.Exp(-0.5 * d * d);
                gs.Add(new GradientStop(Color.FromArgb(255,
                    (byte)(rgb + (p.Light.R - rgb) * gain),
                    (byte)(ggb + (p.Light.G - ggb) * gain),
                    (byte)(bbl + (p.Light.B - bbl) * gain)), x));
            }
            p.Brush.GradientStops = gs;
        }
    }

    private void ResetSpin()
    {
        SetPostShift(0);
        foreach (var p in _litParts)
        {
            p.Brush.GradientStops = Clone(p.Snapshot);
        }
    }

    private static Color Mix(Color a, Color b, double t)
    {
        return Color.FromArgb(
            (byte)(a.A * (1 - t) + b.A * t),
            (byte)(a.R * (1 - t) + b.R * t),
            (byte)(a.G * (1 - t) + b.G * t),
            (byte)(a.B * (1 - t) + b.B * t));
    }

    private static Color Dim(Color c, double t)
    {
        return Color.FromArgb(c.A, (byte)(c.R * t), (byte)(c.G * t), (byte)(c.B * t));
    }

    private static GradientStopCollection Clone(GradientStopCollection src)
    {
        var s = new GradientStopCollection();
        foreach (var gs in src)
        {
            s.Add(new GradientStop(gs.Color, gs.Offset));
        }
        return s;
    }

    /// <summary>渲染循环单帧步进：相位=墙钟连续时间 → 每帧恒定角速度、无一帧多跳；
    /// 连接柱 1-|cos| 收放；光带走 SweepMotion 缓巡廓线——两端驻留+淡入淡出+静默回绕，
    /// 从左到右单向巡游、两端切换慢而柔、循环无缝。</summary>
    private void AdvanceSpin()
    {
        var seconds = (System.Diagnostics.Stopwatch.GetTimestamp() - _spinStartTicks)
                      / (double)System.Diagnostics.Stopwatch.Frequency;
        _spinAngle = (seconds * SpinDegreesPerSecond) % 360;
        var rad = _spinAngle * Math.PI / 180;
        SetPostShift(1 - Math.Abs(Math.Cos(rad)));
        SweepMotion(_spinAngle / 360.0, out var pos, out var amp);
        SetLightSweep(pos, amp);
    }

    /// <summary>盖板滑动 tick：按 smoothstep 缓动把 Canvas.Top 从 110 插值到 80（或反向）。</summary>
    private void LidTick(object? sender, EventArgs e)
    {
        if (_openGroup == null)
        {
            return;
        }
        _lidElapsedMs += 16;
        var t = Math.Min(1.0, _lidElapsedMs / LidDurationMs);
        // smoothstep：起止更柔
        var eased = t * t * (3 - 2 * t);
        _openGroup.SetValue(Canvas.TopProperty, _lidFrom + (_lidTo - _lidFrom) * eased);
        if (t >= 1.0)
        {
            _lidTimer.Stop();
        }
    }

    /// <summary>
    /// 按行程百分比抬高腔头、按液位刷新液位高度并自动点亮感应器。
    /// HeadLift 越大腔头抬得越高（Y 取负值向上）；液位到达阈值时点亮对应感应器。
    /// </summary>
    private void UpdateGeometry()
    {
        var lift = Math.Clamp(HeadLift, 0, 100) / 100;
        var level = Math.Clamp(Level, 0, 100);

        if (_head != null)
        {
            var liftY = HeadTravelStart - lift * HeadTravelLength;
            if (_head.RenderTransform is TransformGroup group)
            {
                // 模板自带「旋转(倾斜) + 平移(升降)」组合变换时：
                // 只就地更新内部变换值，保留模板结构（含 Angle/Center 等原设定）。
                foreach (var child in group.Children)
                {
                    switch (child)
                    {
                        case TranslateTransform t:
                            // 合严时向下压 (start)；抬起时 Y 减小（向上主动抬升）
                            t.Y = liftY;
                            break;
                        case RotateTransform r:
                            r.Angle = Math.Clamp(Tilt, 0, 45);
                            break;
                    }
                }
            }
            else
            {
                _headTransform ??= new TranslateTransform();
                _head.RenderTransform = _headTransform;
                // 合严时向下压 (start)；抬起时 Y 减小（向上主动抬升）
                _headTransform.Y = liftY;
            }
        }

        // 仅“盖板条(Rectangle)”模板采用滑动模型（如新拟态主题）；
        // Steel/Glass 的 PART_OpenGroup 为 Canvas，沿用模板触发器平移，代码不干预。
        if (_openGroup is Rectangle && _lidTarget != HeadOpen)
        {
            // 开盖/关盖：工艺条(盖板) 在 80(开盖) 与 110(关盖) 之间滑动（首次与目标一致时不重复启动）
            var cur = _openGroup.GetValue(Canvas.TopProperty);
            var from = double.IsFinite((double)cur) ? (double)cur : LidCloseTop;
            var to = HeadOpen ? LidOpenTop : LidCloseTop;
            _lidTarget = HeadOpen;
            if (Math.Abs(from - to) > 0.5)
            {
                _lidFrom = from;
                _lidTo = to;
                _lidElapsedMs = 0;
                _lidTimer.Start();
            }
        }

        if (_liquid != null && _liquid.Parent is FrameworkElement parent)
        {
            // 阳极腔内区高度（父级 Grid 填满内壁），上下各留 1px 防止溢出圆角
            var available = Math.Max(0, parent.ActualHeight - 2);
            _liquid.Height = available * level / 100;
        }

        if (_highLevel != null)
        {
            // 高液位：液位到达顶部阈值即点亮（约 Level >= 72）
            _highLevel.Opacity = level >= 72 ? 1.0 : 0.22;
        }

        if (_lowLevel != null)
        {
            // 低液位：液位到达底部阈值即点亮（约 Level >= 18）
            _lowLevel.Opacity = level >= 18 ? 1.0 : 0.22;
        }
    }
}
