using System.Windows;
using System.Windows.Controls;

namespace CustomControl.Controls;

/// <summary>
/// 毛玻璃面板：作为页面/容器背景使用，半透明玻璃底并保留窗口背景透出。
/// </summary>
public class GlassPanel : ContentControl
{
    static GlassPanel()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassPanel), new FrameworkPropertyMetadata(typeof(GlassPanel)));
    }

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(GlassPanel),
            new PropertyMetadata(new CornerRadius(16)));

    /// <summary>面板圆角半径。</summary>
    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }
}
