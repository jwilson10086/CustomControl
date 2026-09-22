using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace GeneralControl.Controls;

/// <summary>
/// 毛玻璃标签（Chip）：展示分类/状态信息，可选关闭按钮。
///
/// 【设计要点】
/// - 继承 ContentControl，内容可以是文字、图标或任意元素；
/// - IsClosable=true 时模板中的 PART_CloseButton 可见；
/// - 点击关闭按钮：自动隐藏自身（Visibility=Collapsed）并冒泡 Closed 路由事件，
///   便于父容器（如 WrapPanel）同步移除数据项。
///
/// 【为什么用路由事件而不是 CLR 事件？】
/// 路由事件可以在 XAML 中直接挂处理器（&lt;controls:Tag Closed="OnClosed"/&gt;），
/// 且支持冒泡，父级容器能统一监听所有子标签的关闭动作。
/// </summary>
public class GcTag : ContentControl
{
    static GcTag()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcTag), new FrameworkPropertyMetadata(typeof(GcTag)));
    }

    // ==================== IsClosable ====================

    public static readonly DependencyProperty IsClosableProperty =
        DependencyProperty.Register(nameof(IsClosable), typeof(bool), typeof(GcTag),
            new PropertyMetadata(false));

    /// <summary>是否显示关闭按钮。</summary>
    public bool IsClosable
    {
        get => (bool)GetValue(IsClosableProperty);
        set => SetValue(IsClosableProperty, value);
    }

    // ==================== Closed 路由事件 ====================

    public static readonly RoutedEvent ClosedEvent =
        EventManager.RegisterRoutedEvent(nameof(Closed), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(GcTag));

    /// <summary>标签被关闭后触发（此时自身已 Collapsed）。</summary>
    public event RoutedEventHandler Closed
    {
        add => AddHandler(ClosedEvent, value);
        remove => RemoveHandler(ClosedEvent, value);
    }

    // ==================== 模板部件 ====================

    private ButtonBase? _closeButton;

    /// <summary>重新显示被关闭的标签（演示与复用场景使用）。</summary>
    public void Reset() => Visibility = Visibility.Visible;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_closeButton != null)
        {
            _closeButton.Click -= OnCloseClick;
        }

        _closeButton = GetTemplateChild("PART_CloseButton") as ButtonBase;

        if (_closeButton != null)
        {
            _closeButton.Click += OnCloseClick;
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Visibility = Visibility.Collapsed;
        RaiseEvent(new RoutedEventArgs(ClosedEvent, this));
    }
}






