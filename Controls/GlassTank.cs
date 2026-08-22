using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CustomControl.Controls;

/// <summary>
/// 工业槽体 / 液位罐控件。
///
/// 【结构】
/// 玻璃质感罐体 + 底部锚定的液位填充 + 居中百分比文字。
///
/// 【液位实现】
/// 液位块高度 = 可用高度 × Level/100。
/// 因为纯 XAML 无法做"百分比乘法"，由代码后置在
/// Level 变化与尺寸变化（SizeChanged）时同步 PART_Liquid 的高度——
/// 这也是模板部件（TemplatePart）约定的典型用法。
/// </summary>
[TemplatePart(Name = PartLiquid, Type = typeof(Border))]
[TemplatePart(Name = PartPercent, Type = typeof(TextBlock))]
public class GlassTank : Control
{
    private const string PartLiquid = "PART_Liquid";
    private const string PartPercent = "PART_Percent";

    static GlassTank()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassTank), new FrameworkPropertyMetadata(typeof(GlassTank)));
    }

    public GlassTank()
    {
        // 控件尺寸变化（含首次布局）时重算液位高度。
        // 在构造函数挂接保证只订阅一次（OnApplyTemplate 可能被多次调用）。
        SizeChanged += (_, _) => UpdateLevel();
    }

    public static readonly DependencyProperty LevelProperty =
        DependencyProperty.Register(nameof(Level), typeof(double), typeof(GlassTank),
            new PropertyMetadata(0.0, OnLevelChanged));

    public static readonly DependencyProperty ShowPercentProperty =
        DependencyProperty.Register(nameof(ShowPercent), typeof(bool), typeof(GlassTank),
            new PropertyMetadata(true, OnLevelChanged));

    public static readonly DependencyProperty LiquidBrushProperty =
        DependencyProperty.Register(nameof(LiquidBrush), typeof(Brush), typeof(GlassTank),
            new PropertyMetadata(default(Brush)));

    /// <summary>液位 0~100（自动夹取）。</summary>
    public double Level
    {
        get => (double)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    /// <summary>是否显示百分比文字。</summary>
    public bool ShowPercent
    {
        get => (bool)GetValue(ShowPercentProperty);
        set => SetValue(ShowPercentProperty, value);
    }

    /// <summary>液体填充画刷。默认 null 时使用模板内置的强调色渐变。</summary>
    public Brush? LiquidBrush
    {
        get => (Brush?)GetValue(LiquidBrushProperty);
        set => SetValue(LiquidBrushProperty, value);
    }

    // ==================== 模板部件 ====================

    private Border? _liquid;
    private TextBlock? _percent;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _liquid = GetTemplateChild(PartLiquid) as Border;
        _percent = GetTemplateChild(PartPercent) as TextBlock;

        UpdateLevel();
    }

    private static void OnLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((GlassTank)d).UpdateLevel();

    /// <summary>按比例换算液位高度并刷新百分比文字。</summary>
    private void UpdateLevel()
    {
        var level = Math.Clamp(Level, 0, 100);

        if (_liquid != null && _liquid.Parent is FrameworkElement parent)
        {
            // 罐体内区高度（父级 Grid 填满罐体内部），上下各留 1px 防止溢出圆角
            var available = Math.Max(0, parent.ActualHeight - 2);
            _liquid.Height = available * level / 100;
        }

        if (_percent != null)
        {
            _percent.Text = $"{level:0}%";
        }
    }
}
