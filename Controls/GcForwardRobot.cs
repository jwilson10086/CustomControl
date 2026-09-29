using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GeneralControl.Controls;

/// <summary>
/// 前置搬送机械手（Forward Robot）控件，双工位四轴串联结构。
///
/// 【结构】
/// 底座 → X1 主臂（绕底座上端转轴）→ X2 从臂（绕肘部转轴）
///      → X3 叉手（X1 工位）/ X4 叉手（X2 工位），两只叉手可各夹一片晶圆。
/// 四段 Canvas 逐级嵌套，每段挂自己的 RotateTransform，构成串联运动链；
/// 叉手上的椭圆夹持标记由 X1HasWafer / X2HasWafer 控制显隐。
///
/// 【姿态模型】
/// Pose 一个属性驱动 VisualStateManager，每个预设姿态给出 X1/X2/X3/X4 四段旋转角：
/// - Origin  原点；
/// - Load1~3 上料位（Load Position）1/2/3；
/// - Align1~4 对位扫描位（Aligner Scan）1/2/3/4；
/// - Buffer  缓存位，手爪走"先快后慢"的两段动作。
/// X1 前缀表示 X1 工位取放片动作，X2 前缀表示 X2 工位取放片动作。
///
/// 【动画】
/// 模板内每个姿态都有一组 VisualTransition（0→目标角，6 秒时长、SpeedRatio=6，
/// 约 1 秒走完），姿态切换时按过渡平滑插值；对应的 VisualState 负责停稳后的角度。
///
/// 【右键面板】
/// 与演示区第一个机械手 WaferRobot 一致：在本体上点右键浮出控制面板
/// （GcForwardRobotPanel），可切姿态、开关夹片，并实时显示四段关节角。
/// </summary>
public class GcForwardRobot : Control
{
    private RotateTransform? _x1;
    private RotateTransform? _x2;
    private RotateTransform? _x3;
    private RotateTransform? _x4;
    private GcForwardRobotPanel? _panel;

