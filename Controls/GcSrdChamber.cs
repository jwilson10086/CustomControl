using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GeneralControl.Controls;

/// <summary>
/// SRD 腔控件 —— 旋转喷淋甩干腔（由 GcAptChamber 改造而来）。
///
/// ① 下半部分（PART_Head）：原样保留，静止（升降不再作用在腔头上）；
/// ② 喷淋（PART_OpenGroup + sprayNozzle + spray）：与 APT 完全相同的喷雾臂——
///    HeadOpen 向右拉长（Width 20→65，左端固定），导向套不动，喷头（sprayNozzle）
///    与喷锥（spray）随臂尖平移；喷雾由 Spray 属性控制（true 才显示，雾滴做下落
///    动画），长度由 SprayLength 缩放（0~100）；
/// ③ 气罩（PART_Shroud）：升降 HeadLift 驱动其上下（0=收在槽口 / 100=升到顶），
///    升到顶时罩顶不超过喷头高度（模板几何保证）；
/// ④ V 型卡盘 chuck（chuckLegL/C/R）：三条腿绕竖直轴均布 120° 旋转的
///    【真实3D投影呼吸式】卡盘——静止(方位角0°)正视投影 = 一根居中在前（透视稍短）
///    + 两根在左右两侧；旋转时三根腿依次呼吸、两两在投影面重叠交叉（每旋转到
///    某些相位两根投影重合），端帽（chuckCapL/C/R）恒水平、随腿尖端部平移；
///    锥尖（世界 (100,46)）在顶压板（PART_SpinBase）下方居中；旋转相位=视角方位角；
///    顶压板渐变扫光。
/// ⑤ 自动模拟（AutoSimulate=true）：循环执行
///    有片→升降到位(气罩升起)→旋转→喷雾臂到位→喷雾→关雾回臂→旋转停→下降回位(气罩回落)；
///    全部动画由渲染帧（CompositionTarget.Rendering，vsync 同拍）驱动，
///    不用 DispatcherTimer——升降/拉长/旋转/喷雾四路同走一个帧泵。
/// </summary>

