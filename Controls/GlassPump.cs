using System.Windows;
using System.Windows.Controls;

namespace CustomControl.Controls;

/// <summary>叶轮旋转方向。</summary>
public enum GlassSpinDirection
{
    /// <summary>顺时针（默认）。</summary>
    Clockwise,

    /// <summary>逆时针。</summary>
    Counterclockwise
}

/// <summary>排出口朝向。枚举值即相对"朝右"的顺时针角度，与模板中的 RotateTransform.Angle 一一对应。</summary>
public enum GlassOutletDirection
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
/// 外圈圆 = 泵涡壳（Volute）；内部叶片 = 叶轮（Impeller）；
/// 右侧三角 = 排出口（Discharge）。
///
/// 【状态模型】
/// - IsRunning=true：叶轮持续旋转动画（方向由 SpinDirection 决定）；
/// - IsFault=true：红色警示，动画停止；
/// - OutletDirection：排出口朝向（右/下/左/上）。
/// </summary>
public class GlassPump : Control
{
    static GlassPump()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassPump), new FrameworkPropertyMetadata(typeof(GlassPump)));
    }

    public static readonly DependencyProperty IsRunningProperty =
        DependencyProperty.Register(nameof(IsRunning), typeof(bool), typeof(GlassPump),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsFaultProperty =
        DependencyProperty.Register(nameof(IsFault), typeof(bool), typeof(GlassPump),
            new PropertyMetadata(false));

    public static readonly DependencyProperty OutletDirectionProperty =
        DependencyProperty.Register(nameof(OutletDirection), typeof(GlassOutletDirection), typeof(GlassPump),
            new PropertyMetadata(GlassOutletDirection.Right));

    public static readonly DependencyProperty SpinDirectionProperty =
        DependencyProperty.Register(nameof(SpinDirection), typeof(GlassSpinDirection), typeof(GlassPump),
            new PropertyMetadata(GlassSpinDirection.Clockwise));

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
    public GlassOutletDirection OutletDirection
    {
        get => (GlassOutletDirection)GetValue(OutletDirectionProperty);
        set => SetValue(OutletDirectionProperty, value);
    }

    /// <summary>叶轮旋转方向：顺时针 / 逆时针。</summary>
    public GlassSpinDirection SpinDirection
    {
        get => (GlassSpinDirection)GetValue(SpinDirectionProperty);
        set => SetValue(SpinDirectionProperty, value);
    }
}
