using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace GeneralControl.Controls;

/// <summary>
/// 工业冰水机控件（冷水机组正面视图）。
///
/// 【符号语义】
/// 机柜 + 双散热风扇（IsRunning 时旋转）+ 温度面板（设置温度/实际温度）
/// + 电源开关 + 状态灯。
///
/// 【状态模型】
/// - IsRunning=true：电源开关合 + 风扇旋转；
/// - IsFault=true：红色警示，风扇停转。
/// </summary>
public class GcChiller : Control
{
    static GcChiller()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcChiller), new FrameworkPropertyMetadata(typeof(GcChiller)));
    }

    public static readonly DependencyProperty SetTempProperty =
        DependencyProperty.Register(nameof(SetTemp), typeof(double), typeof(GcChiller),
            new PropertyMetadata(7.0));

    public static readonly DependencyProperty ActualTempProperty =
        DependencyProperty.Register(nameof(ActualTemp), typeof(double), typeof(GcChiller),
            new PropertyMetadata(12.0));

    public static readonly DependencyProperty IsRunningProperty =
        DependencyProperty.Register(nameof(IsRunning), typeof(bool), typeof(GcChiller),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsFaultProperty =
        DependencyProperty.Register(nameof(IsFault), typeof(bool), typeof(GcChiller),
            new PropertyMetadata(false));

    /// <summary>设置温度（°C）。</summary>
    public double SetTemp
    {
        get => (double)GetValue(SetTempProperty);
        set => SetValue(SetTempProperty, value);
    }

    /// <summary>实际温度（°C）。</summary>
    public double ActualTemp
    {
        get => (double)GetValue(ActualTempProperty);
        set => SetValue(ActualTempProperty, value);
    }

    /// <summary>是否运行（电源开关 + 风扇旋转）。</summary>
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
