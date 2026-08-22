using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CustomControl.Controls;

/// <summary>
/// 毛玻璃页面：透明背景，让窗体的毛玻璃效果透出；
/// 可选 <see cref="GlassBackdrop"/> 模拟整页玻璃面板效果。
/// </summary>
public class GlassPage : Page
{
    static GlassPage()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassPage), new FrameworkPropertyMetadata(typeof(GlassPage)));
    }

    public GlassPage()
    {
        Background = Brushes.Transparent;
    }

    public static readonly DependencyProperty GlassPanelProperty =
        DependencyProperty.Register(nameof(GlassPanel), typeof(bool), typeof(GlassPage),
            new PropertyMetadata(false, OnGlassPanelChanged));

    /// <summary>是否将页面内容包裹在半透明玻璃面板中（便于直接看到玻璃质感）。</summary>
    public bool GlassPanel
    {
        get => (bool)GetValue(GlassPanelProperty);
        set => SetValue(GlassPanelProperty, value);
    }

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(GlassPage),
            new PropertyMetadata(new CornerRadius(12)));

    /// <summary>玻璃面板的圆角半径。</summary>
    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    private static void OnGlassPanelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GlassPage page && (bool)e.NewValue)
        {
            page.ApplyPanel();
        }
    }

    private void ApplyPanel()
    {
        var panel = new GlassPanel
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
        if (element is FrameworkElement { Parent: Panel parent })
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
