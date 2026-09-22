using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GeneralControl.Controls;

/// <summary>
/// 毛玻璃页面：透明背景，让窗体的毛玻璃效果透出；
/// 可选 GcBackdropMode 模拟整页玻璃面板效果。
/// </summary>
public class GcPage : System.Windows.Controls.Page
{
    static GcPage()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcPage), new FrameworkPropertyMetadata(typeof(GcPage)));
    }

    public GcPage()
    {
        Background = Brushes.Transparent;
    }

    public static readonly DependencyProperty PanelProperty =
        DependencyProperty.Register("Panel", typeof(bool), typeof(GcPage),
            new PropertyMetadata(false, OnPanelChanged));

    /// <summary>是否将页面内容包裹在半透明玻璃面板中（否则直接承载内容元素）。</summary>
    public bool Panel
    {
        get => (bool)GetValue(PanelProperty);
        set => SetValue(PanelProperty, value);
    }

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(GcPage),
            new PropertyMetadata(new CornerRadius(12)));

    /// <summary>玻璃面板的圆角半径。</summary>
    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    private static void OnPanelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GcPage page && (bool)e.NewValue)
        {
            page.ApplyPanel();
        }
    }

    private void ApplyPanel()
    {
        var panel = new GcPanel
        {
            CornerRadius = CornerRadius,
            Margin = new Thickness(12),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        if (Content is UIElement element && !ReferenceEquals(element, panel))
        {
            element = DetachFromParent(element);
            panel.Content = element;
            Content = panel;
        }
    }

    private static UIElement DetachFromParent(UIElement element)
    {
        if (element is FrameworkElement { Parent: System.Windows.Controls.Panel parent })
        {
            parent.Children.Remove(element);
        }
        else if (element is FrameworkElement { Parent: ContentControl contentParent })
        {
            contentParent.Content = null;
        }

        return element;
    }
}