[TemplatePart(Name = PartHead, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartShroud, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartOpenGroup, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartLiquid, Type = typeof(Border))]
    [TemplatePart(Name = PartHighLevel, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartLowLevel, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartChuckLegL, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartChuckLegR, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartChuckCapL, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartChuckCapR, Type = typeof(FrameworkElement))]
    public class GcSrdChamber : Control
    {
    
    private const string PartHead = "PART_Head";
    private const string PartShroud = "PART_Shroud";
    private const string PartOpenGroup = "PART_OpenGroup";
    private const string PartLiquid = "PART_Liquid";
    private const string PartHighLevel = "PART_HighLevel";
    private const string PartLowLevel = "PART_LowLevel";
    private const string PartSpray = "spray";
    private const string PartSprayNozzle = "sprayNozzle";
    private const string PartChuckLegL = "chuckLegL";
    private const string PartChuckLegC = "chuckLegC";
    private const string PartChuckLegR = "chuckLegR";
    private const string PartChuckCapL = "chuckCapL";
    private const string PartChuckCapC = "chuckCapC";
    private const string PartChuckCapR = "chuckCapR";

    static GcSrdChamber()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcSrdChamber), new FrameworkPropertyMetadata(typeof(GcSrdChamber)));
    }

    public GcSrdChamber()
    {
        // 控件尺寸变化（含首次布局）时重算升降与液位。
        // 在构造函数挂接保证只订阅一次（OnApplyTemplate 可能被多次调用）。
        SizeChanged += (_, _) => UpdateGeometry();
    }

    // ==================== 依赖属性 ====================

    public static readonly DependencyProperty HeadOpenProperty =
        DependencyProperty.Register(nameof(HeadOpen), typeof(bool), typeof(GcSrdChamber),
            new PropertyMetadata(false, OnStateChanged));

    public static readonly DependencyProperty HeadLiftProperty =
        DependencyProperty.Register(nameof(HeadLift), typeof(double), typeof(GcSrdChamber),
            new PropertyMetadata(0.0, OnStateChanged));

    public static readonly DependencyProperty TiltProperty =
        DependencyProperty.Register(nameof(Tilt), typeof(double), typeof(GcSrdChamber),
            new PropertyMetadata(0.0, OnStateChanged));

    public static readonly DependencyProperty IsRotatingProperty =
        DependencyProperty.Register(nameof(IsRotating), typeof(bool), typeof(GcSrdChamber),
            new PropertyMetadata(false, OnStateChanged));

    public static readonly DependencyProperty SprayProperty =
        DependencyProperty.Register(nameof(Spray), typeof(bool), typeof(GcSrdChamber),
            new PropertyMetadata(false, OnStateChanged));

    public static readonly DependencyProperty SprayLengthProperty =
        DependencyProperty.Register(nameof(SprayLength), typeof(double), typeof(GcSrdChamber),
            new PropertyMetadata(30.0, OnStateChanged));

    public static readonly DependencyProperty HasWaferProperty =
        DependencyProperty.Register(nameof(HasWafer), typeof(bool), typeof(GcSrdChamber),
            new PropertyMetadata(false, OnStateChanged));

    public static readonly DependencyProperty AutoSimulateProperty =
        DependencyProperty.Register(nameof(AutoSimulate), typeof(bool), typeof(GcSrdChamber),
            new PropertyMetadata(false, OnStateChanged));

    public static readonly DependencyProperty LevelProperty =
        DependencyProperty.Register(nameof(Level), typeof(double), typeof(GcSrdChamber),
            new PropertyMetadata(40.0, OnStateChanged));

    public static readonly DependencyProperty LiquidBrushProperty =
        DependencyProperty.Register(nameof(LiquidBrush), typeof(Brush), typeof(GcSrdChamber),
            new PropertyMetadata(default(Brush)));

    /// <summary>喷雾臂是否打开：true=喷臂向右拉长并放出圆锥喷雾 / false=收回隐藏（代码后置动画）。</summary>
    public bool HeadOpen
    {
        get => (bool)GetValue(HeadOpenProperty);
        set => SetValue(HeadOpenProperty, value);
    }

    /// <summary>升降 0~100：驱动气罩（PART_Shroud）上下——0=收在槽口 / 100=升到顶。</summary>
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

    /// <summary>是否旋转：正视视角下绕垂直轴(Y)伪 3D 旋转（角度 0~360 循环）。</summary>
    public bool IsRotating
    {
        get => (bool)GetValue(IsRotatingProperty);
        set => SetValue(IsRotatingProperty, value);
    }

    /// <summary>喷雾开关：true=显示喷头喷锥并播放下落动画 / false=隐藏（XAML 触发器控制显隐）。</summary>
    public bool Spray
    {
        get => (bool)GetValue(SprayProperty);
        set => SetValue(SprayProperty, value);
    }

    /// <summary>喷头长度 0~100：喷锥 Y 向缩放（0.2x~1.6x），物理长度改这里即可。</summary>
    public double SprayLength
    {
        get => (double)GetValue(SprayLengthProperty);
        set => SetValue(SprayLengthProperty, value);
    }

    /// <summary>是否有晶圆：true=卡盘顶端显示晶圆（三只端帽上搁置的硅片）/ false=隐藏。</summary>
    public bool HasWafer
    {
        get => (bool)GetValue(HasWaferProperty);
        set => SetValue(HasWaferProperty, value);
    }

    /// <summary>自动模拟开关：true=循环执行
    /// 有片→升降到位(气罩升起)→旋转→喷雾臂到位→喷雾→关雾回臂→旋转停→下降回位(气罩回落)，
    /// 全程由渲染帧(vsync)驱动。</summary>
    public bool AutoSimulate
    {
        get => (bool)GetValue(AutoSimulateProperty);
        set => SetValue(AutoSimulateProperty, value);
    }

    /// <summary>液位 0~100。</summary>
    public double Level
    {
        get => (double)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    /// <summary>液体填充画刷。默认 null 时使用模板内置的液体渐变（蓝色半透明）。</summary>
    public Brush? LiquidBrush
    {
        get => (Brush?)GetValue(LiquidBrushProperty);
        set => SetValue(LiquidBrushProperty, value);
    }

    // ==================== 模板部件 ====================

    private FrameworkElement? _spray;
    private FrameworkElement? _shroud;
    private FrameworkElement? _nozzle;
    private FrameworkElement? _openGroup;
    private Border? _liquid;
    private FrameworkElement? _highLevel;
    private FrameworkElement? _lowLevel;

    private TranslateTransform? _shroudTransform;
    /// <summary>气罩收回时的下移量（逻辑坐标）；HeadLift=100 时归 0（升到顶）。
    /// 收回(0)时罩体落在槽口下方，升起(100)时罩顶≤喷头高度。</summary>
    private const double ShroudTravelLength = 58;

    // 喷雾臂拉长动画（与 APT 相同）：挂在统一渲染帧泵里插值“进度 0~1”（vsync 同拍），
    // 不用 DispatcherTimer（0.4s smoothstep，0⇄1）。
    private const int LidDurationMs = 400;
    private bool? _lidTarget;
    private double _lidFrom;
    private double _lidTo;
    private long _lidStartTicks;
    /// <summary>拉长动画正在推进（帧泵持有期间为 true）。</summary>
    private bool _lidAnimating;
    /// <summary>拉长进度 0..1（0=臂收回, 1=臂全张开喷雾）。</summary>
    private double _lidProgress;

    // 喷雾臂几何参数（逻辑坐标，与模板尺寸对应）：HeadOpen 时仅拉长 Width（20→65），
    // 左端固定（导向套不动）；喷头 + 喷锥随臂尖平移（臂尖X = 左端 + 当前宽）。
    private const double NozzleArmLeft = 34;
    private const double NozzleArmWidth = 20;
    private const double NozzleArmWidthOpen = 65;
    /// <summary>喷头宽 14（漏斗），容器左移半宽使其中心对齐臂尖。</summary>
    private const double NozzleNozzleHalf = 7;

    // 正视 Y 轴旋转【真实3D投影·三条腿】：三条腿绕竖直锥尖轴均布 120°、恒速整圈转，
    // 每条腿与竖直轴成 60°。正视投影 = 腿长出/缩短（透视）+ 左右摆动（呼吸），静止
    // (方位角=0°) 为一根居中在前(稍短) + 两根在两侧；旋转到某些相位两根投影重合
    // （两两重叠）。枢轴=腿 Canvas 底边中点 (10,50) → 锥尖世界 (100,46)；腿长 50。
    private const string PartSpinBase = "PART_SpinBase";
    /// <summary>腿枢轴（本地坐标，腿 Canvas 底边中点=锥尖 apex）。腿 Canvas 放于 (90,-4)，臂向上。</summary>
    private const double PostPivotCenterX = 10;
    private const double PostPivotCenterY = 50;
    /// <summary>静止(不旋转)相位：方位角 0° = 正视一根居中 + 两根两侧。</summary>
    private const double PostRestDegrees = 0;
    private readonly System.Collections.Generic.List<(FrameworkElement Fe, ScaleTransform Scale, RotateTransform Rot)> _spinLegs = new();
    /// <summary>端帽（与腿一一对应）：位置由 ApplyChuckSpin 每帧改写，恒水平。</summary>
    private readonly System.Collections.Generic.List<FrameworkElement> _spinCaps = new();
    /// <summary>腿投影几何：投影横系数 sin60°、投影纵系数 cos60°、腿长（逻辑像素）。</summary>
    private const double SpinLegHalfSpread = 0.8660254037844386;
    private const double SpinLegProjectedY = 0.5;
    private const double SpinLegLength = 50;
    private const double SpinApexX = 100;
    private const double SpinApexY = 46;
    /// <summary>腿基方位角（与 Left 同起点即 L210°→投影左侧、C90°→居中在前、R330°→右侧）：
    /// 静止(α=0)时一张一合为正视三脚架。</summary>
    private static readonly double[] SpinLegAzimuthDeg = { 210, 90, 330 };
    private readonly System.Collections.Generic.List<LitPart> _litParts = new();
    /// <summary>旋转速度（°/s）。亦作为高光带的匀速率：一整圈左→右扫一遍游标。</summary>
    private const double SpinDegreesPerSecond = 125.0;
    private long _spinStartTicks;
    /// <summary>仅在一次渲染循环中推进的相位（0~360°）。连续时间驱动 + 每帧一更，
    /// 取代旧版离散 16ms 定时器（其落帧会偶发 1×/2× 步进不均 → 观感顿挫）。</summary>
    private double _spinAngle;
    /// <summary>旋转是否在推进（IsRotating=true 时帧泵持有）。</summary>
    private bool _spinActive;
    private EventHandler? _renderHandler;

    // ==================== 自动模拟（渲染帧驱动状态机） ====================
    /// <summary>自动模拟是否在跑（AutoSimulate=true 时帧泵持有）。</summary>
    private bool _autoRunning;
    /// <summary>当前阶段索引：0 有片→1 升降到位→2 旋转→3 喷雾臂到位→4 喷雾→5 关雾回臂→6 旋转停→7 下降回位。</summary>
    private int _autoPhase;
    /// <summary>当前阶段已流逝秒数。</summary>
    private double _autoElapsed;
    /// <summary>上一帧时间戳（计算帧间 delta）。</summary>
    private long _autoPrevTicks;

    /// <summary>受光照部件：渐变刷已被接管为可变的“扫光”刷（水平基底 + 移动高光带），
    /// 并保存原始竖向渐变快照以便停转复位。L=顶亮 D=底暗 Mid=中调 Light=高光色。</summary>
    private readonly record struct LitPart(
        LinearGradientBrush Brush, Color L, Color D, Color Mid, Color Light,
        GradientStopCollection Snapshot);

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        
        _shroud = GetTemplateChild(PartShroud) as FrameworkElement;
        _openGroup = GetTemplateChild(PartOpenGroup) as FrameworkElement;
        _liquid = GetTemplateChild(PartLiquid) as Border;
        _highLevel = GetTemplateChild(PartHighLevel) as FrameworkElement;
        _lowLevel = GetTemplateChild(PartLowLevel) as FrameworkElement;
        _spray = GetTemplateChild(PartSpray) as FrameworkElement;
        _nozzle = GetTemplateChild(PartSprayNozzle) as FrameworkElement;

        _spinLegs.Clear();
        _spinCaps.Clear();
        foreach (var name in new[] { PartChuckLegL, PartChuckLegC, PartChuckLegR })
        {
            // 模板内联变换应用后是只读(frozen)的：换成代码创建的未冻结「缩放+旋转」组，
            // 缩放做透视缩短（投影长度随方位角呼吸）、旋转摆出屏幕张角。
            if (GetTemplateChild(name) is FrameworkElement fe)
            {
                var scale = new ScaleTransform(1, 1, PostPivotCenterX, PostPivotCenterY);
                var rot = new RotateTransform(0, PostPivotCenterX, PostPivotCenterY);
                var group = new TransformGroup();
                group.Children.Add(scale);
                group.Children.Add(rot);
                fe.RenderTransform = group;
                _spinLegs.Add((fe, scale, rot));
            }
        }
        foreach (var name in new[] { PartChuckCapL, PartChuckCapC, PartChuckCapR })
        {
            // 端帽：恒水平，位置由 ApplyChuckSpin 每帧改写（随腿尖端部平移）。
            if (GetTemplateChild(name) is FrameworkElement fe)
            {
                _spinCaps.Add(fe);
            }
        }

        // 光照部件：仅【顶压板】被接管为可变“扫光”刷 —— 水平基底渐变(左亮→右暗)之上
        // 一条移动高光带，旋转期间几何不动，只让高光带随角度从左往右扫过（光追移动感）。
        // 注意：喷头 spray / 喷雾臂不注册进 _litParts —— 保证喷雾区永不参与渐变扫光。
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
        foreach (var name in new[] { PartSpinBase })
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

        // 喷头 spray：喷雾长度由 SprayLength 缩放（Y 向，从喷口 CenterY=3 起）。
        if (_spray != null)
        {
            _spray.RenderTransform = new ScaleTransform { CenterX = 0, CenterY = 3 };
            ApplySprayScale();
        }

        // 气罩：只做竖直平移（0=收在槽口 / 全升起），外形在模板里固定。
        if (_shroud != null)
        {
            _shroudTransform = new TranslateTransform();
            _shroud.RenderTransform = _shroudTransform;
        }

        UpdateGeometry();
        ApplySpinState();
    }

    private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var chamber = (GcSrdChamber)d;
        if (e.Property == AutoSimulateProperty)
        {
            // 自动模拟开关：起停帧泵与复位，不额外触发 UpdateGeometry
            if ((bool)e.NewValue)
            {
                chamber.StartAutoSimulation();
            }
            else
            {
                chamber.StopAutoSimulation();
            }
            return;
        }
        chamber.UpdateGeometry();
        if (e.Property == IsRotatingProperty)
        {
            chamber.ApplySpinState();
        }
    }

    /// <summary>IsRotating ⇨ 启用/停用旋转推进（帧泵统一驱动，vsync 每帧精确更新一次，
    /// 相位由墙钟连续推进），停转时卡盘与光带复位。</summary>
    private void ApplySpinState()
    {
        if (IsRotating)
        {
            if (_spinActive)
            {
                return;
            }
            _spinActive = true;
            _spinStartTicks = System.Diagnostics.Stopwatch.GetTimestamp();
            EnsureFrameLoop();
        }
        else
        {
            if (_spinActive)
            {
                _spinActive = false;
                ResetSpin();
            }
            EnsureFrameLoop();
        }
    }

    /// <summary>统一渲染帧泵：旋转 / 自动模拟 / 喷雾臂拉长 三位一体，
    /// 都按 CompositionTarget.Rendering（vsync 同拍）每帧推进——不复用
    /// DispatcherTimer，无定时器 16ms 落帧导致的步进不均。</summary>
    private void EnsureFrameLoop()
    {
        if (_renderHandler == null)
        {
            _renderHandler = FrameTick;
            System.Windows.Media.CompositionTarget.Rendering += _renderHandler;
        }
    }

    private void StopFrameLoop()
    {
        if (_renderHandler != null)
        {
            System.Windows.Media.CompositionTarget.Rendering -= _renderHandler;
            _renderHandler = null;
        }
    }

    private void FrameTick(object? sender, EventArgs e)
    {
        if (_spinActive)
        {
            AdvanceSpin();
        }
        if (_autoRunning)
        {
            AdvanceAuto();
        }
        if (_lidAnimating)
        {
            AdvanceLid();
        }
        if (!_spinActive && !_autoRunning && !_lidAnimating)
        {
            StopFrameLoop();
        }
    }

    /// <summary>真实3D投影驱动（三腿均布120°）：α=视角方位角（°）。每条腿与竖直轴成
    /// 60°，绕竖直锥尖轴恒速整圈转。某腿正视投影方向=(sin60°·cos(φi+α), −cos60°)，
    /// 投影长度=√(sin²60°·cos²(φi+α) + cos²60°)——腿长出/缩短(透视)+左右摆动(呼吸)。
    /// 静止(α=0)=一根居中在前(稍短)+两根两侧；旋转中两腿投影重合即“重叠”。端帽恒
    /// 水平、随腿尖端部平移。深度：zi=sin60°·sin(φi+α) 越大越靠前，ZIndex 由 zi 排序。</summary>
    private void ApplyChuckSpin(double azimuthDegrees)
    {
        var n = _spinLegs.Count;
        var z = new double[n];
        for (int i = 0; i < n; i++)
        {
            var phi = (SpinLegAzimuthDeg[i] + azimuthDegrees) * Math.PI / 180.0;
            var c = Math.Cos(phi);
            z[i] = SpinLegHalfSpread * Math.Sin(phi);
            var pLen = Math.Sqrt(SpinLegHalfSpread * SpinLegHalfSpread * c * c + SpinLegProjectedY * SpinLegProjectedY);
            var ang = Math.Atan2(SpinLegHalfSpread * c, SpinLegProjectedY) * 180.0 / Math.PI;
            var (fe, scale, rot) = _spinLegs[i];
            rot.Angle = ang;
            scale.ScaleX = pLen;
            scale.ScaleY = pLen;
            if (i < _spinCaps.Count)
            {
                var tipX = SpinApexX + SpinLegHalfSpread * SpinLegLength * c;
                var tipY = SpinApexY - SpinLegProjectedY * SpinLegLength;
                Canvas.SetLeft(_spinCaps[i], tipX - 8);
                Canvas.SetTop(_spinCaps[i], tipY - 2.5);
            }
        }
        // 深度排序：zi 越大越靠前 → ZIndex 越高；两腿投影重合时前腿盖住后腿。
        for (int i = 0; i < n; i++)
        {
            var rank = 1;
            for (int j = 0; j < n; j++)
            {
                if (j == i)
                {
                    continue;
                }
                if (z[j] > z[i] + 1e-9 || (Math.Abs(z[j] - z[i]) <= 1e-9 && j < i))
                {
                    rank++;
                }
            }
            Panel.SetZIndex(_spinLegs[i].Fe, rank);
            if (i < _spinCaps.Count)
            {
                Panel.SetZIndex(_spinCaps[i], rank);
            }
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
        ApplyChuckSpin(PostRestDegrees);
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
    /// 三条腿从静止方位角 0°（正视=一根居中+两根两侧）起绕竖直轴恒速整圈转，
    /// 真实3D投影呼吸（腿长出/缩短+两两重叠），端帽随腿尖平移；光带走 SweepMotion
    /// 缓巡廓线——两端驻留+淡入淡出+静默回绕，从左到右单向巡游、循环无缝。</summary>
    private void AdvanceSpin()
    {
        var seconds = (System.Diagnostics.Stopwatch.GetTimestamp() - _spinStartTicks)
                      / (double)System.Diagnostics.Stopwatch.Frequency;
        _spinAngle = (PostRestDegrees + seconds * SpinDegreesPerSecond) % 360;
        ApplyChuckSpin(_spinAngle);
        SweepMotion(_spinAngle / 360.0, out var pos, out var amp);
        SetLightSweep(pos, amp);
    }

    /// <summary>喷雾臂拉长单帧推进：按 smoothstep 缓动推进进度 0⇄1（墙钟计 0.4s），
    /// 同步臂长（Width）。挂在统一帧泵里，与旋转/自动模拟同拍。</summary>
    private void AdvanceLid()
    {
        if (_openGroup == null)
        {
            _lidAnimating = false;
            return;
        }
        var t = Math.Min(1.0, (System.Diagnostics.Stopwatch.GetTimestamp() - _lidStartTicks)
                    / (double)System.Diagnostics.Stopwatch.Frequency / (LidDurationMs / 1000.0));
        var eased = t * t * (3 - 2 * t);
        _lidProgress = _lidFrom + (_lidTo - _lidFrom) * eased;
        ApplyNozzleState();
        if (t >= 1.0)
        {
            _lidAnimating = false;
        }
    }

    /// <summary>喷雾臂驱动：HeadOpen 只拉长 Width（20→65，左端固定向右延展）；
    /// 导向套不动，喷头（sprayNozzle）与喷锥（spray）随臂尖一起平移。
    /// 显隐由 Spray 属性在模板触发器里控制（XAML）。</summary>
    private void ApplyNozzleState()
    {
        if (_openGroup is Rectangle arm)
        {
            arm.Width = NozzleArmWidth + _lidProgress * (NozzleArmWidthOpen - NozzleArmWidth);
            var tipX = NozzleArmLeft + arm.Width;
            if (_spray != null)
            {
                // 喷锥中轴对齐喷头漏斗中心（= 臂尖 − 漏斗半宽），否则锥会偏右一截
                Canvas.SetLeft(_spray, tipX - NozzleNozzleHalf);
            }
            if (_nozzle != null)
            {
                Canvas.SetLeft(_nozzle, tipX - NozzleNozzleHalf);
            }
        }
    }

    /// <summary>气罩驱动：HeadLift 0~100 → 控制气罩（PART_Shroud）上下。
    /// 0=收在槽口（下移到槽口下方）/ 100=升到顶（罩顶恰好不高于喷头，模板几何保证）。</summary>
    private void ApplyShroudLift()
    {
        if (_shroudTransform == null)
        {
            return;
        }
        var lift = Math.Clamp(HeadLift, 0, 100) / 100.0;
        _shroudTransform.Y = ShroudTravelLength * (1 - lift);
    }

    /// <summary>喷头长度：SprayLength 0~100 → 喷锥 Y 向缩放 0.2x~1.6x（0 时缩成短喷）。
    /// 想改物理形状直接在模板 XAML 里动 Path 的 66/62/56 的高度坐标即可。</summary>
    private void ApplySprayScale()
    {
        if (_spray?.RenderTransform is ScaleTransform st)
        {
            var len = Math.Clamp(SprayLength, 0, 100) / 100.0;
            st.ScaleY = 0.2 + 1.4 * len;
        }
    }

    // ==================== 自动模拟状态机 ====================
    // 阶段时序（秒）：0 有片 / 1 升降到位(气罩升起) / 2 旋转起转 / 3 喷雾臂到位 /
    // 4 喷雾 / 5 关雾+回臂 / 6 旋转停 / 7 下降回位(气罩回落) → 回到 0 循环往复。
    private const double AutoDwellLoad = 0.6;    // 0：晶圆上料展示
    private const double AutoLiftUp = 1.6;       // 1：HeadLift（气罩）0→100
    private const double AutoSpinCue = 0.6;      // 2：卡盘起转
    private const double AutoArmCue = 0.7;       // 3：臂拉长（含 0.4s 伸出动画）
    private const double AutoSpray = 2.4;        // 4：喷雾保持
    private const double AutoArmIn = 0.7;        // 5：关雾 + 臂收回
    private const double AutoSpinStop = 0.6;     // 6：旋转停
    private const double AutoLiftBack = 1.6;     // 7：HeadLift（气罩）100→0

    /// <summary>平滑曲线（smoothstep），用于升降（气罩）/拉长的缓入缓出。</summary>
    private static double Smooth(double t)
    {
        return t * t * (3 - 2 * t);
    }

    /// <summary>AutoSimulate=true：把各状态复位到初始（气罩收、先无片），
    /// 然后启动渲染帧泵循环。</summary>
    private void StartAutoSimulation()
    {
        if (_autoRunning)
        {
            return;
        }
        _autoRunning = true;
        // 初始位置：气罩收（HeadLift 0）、臂收回、喷雾关、不旋转、先无片
        HasWafer = false;
        HeadOpen = false;
        Spray = false;
        IsRotating = false;
        HeadLift = 0;
        _autoPhase = 0;
        _autoElapsed = 0;
        _autoPrevTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        EnsureFrameLoop();
    }

    /// <summary>AutoSimulate=false：停泵，保持当前画面（外部接管各属性）。</summary>
    private void StopAutoSimulation()
    {
        _autoRunning = false;
        if (!_spinActive && !_lidAnimating)
        {
            StopFrameLoop();
        }
    }

    /// <summary>帧泵单步：推进当前阶段（delta 增量），到点切下一阶段并执行进入动作。</summary>
    private void AdvanceAuto()
    {
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        var dt = (now - _autoPrevTicks) / (double)System.Diagnostics.Stopwatch.Frequency;
        _autoPrevTicks = now;
        _autoElapsed += dt;

        switch (_autoPhase)
        {
            case 0: // 有片
                HasWafer = true;
                if (_autoElapsed >= AutoDwellLoad)
                {
                    NextAutoPhase(AutoDwellLoad);
                }
                break;

            case 1: // 升降到位：HeadLift（气罩）0→100 缓入缓出
                HeadLift = 100 * Smooth(Math.Min(1.0, _autoElapsed / AutoLiftUp));
                if (_autoElapsed >= AutoLiftUp)
                {
                    HeadLift = 100;
                    NextAutoPhase(AutoLiftUp);
                }
                break;

            case 2: // 旋转起转（IsRotating=true，帧泵内开始呼吸）
                if (_autoElapsed >= AutoSpinCue)
                {
                    NextAutoPhase(AutoSpinCue);
                }
                break;

            case 3: // 喷雾臂到位（HeadOpen=true，臂拉长 0→1）
                if (_autoElapsed >= AutoArmCue)
                {
                    NextAutoPhase(AutoArmCue);
                }
                break;

            case 4: // 喷雾保持（Spray=true，喷锥 + 雾滴动画）
                if (_autoElapsed >= AutoSpray)
                {
                    NextAutoPhase(AutoSpray);
                }
                break;

            case 5: // 关雾 + 回臂（Spray=false、HeadOpen=false，臂 1→0）
                if (_autoElapsed >= AutoArmIn)
                {
                    NextAutoPhase(AutoArmIn);
                }
                break;

            case 6: // 旋转停（IsRotating=false，卡盘/光带复位）
                if (_autoElapsed >= AutoSpinStop)
                {
                    NextAutoPhase(AutoSpinStop);
                }
                break;

            case 7: // 下降回位：HeadLift（气罩）100→0
                HeadLift = 100 * (1 - Smooth(Math.Min(1.0, _autoElapsed / AutoLiftBack)));
                if (_autoElapsed >= AutoLiftBack)
                {
                    HeadLift = 0;
                    NextAutoPhase(AutoLiftBack);
                }
                break;
        }
    }

    /// <summary>切到下一阶段：扣除【刚结束阶段】的时长，只把溢出携带到下一段，
    /// 保证停留期到尾时续上的段从 0 平滑起步（else 会因负时间把缓动算爆→跳变卡顿）。</summary>
    private void NextAutoPhase(double currentDuration)
    {
        _autoPhase = (_autoPhase + 1) % 8;
        _autoElapsed -= currentDuration;
        switch (_autoPhase)
        {
            case 2:
                IsRotating = true;
                break;
            case 3:
                HeadOpen = true;
                break;
            case 4:
                SprayLength = 35;
                Spray = true;
                break;
            case 5:
                Spray = false;
                HeadOpen = false;
                break;
            case 6:
                IsRotating = false;
                break;
        }
    }

    /// <summary>
    /// 按升降百分比驱动气罩上下、按液位刷新液位高度并自动点亮感应器。
    /// 下半部分（PART_Head）保持静止，升降只作用在气罩（PART_Shroud）上。
    /// </summary>
    private void UpdateGeometry()
    {
        var level = Math.Clamp(Level, 0, 100);

        // 气罩：HeadLift 每次变更即时同步（首次模板应用亦生效）
        ApplyShroudLift();

        // 仅“喷雾臂(Rectangle)”模板采用拉长模型（与 APT 相同）：
        // HeadOpen ⇨ 臂向右拉长（Width 20→65）、喷头喷锥随臂尖平移；关闭 ⇨ 回缩雾隐。
        if (_openGroup is Rectangle && _lidTarget != HeadOpen)
        {
            var to = HeadOpen ? 1.0 : 0.0;
            _lidTarget = HeadOpen;
            if (Math.Abs(to - _lidProgress) > 0.01)
            {
                _lidFrom = _lidProgress;
                _lidTo = to;
                _lidStartTicks = System.Diagnostics.Stopwatch.GetTimestamp();
                _lidAnimating = true;
                EnsureFrameLoop();
            }
        }

        // 无论是否触发拉长动画，都同步一次臂/雾当前状态（首次模板应用亦生效）
        ApplyNozzleState();

        if (_liquid != null && _liquid.Parent is FrameworkElement parent)
        {
            // 内区高度（父级 Grid 填满内壁），上下各留 1px 防止溢出圆角
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

        // 喷头长度变化（SprayLength）随时生效
        ApplySprayScale();

        // 未旋转时复位（卡盘回位 + 光照回满），与 ApplySpinState 一致。
        // 自动模拟期间不由每帧 UpdateGeometry 反复复位（由状态机切停旋转时复位一次）。
        if (!IsRotating && !_autoRunning)
        {
            ResetSpin();
        }
    }
}
