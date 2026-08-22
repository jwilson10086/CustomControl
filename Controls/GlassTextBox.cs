using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CustomControl.Controls;

/// <summary>
/// 毛玻璃文本输入框，支持自定义占位提示（Placeholder）。
///
/// 【为什么 TextBox 需要自定义 Placeholder？】
/// WPF 原生 TextBox 没有内置的 Placeholder 属性。
/// 为了实现类似 HTML 中 placeholder="请输入..." 的效果，
/// 需要自行添加依赖属性，并在控件模板中通过 Trigger 绑定显示。
///
/// 【依赖属性 vs CLR 属性】
/// 依赖属性（DependencyProperty）是 WPF 属性系统的核心机制，
/// 支持数据绑定、动画、样式继承、值继承等功能。
/// CLR 包装属性只是对 GetValue/SetValue 的简写。
/// </summary>
public class GlassTextBox : TextBox
{
    static GlassTextBox()
    {
        // 注册毛玻璃主题样式。
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassTextBox), new FrameworkPropertyMetadata(typeof(GlassTextBox)));
    }

    /// <summary>
    /// 占位提示文本的依赖属性定义。
    ///
    /// 【Register 参数说明】
    /// - nameof(Placeholder)：属性名称字符串，用于 XAML 绑定和样式 TargetType 查找。
    /// - typeof(string)：属性值类型。
    /// - typeof(GlassTextBox)：拥有者类型（即注册在哪个类上）。
    /// - new PropertyMetadata(string.Empty)：默认值为空字符串。
    ///   这里用 string.Empty 而非 null，是为了避免控件模板中的 Trigger
    ///   在读取 Placeholder 时抛出 NullReferenceException。
    /// </summary>
    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(GlassTextBox),
            new PropertyMetadata(string.Empty));

    /// <summary>占位提示文本（输入为空时显示）。</summary>
    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>
    /// 占位提示文本颜色的依赖属性。
    ///
    /// 【为什么用 Brush 而不是 Color？】
    /// WPF 的填充/边框属性统一使用 Brush 类型，
    /// 这样可以在 XAML 中方便地使用 LinearGradientBrush、
    /// SolidColorBrush、VisualBrush 等各种画刷。
    /// 这里默认使用 120/255 透明度的白色，呈现柔和的半透明效果。
    /// </summary>
    public static readonly DependencyProperty PlaceholderBrushProperty =
        DependencyProperty.Register(nameof(PlaceholderBrush), typeof(Brush), typeof(GlassTextBox),
            new PropertyMetadata(new SolidColorBrush(Color.FromArgb(120, 255, 255, 255))));

    /// <summary>占位提示文本颜色。</summary>
    public Brush PlaceholderBrush
    {
        get => (Brush)GetValue(PlaceholderBrushProperty);
        set => SetValue(PlaceholderBrushProperty, value);
    }
}
