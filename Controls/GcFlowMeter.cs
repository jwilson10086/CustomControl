using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GeneralControl.Controls;

/// <summary>
/// 工业流量计控件（Keyence 风格数显面板）。
///
/// 【符号语义】
/// 数显面板：顶部 FLOW 标签 + RUN 状态灯（IsFlowing 时呼吸闪烁）；
/// 中部大号读数 + 单位；底部量程条按 Value/100 填充。
/// 可选底部辅助行：设定流量(SP) + 报警百分比(AL)，可用变量控制显隐。
///
/// 【实现要点】
/// 量程条 PART_Bar 的 RenderTransform 必须是 ScaleTransform（原点在左缘），
/// 代码后置只改 ScaleX。模板密封会把 Freezable 冻结为只读，
/// 修改前需 Clone 出可变副本。
/// </summary>
public class GcFlowMeter : Control
{
    static GcFlowMeter()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcFlowMeter), new FrameworkPropertyMetadata(typeof(GcFlowMeter)));
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(GcFlowMeter),
            new PropertyMetadata(0.0, OnValueChanged));

    public static readonly DependencyProperty UnitProperty =
        DependencyProperty.Register(nameof(Unit), typeof(string), typeof(GcFlowMeter),
            new PropertyMetadata("L/min"));

    public static readonly DependencyProperty IsFlowingProperty =
        DependencyProperty.Register(nameof(IsFlowing), typeof(bool), typeof(GcFlowMeter),
            new PropertyMetadata(false));

    // ---- 新增：设定流量 / 报警百分比 ----

    public static readonly DependencyProperty SetpointProperty =
        DependencyProperty.Register(nameof(Setpoint), typeof(double), typeof(GcFlowMeter),
            new PropertyMetadata(80.0));

    public static readonly DependencyProperty AlarmPercentProperty =
        DependencyProperty.Register(nameof(AlarmPercent), typeof(double), typeof(GcFlowMeter),
            new PropertyMetadata(90.0, OnValueChanged));

    public static readonly DependencyProperty ShowSetpointProperty =
        DependencyProperty.Register(nameof(ShowSetpoint), typeof(bool), typeof(GcFlowMeter),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ShowAlarmProperty =
        DependencyProperty.Register(nameof(ShowAlarm), typeof(bool), typeof(GcFlowMeter),
            new PropertyMetadata(false, OnValueChanged));

    public static readonly DependencyProperty IsAlarmProperty =
        DependencyProperty.Register(nameof(IsAlarm), typeof(bool), typeof(GcFlowMeter),
            new PropertyMetadata(false));

    /// <summary>瞬时流量（0~100，按满量程百分比，同时驱动量程条）。</summary>
    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>读数单位文案。</summary>
    public string Unit
    {
        get => (string)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    /// <summary>介质流动中（RUN 灯闪烁）。</summary>
    public bool IsFlowing
    {
        get => (bool)GetValue(IsFlowingProperty);
        set => SetValue(IsFlowingProperty, value);
    }

    /// <summary>设定流量（满量程百分比显示）。</summary>
    public double Setpoint
    {
        get => (double)GetValue(SetpointProperty);
        set => SetValue(SetpointProperty, value);
    }

    /// <summary>报警百分比（0~100，超过即报警）。</summary>
    public double AlarmPercent
    {
        get => (double)GetValue(AlarmPercentProperty);
        set => SetValue(AlarmPercentProperty, value);
    }

    /// <summary>是否显示设定流量。</summary>
    public bool ShowSetpoint
    {
        get => (bool)GetValue(ShowSetpointProperty);
        set => SetValue(ShowSetpointProperty, value);
    }

    /// <summary>是否显示报警百分比。</summary>
    public bool ShowAlarm
    {
        get => (bool)GetValue(ShowAlarmProperty);
        set => SetValue(ShowAlarmProperty, value);
    }

    /// <summary>是否越限报警（Value 超过 AlarmPercent 自动置位）。</summary>
    public bool IsAlarm
    {
        get => (bool)GetValue(IsAlarmProperty);
        private set => SetValue(IsAlarmProperty, value);
    }

    private const string PartBarName = "PART_Bar";
    private const string PartTitleTextName = "PART_TitleText";
    private const string PartValueTextName = "PART_ValueText";
    private const string PartUnitTextName = "PART_UnitText";

    private FrameworkElement? _bar;
    private System.Windows.Controls.TextBlock? _titleText;
    private System.Windows.Controls.TextBlock? _valueText;
    private System.Windows.Controls.TextBlock? _unitText;

    public GcFlowMeter()
    {
        SizeChanged += (_, _) => UpdateFontScale();
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _bar = GetTemplateChild(PartBarName) as FrameworkElement;
        _titleText = GetTemplateChild(PartTitleTextName) as System.Windows.Controls.TextBlock;
        _valueText = GetTemplateChild(PartValueTextName) as System.Windows.Controls.TextBlock;
        _unitText = GetTemplateChild(PartUnitTextName) as System.Windows.Controls.TextBlock;
        UpdateBar();
        UpdateFontScale();
    }

    /// <summary>
    /// 按控件实际尺寸等比设置三处字号。
    /// 基准 112x56：数值 21 / 标题 9 / 单位 10；
    /// 取高度与宽度两个方向缩放系数的较小值，
    /// 避免横向被压窄时大号读数+单位超出面板显示不全。
    /// </summary>
    private void UpdateFontScale()
    {
        var h = ActualHeight;
        var w = ActualWidth;
        if (h <= 0 || w <= 0)
        {
            return;
        }

        var k = Math.Min(h / 56.0, w / 112.0);

        if (_valueText != null)
        {
            _valueText.FontSize = Math.Max(12, 21 * k);
        }

        if (_titleText != null)
        {
            _titleText.FontSize = Math.Max(8, 9 * k);
        }

        if (_unitText != null)
        {
            _unitText.FontSize = Math.Max(9, 10 * k);
        }
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var meter = (GcFlowMeter)d;
        meter.UpdateBar();
        meter.UpdateAlarm();
    }

    /// <summary>按 Value 更新量程条填充比例，并评估报警。</summary>
    private void UpdateAlarm()
    {
        IsAlarm = ShowAlarm && Value > AlarmPercent;
    }

    /// <summary>按 Value 更新量程条填充比例。</summary>
    private void UpdateBar()
    {
        if (_bar?.RenderTransform is not ScaleTransform st)
        {
            return;
        }

        if (st.IsFrozen)
        {
            // 模板密封后其中的 Freezable 会变成只读，替换为可变克隆再修改
            st = st.Clone();
            _bar.RenderTransform = st;
        }

        var frac = Math.Clamp(Value, 0, 100) / 100.0;
        st.ScaleX = 0.02 + frac * 0.98;
    }
}
