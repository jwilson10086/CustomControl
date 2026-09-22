using System.Windows;
using System.Windows.Controls;

namespace GeneralControl.Controls;

/// <summary>叶轮旋转方向。</summary>
public enum GcSpinDirection
{
    /// <summary>顺时针（默认）。</summary>
    Clockwise,

    /// <summary>逆时针。</summary>
    Counterclockwise
}

/// <summary>排出口朝向。枚举值即相对"朝右"的顺时针角度，与模板中的 RotateTransform.Angle 一一对应。</summary>
public enum GcOutletDirection
{
    /// <summary>朝右（默认）。</summary>
    Right = 0,

    /// <summary>朝下。</summary>
    Down = 90,

    /// <summary>朝左。</summary>
    Left = 180,

    /// <summary>朝上。</summary>
    Up = 270
}

/// <summary>
/// 工业泵控件（涡壳 + 叶轮符号）。
///
/// 【符号语义】
/// 外圈圆 = 泵涡壳（Volute）；内部弧形叶片 = 叶轮（Impeller）；
/// 右侧锥形颈 = 排出口（Discharge）。
///
/// 【状态模型】
/// - IsRunning=true：叶轮持续旋转动画（方向由 GcSpinDirection 决定）；
/// - IsFault=true：红色警示，动画停止；
/// - GcOutletDirection：排出口朝向（右/下/左/上）；
/// - InletDirection：吸入口朝向（左/右/上/下，默认左）。
/// </summary>
public class GcPump : Control
{
    static GcPump()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcPump), new FrameworkPropertyMetadata(typeof(GcPump)));
    }

    public static readonly DependencyProperty IsRunningProperty =
        DependencyProperty.Register(nameof(IsRunning), typeof(bool), typeof(GcPump),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsFaultProperty =
        DependencyProperty.Register(nameof(IsFault), typeof(bool), typeof(GcPump),
            new PropertyMetadata(false));

    public static readonly DependencyProperty OutletDirectionProperty =
        DependencyProperty.Register("OutletDirection", typeof(GcOutletDirection), typeof(GcPump),
            new PropertyMetadata(GcOutletDirection.Right));

    public static readonly DependencyProperty InletDirectionProperty =
        DependencyProperty.Register(nameof(InletDirection), typeof(GcOutletDirection), typeof(GcPump),
            new PropertyMetadata(GcOutletDirection.Left));

    public static readonly DependencyProperty SpinDirectionProperty =
        DependencyProperty.Register("SpinDirection", typeof(GcSpinDirection), typeof(GcPump),
            new PropertyMetadata(GcSpinDirection.Clockwise));

    /// <summary>泵是否运行中（叶轮旋转）。</summary>
    public bool IsRunning
    {
        get => (bool)GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }

    /// <summary>是否故障。</summary>
    public bool IsFault
    {
        get => (bool)GetValue(IsFaultProperty);
        set => SetValue(IsFaultProperty, value);
    }

    /// <summary>排出口朝向：右 / 下 / 左 / 上。</summary>
    public GcOutletDirection OutletDirection
    {
        get => (GcOutletDirection)GetValue(OutletDirectionProperty);
        set => SetValue(OutletDirectionProperty, value);
    }

    /// <summary>吸入口朝向：左（默认）/ 右 / 上 / 下。</summary>
    public GcOutletDirection InletDirection
    {
        get => (GcOutletDirection)GetValue(InletDirectionProperty);
        set => SetValue(InletDirectionProperty, value);
    }

    /// <summary>叶轮旋转方向：顺时针 / 逆时针。</summary>
    public GcSpinDirection SpinDirection
    {
        get => (GcSpinDirection)GetValue(SpinDirectionProperty);
        set => SetValue(SpinDirectionProperty, value);
    }
}




