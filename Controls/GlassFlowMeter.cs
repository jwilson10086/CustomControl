using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CustomControl.Controls;

/// <summary>
/// 工业流量计控件（Keyence 风格数显面板）。
///
/// 【符号语义】
/// 数显面板：顶部 FLOW 标签 + RUN 状态灯（IsFlowing 时呼吸闪烁）；
/// 中部大号读数 + 单位；底部量程条按 Value/100 填充。
///
/// 【实现要点】
/// 量程条 PART_Bar 的 RenderTransform 必须是 ScaleTransform（原点在左缘），
/// 代码后置只改 ScaleX。模板密封会把 Freezable 冻结为只读，
/// 修改前需 Clone 出可变副本。
/// </summary>
public class GlassFlowMeter : Control
{
    static GlassFlowMeter()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassFlowMeter), new FrameworkPropertyMetadata(typeof(GlassFlowMeter)));
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(GlassFlowMeter),
            new PropertyMetadata(0.0, OnValueChanged));

    public static readonly DependencyProperty UnitProperty =
        DependencyProperty.Register(nameof(Unit), typeof(string), typeof(GlassFlowMeter),
            new PropertyMetadata("L/min"));

    public static readonly DependencyProperty IsFlowingProperty =
        DependencyProperty.Register(nameof(IsFlowing), typeof(bool), typeof(GlassFlowMeter),
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

    private const string PartBarName = "PART_Bar";
    private const string PartTitleTextName = "PART_TitleText";
    private const string PartValueTextName = "PART_ValueText";
    private const string PartUnitTextName = "PART_UnitText";

    private FrameworkElement? _bar;
    private TextBlock? _titleText;
    private TextBlock? _valueText;
    private TextBlock? _unitText;

    public GlassFlowMeter()
    {
        SizeChanged += (_, _) => UpdateFontScale();
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _bar = GetTemplateChild(PartBarName) as FrameworkElement;
        _titleText = GetTemplateChild(PartTitleTextName) as TextBlock;
        _valueText = GetTemplateChild(PartValueTextName) as TextBlock;
        _unitText = GetTemplateChild(PartUnitTextName) as TextBlock;
        UpdateBar();
        UpdateFontScale();
    }

    /// <summary>按控件实际高度等比设置三处字号（基准 56 高：24/9/10）。</summary>
    private void UpdateFontScale()
    {
        var h = ActualHeight;
        if (h <= 0)
        {
            return;
        }

        if (_valueText != null)
        {
            _valueText.FontSize = Math.Max(12, h * 0.43);
        }

        if (_titleText != null)
        {
            _titleText.FontSize = Math.Max(8, h * 0.16);
        }

        if (_unitText != null)
        {
            _unitText.FontSize = Math.Max(9, h * 0.18);
        }
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((GlassFlowMeter)d).UpdateBar();

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
