using System.Windows;
using System.Windows.Controls;

namespace GeneralControl.Controls;

/// <summary>
/// 工业整流器控件（带操作面板的整流电源）。
///
/// 【符号语义】
/// 机柜正面：顶部标题 + 指示/报警灯，中部四个读数区
/// （设定电压/设定电流/当前电压/当前电流），
/// 底部整流器开关（IsOn 时亮起）+ 报警上下限设置。
///
/// 【状态模型】
/// - IsOn=true：电源打开，指示灯亮；
/// - IsAlarm=true：越限报警，报警灯亮（红）。
/// </summary>
public class GcRectifier : Control
{
    static GcRectifier()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcRectifier), new FrameworkPropertyMetadata(typeof(GcRectifier)));
    }

    #region 依赖属性

    public static readonly DependencyProperty IsOnProperty =
        DependencyProperty.Register(nameof(IsOn), typeof(bool), typeof(GcRectifier),
            new PropertyMetadata(false));

    // 设定值
    public static readonly DependencyProperty SetVoltageProperty =
        DependencyProperty.Register(nameof(SetVoltage), typeof(double), typeof(GcRectifier),
            new PropertyMetadata(220.0));

    public static readonly DependencyProperty SetCurrentProperty =
        DependencyProperty.Register(nameof(SetCurrent), typeof(double), typeof(GcRectifier),
            new PropertyMetadata(10.0));

    // 当前值
    public static readonly DependencyProperty CurrentVoltageProperty =
        DependencyProperty.Register(nameof(CurrentVoltage), typeof(double), typeof(GcRectifier),
            new PropertyMetadata(0.0));

    public static readonly DependencyProperty CurrentCurrentProperty =
        DependencyProperty.Register(nameof(CurrentCurrent), typeof(double), typeof(GcRectifier),
            new PropertyMetadata(0.0));

    // 报警上下限
    public static readonly DependencyProperty AlarmHighVoltageProperty =
        DependencyProperty.Register(nameof(AlarmHighVoltage), typeof(double), typeof(GcRectifier),
            new PropertyMetadata(240.0));

    public static readonly DependencyProperty AlarmLowVoltageProperty =
        DependencyProperty.Register(nameof(AlarmLowVoltage), typeof(double), typeof(GcRectifier),
            new PropertyMetadata(180.0));

    public static readonly DependencyProperty AlarmHighCurrentProperty =
        DependencyProperty.Register(nameof(AlarmHighCurrent), typeof(double), typeof(GcRectifier),
            new PropertyMetadata(15.0));

    public static readonly DependencyProperty AlarmLowCurrentProperty =
        DependencyProperty.Register(nameof(AlarmLowCurrent), typeof(double), typeof(GcRectifier),
            new PropertyMetadata(5.0));

    // 自动报警（根据当前值是否越限自动计算）
    public static readonly DependencyProperty IsAlarmProperty =
        DependencyProperty.Register(nameof(IsAlarm), typeof(bool), typeof(GcRectifier),
            new PropertyMetadata(false, OnIsAlarmChanged));

    #endregion

    #region 属性封装

    /// <summary>整流器电源开关。</summary>
    public bool IsOn
    {
        get => (bool)GetValue(IsOnProperty);
        set => SetValue(IsOnProperty, value);
    }

    /// <summary>设定电压（V）。</summary>
    public double SetVoltage
    {
        get => (double)GetValue(SetVoltageProperty);
        set => SetValue(SetVoltageProperty, value);
    }

    /// <summary>设定电流（A）。</summary>
    public double SetCurrent
    {
        get => (double)GetValue(SetCurrentProperty);
        set => SetValue(SetCurrentProperty, value);
    }

    /// <summary>当前电压（V）。</summary>
    public double CurrentVoltage
    {
        get => (double)GetValue(CurrentVoltageProperty);
        set => SetValue(CurrentVoltageProperty, value);
    }

    /// <summary>当前电流（A）。</summary>
    public double CurrentCurrent
    {
        get => (double)GetValue(CurrentCurrentProperty);
        set => SetValue(CurrentCurrentProperty, value);
    }

    /// <summary>电压报警上限（V）。</summary>
    public double AlarmHighVoltage
    {
        get => (double)GetValue(AlarmHighVoltageProperty);
        set => SetValue(AlarmHighVoltageProperty, value);
    }

    /// <summary>电压报警下限（V）。</summary>
    public double AlarmLowVoltage
    {
        get => (double)GetValue(AlarmLowVoltageProperty);
        set => SetValue(AlarmLowVoltageProperty, value);
    }

    /// <summary>电流报警上限（A）。</summary>
    public double AlarmHighCurrent
    {
        get => (double)GetValue(AlarmHighCurrentProperty);
        set => SetValue(AlarmHighCurrentProperty, value);
    }

    /// <summary>电流报警下限（A）。</summary>
    public double AlarmLowCurrent
    {
        get => (double)GetValue(AlarmLowCurrentProperty);
        set => SetValue(AlarmLowCurrentProperty, value);
    }

    /// <summary>是否越限报警（当前值超出上下限自动置位）。</summary>
    public bool IsAlarm
    {
        get => (bool)GetValue(IsAlarmProperty);
        set => SetValue(IsAlarmProperty, value);
    }

    #endregion

    private static void OnIsAlarmChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // 无额外逻辑；模板通过触发器响应 IsAlarm。
    }
}
