using System.Windows;
using System.Windows.Controls;

namespace GeneralControl.Controls;

/// <summary>
/// 毛玻璃面板：作为页面/容器背景使用，半透明玻璃底并保留窗口背景透出。
/// </summary>
public class GcPanel : ContentControl
{
    static GcPanel()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcPanel), new FrameworkPropertyMetadata(typeof(GcPanel)));
    }

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(GcPanel),
            new PropertyMetadata(new CornerRadius(16)));

    /// <summary>面板圆角半径。</summary>
    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }
}