    static GcForwardRobot()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcForwardRobot), new FrameworkPropertyMetadata(typeof(GcForwardRobot)));
    }

    public GcForwardRobot()
    {
        // 脱离视觉树时顺手关掉面板，避免留下孤儿窗口
        Unloaded += (_, _) => CloseControlPanel();
    }

    public static readonly DependencyProperty PoseProperty =
        DependencyProperty.Register(nameof(Pose), typeof(GcForwardRobotPose), typeof(GcForwardRobot),
            new PropertyMetadata(GcForwardRobotPose.X1Origin, OnPoseChanged));

    public static readonly DependencyProperty HasWaferProperty =
        DependencyProperty.Register(nameof(HasWafer), typeof(bool), typeof(GcForwardRobot),
            new PropertyMetadata(true));

    public static readonly DependencyProperty X1HasWaferProperty =
        DependencyProperty.Register(nameof(X1HasWafer), typeof(bool), typeof(GcForwardRobot),
            new PropertyMetadata(false));

    public static readonly DependencyProperty X2HasWaferProperty =
        DependencyProperty.Register(nameof(X2HasWafer), typeof(bool), typeof(GcForwardRobot),
            new PropertyMetadata(false));

    /// <summary>当前预设姿态，切换时驱动模板内四段臂的旋转动画。</summary>
    public GcForwardRobotPose Pose
    {
        get => (GcForwardRobotPose)GetValue(PoseProperty);
        set => SetValue(PoseProperty, value);
    }

    /// <summary>机械手上是否有片（底座料仓指示）。</summary>
    public bool HasWafer
    {
        get => (bool)GetValue(HasWaferProperty);
        set => SetValue(HasWaferProperty, value);
    }

    /// <summary>X1 工位叉手上是否夹着片。</summary>
    public bool X1HasWafer
    {
        get => (bool)GetValue(X1HasWaferProperty);
        set => SetValue(X1HasWaferProperty, value);
    }

    /// <summary>X2 工位叉手上是否夹着片。</summary>
    public bool X2HasWafer
    {
        get => (bool)GetValue(X2HasWaferProperty);
        set => SetValue(X2HasWaferProperty, value);
    }

    /// <summary>X1 主臂当前角度（度），姿态过渡过程中连续变化。</summary>
    public double X1Angle => _x1?.Angle ?? 0d;

    /// <summary>X2 从臂当前角度（度）。</summary>
    public double X2Angle => _x2?.Angle ?? 0d;

    /// <summary>X3 叉手当前角度（度）。</summary>
    public double X3Angle => _x3?.Angle ?? 0d;

    /// <summary>X4 叉手当前角度（度）。</summary>
    public double X4Angle => _x4?.Angle ?? 0d;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // 抓四段臂的 RotateTransform，供 X1Angle~X4Angle 实时读数与面板显示使用。
        // 名字与模板里 x:Name 一致；模板尚未套用时为空，此时角度读数返回 0。
        _x1 = GetTemplateTransform("RobotX1ArmRotateAct");
        _x2 = GetTemplateTransform("RobotX2ArmRotateAct");
        _x3 = GetTemplateTransform("RobotX3ArmRotateAct");
        _x4 = GetTemplateTransform("RobotX4ArmRotateAct");

        // XAML 里写死的 Pose 在模板套用前就已赋值，那时的 GoToState 是空转，
        // 这里补一次，保证初始姿态一定被应用。
        VisualStateManager.GoToState(this, Pose.ToString(), true);
    }

    private RotateTransform? GetTemplateTransform(string name)
    {
        return Template?.FindName(name, this) as RotateTransform;
    }

    /// <summary>打开（或置顶）右键控制面板。</summary>
    public void ShowControlPanel()
    {
        if (_panel is not null && _panel.IsVisible)
        {
            _panel.Activate();
            return;
        }

        _panel = null;

        // 面板默认贴在本体右侧；本体还没上屏（无 PresentationSource）时退到主屏左上角，
        // 免得 PointToScreen 直接抛异常。
        var left = 40d;
        var top = 40d;
        if (PresentationSource.FromVisual(this) is not null)
        {
            left = PointToScreen(new Point(ActualWidth + 8, 0)).X;
            top = PointToScreen(new Point(0, 0)).Y;
        }

        var panel = new GcForwardRobotPanel(this)
        {
            Left = left,
            Top = top,
        };

        panel.Closed += (_, _) => _panel = null;
        _panel = panel;
        panel.Show();
        panel.Activate();
    }

    /// <summary>关闭已浮出的控制面板（本体脱离视觉树时调用）。</summary>
    public void CloseControlPanel()
    {
        _panel?.Close();
        _panel = null;
    }

    /// <summary>
    /// 右击本体浮出控制面板。
    /// 走 preview 而不是模板上的 MouseRightButtonUp：主题模板是 ResourceDictionary，
    /// 没有 x:Class 挂不了事件处理程序，所以事件只能在本体代码里接。
    /// </summary>
    protected override void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseRightButtonDown(e);

        if (e.Handled)
        {
            return;
        }

        ShowControlPanel();
        e.Handled = true;
    }

    private static void OnPoseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        VisualStateManager.GoToState((GcForwardRobot)d, ((GcForwardRobotPose)e.NewValue).ToString(), true);
    }
}

/// <summary>前置搬送机械手预设姿态，成员名与模板内 VisualState 名称一一对应。</summary>
public enum GcForwardRobotPose
{
    /// <summary>X1 工位原点。</summary>
    X1Origin,

    /// <summary>X1 工位上料位 1。</summary>
    X1Load1,

    /// <summary>X1 工位上料位 2。</summary>
    X1Load2,

    /// <summary>X1 工位上料位 3。</summary>
    X1Load3,

    /// <summary>X1 工位对位扫描位 1。</summary>
    X1Align1,

    /// <summary>X1 工位对位扫描位 2。</summary>
    X1Align2,

    /// <summary>X1 工位对位扫描位 3。</summary>
    X1Align3,

    /// <summary>X1 工位对位扫描位 4。</summary>
    X1Align4,

    /// <summary>X1 工位缓存位。</summary>
    X1Buffer,

    /// <summary>X2 工位原点。</summary>
    X2Origin,

    /// <summary>X2 工位上料位 1。</summary>
    X2Load1,

    /// <summary>X2 工位上料位 2。</summary>
    X2Load2,

    /// <summary>X2 工位上料位 3。</summary>
    X2Load3,

    /// <summary>X2 工位对位扫描位 1。</summary>
    X2Align1,

    /// <summary>X2 工位对位扫描位 2。</summary>
    X2Align2,

    /// <summary>X2 工位对位扫描位 3。</summary>
    X2Align3,

    /// <summary>X2 工位对位扫描位 4。</summary>
    X2Align4,

    /// <summary>X2 工位缓存位。</summary>
    X2Buffer,
}
