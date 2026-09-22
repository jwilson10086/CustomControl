using System.Windows;
using System.Windows.Controls;

namespace GeneralControl.Controls;

/// <summary>
/// 工业电机控件（P&amp;ID 电机符号）。
///
/// 【符号语义】
/// 圆形机壳 + 散热鳍 + 右侧轴伸；内部虚线环 = 转子磁场，
/// IsRunning 时绕中心旋转形成"转动感"，圆心 M 标识电机。
///
/// 【状态模型】
/// - IsRunning=true：转子环持续旋转；
/// - IsFault=true：红色警示，动画停止。
/// </summary>
public class GcMotor : Control
{
    static GcMotor()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcMotor), new FrameworkPropertyMetadata(typeof(GcMotor)));
    }

    public static readonly DependencyProperty IsRunningProperty =
        DependencyProperty.Register(nameof(IsRunning), typeof(bool), typeof(GcMotor),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsFaultProperty =
        DependencyProperty.Register(nameof(IsFault), typeof(bool), typeof(GcMotor),
            new PropertyMetadata(false));

    /// <summary>电机是否运行中（转子环旋转）。</summary>
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
}



