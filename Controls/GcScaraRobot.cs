using System.Windows;
using System.Windows.Controls;

namespace GeneralControl.Controls;

/// <summary>
/// SCARA 机械手控件（俯视图，四轴选择性柔顺装配机械臂）。
///
/// 【结构】
/// 基座 → 肩关节(Link1 水平大臂) → 肘关节(Link2 水平小臂) → 末端吸盘/夹爪。
/// 视角为俯视：两个旋转轴垂直于屏幕，末端 Z 轴用 HoldingWafer 表达吸附状态。
///
/// 【角度模型】
/// ShoulderAngle / ElbowAngle 为相对父级角度（度，顺时针为正），
/// 直接驱动模板内 RotateTransform.Angle。
/// </summary>
public class GcScaraRobot : Control
{
    static GcScaraRobot()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcScaraRobot), new FrameworkPropertyMetadata(typeof(GcScaraRobot)));
    }

    public static readonly DependencyProperty ShoulderAngleProperty =
        DependencyProperty.Register(nameof(ShoulderAngle), typeof(double), typeof(GcScaraRobot),
            new PropertyMetadata(-30.0));

    public static readonly DependencyProperty ElbowAngleProperty =
        DependencyProperty.Register(nameof(ElbowAngle), typeof(double), typeof(GcScaraRobot),
            new PropertyMetadata(65.0));

    public static readonly DependencyProperty HoldingWaferProperty =
        DependencyProperty.Register(nameof(HoldingWafer), typeof(bool), typeof(GcScaraRobot),
            new PropertyMetadata(true));

    public static readonly DependencyProperty IsServoOnProperty =
        DependencyProperty.Register(nameof(IsServoOn), typeof(bool), typeof(GcScaraRobot),
            new PropertyMetadata(true));

    /// <summary>肩关节角度（大臂相对基座，顺时针为正）。</summary>
    public double ShoulderAngle { get => (double)GetValue(ShoulderAngleProperty); set => SetValue(ShoulderAngleProperty, value); }

    /// <summary>肘关节角度（小臂相对大臂，顺时针为正）。</summary>
    public double ElbowAngle { get => (double)GetValue(ElbowAngleProperty); set => SetValue(ElbowAngleProperty, value); }

    /// <summary>末端是否吸附晶圆。</summary>
    public bool HoldingWafer { get => (bool)GetValue(HoldingWaferProperty); set => SetValue(HoldingWaferProperty, value); }

    /// <summary>伺服是否上电（基座指示灯）。</summary>
    public bool IsServoOn { get => (bool)GetValue(IsServoOnProperty); set => SetValue(IsServoOnProperty, value); }
}
