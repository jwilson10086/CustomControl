using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CustomControl.Controls;

/// <summary>温控状态：当前温度与设定值的偏差方向。</summary>
public enum GlassThermoMode
{
    /// <summary>恒温（偏差 &le; 0.5°）。</summary>
    Idle,

    /// <summary>低于设定值，加热中。</summary>
    Heating,

    /// <summary>高于设定值，冷却中。</summary>
    Cooling
}

/// <summary>
/// 工业温控器控件（显示面板符号）。
///
/// 【符号语义】
/// 左侧温度计：液柱高度 = (Temperature - RangeMin) / (RangeMax - RangeMin)；
/// 右侧面板：当前温度大字 + SET 设定值 + 状态点（橙=加热 / 蓝=冷却 / 灰=恒温）。
///
/// 【实现要点】
/// 模板里 PART_Mercury 的 RenderTransform 必须是 ScaleTransform，
/// 代码后置只改其 ScaleY（原点在液柱底部），避免绑定乘法转换器。
/// </summary>
public class GlassThermoController : Control
{
    static GlassThermoController()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassThermoController), new FrameworkPropertyMetadata(typeof(GlassThermoController)));
    }

    public static readonly DependencyProperty TemperatureProperty =
        DependencyProperty.Register(nameof(Temperature), typeof(double), typeof(GlassThermoController),
            new PropertyMetadata(25.0, OnMetricsChanged));

    public static readonly DependencyProperty SetPointProperty =
        DependencyProperty.Register(nameof(SetPoint), typeof(double), typeof(GlassThermoController),
            new PropertyMetadata(40.0, OnMetricsChanged));

    public static readonly DependencyProperty RangeMinProperty =
        DependencyProperty.Register(nameof(RangeMin), typeof(double), typeof(GlassThermoController),
            new PropertyMetadata(0.0, OnMetricsChanged));

    public static readonly DependencyProperty RangeMaxProperty =
        DependencyProperty.Register(nameof(RangeMax), typeof(double), typeof(GlassThermoController),
            new PropertyMetadata(100.0, OnMetricsChanged));

    public static readonly DependencyProperty ModeProperty =
        DependencyProperty.Register(nameof(Mode), typeof(GlassThermoMode), typeof(GlassThermoController),
            new PropertyMetadata(GlassThermoMode.Idle));

    /// <summary>当前温度。</summary>
    public double Temperature
    {
        get => (double)GetValue(TemperatureProperty);
        set => SetValue(TemperatureProperty, value);
    }

    /// <summary>设定温度。</summary>
    public double SetPoint
    {
        get => (double)GetValue(SetPointProperty);
        set => SetValue(SetPointProperty, value);
    }

    /// <summary>量程下限（液柱为 0）。</summary>
    public double RangeMin
    {
        get => (double)GetValue(RangeMinProperty);
        set => SetValue(RangeMinProperty, value);
    }

    /// <summary>量程上限（液柱满）。</summary>
    public double RangeMax
    {
        get => (double)GetValue(RangeMaxProperty);
        set => SetValue(RangeMaxProperty, value);
    }

    /// <summary>温控状态（内部按偏差推导，供模板 Trigger 上色）。</summary>
    public GlassThermoMode Mode
    {
        get => (GlassThermoMode)GetValue(ModeProperty);
        private set => SetValue(ModeProperty, value);
    }

    private const string PartMercuryName = "PART_Mercury";

    private FrameworkElement? _mercury;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _mercury = GetTemplateChild(PartMercuryName) as FrameworkElement;
        UpdateVisuals();
    }

    private static void OnMetricsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((GlassThermoController)d).UpdateVisuals();

    /// <summary>根据温度/设定值刷新状态模式与液柱高度。</summary>
    private void UpdateVisuals()
    {
        var delta = Temperature - SetPoint;
        Mode = delta < -0.5 ? GlassThermoMode.Heating
             : delta > 0.5 ? GlassThermoMode.Cooling
             : GlassThermoMode.Idle;

        if (_mercury?.RenderTransform is not ScaleTransform st)
        {
            return;
        }

        if (st.IsFrozen)
        {
            // 模板密封后其中的 Freezable 会变成只读，替换为可变克隆再修改
            st = st.Clone();
            _mercury.RenderTransform = st;
        }

        var span = Math.Max(1e-6, RangeMax - RangeMin);
        var level = System.Math.Clamp((Temperature - RangeMin) / span, 0, 1);
        st.ScaleY = 0.06 + level * 0.94;
    }
}
