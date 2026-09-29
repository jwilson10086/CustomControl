using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GeneralControl.Controls;

/// <summary>
/// 阀岛中的单个工位（电磁阀 + 阀体 + 状态灯）。
/// 一般由 <see cref="GcValveIsland" /> 自动创建并托管，不建议单独使用。
/// </summary>
public class GcValveIslandValve : Control
{
    static GcValveIslandValve()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(GcValveIslandValve),
            new FrameworkPropertyMetadata(typeof(GcValveIslandValve)));
    }

    public GcValveIslandValve()
    {
        Cursor = Cursors.Hand;
        Focusable = true;
    }

    #region 属性

    /// <summary>工位序号（从 0 开始），由阀岛写入。</summary>
    public static readonly DependencyProperty IndexProperty = DependencyProperty.Register(
        nameof(Index),
        typeof(int),
        typeof(GcValveIslandValve),
        new FrameworkPropertyMetadata(-1));

    public int Index
    {
        get => (int)GetValue(IndexProperty);
        set => SetValue(IndexProperty, value);
    }

    /// <summary>是否打开。点击工位即切换该值。</summary>
    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen),
        typeof(bool),
        typeof(GcValveIslandValve),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsOpenChanged));
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>故障。故障工位不可点击，灯与阀体转红。</summary>
    public static readonly DependencyProperty IsFaultProperty = DependencyProperty.Register(
        nameof(IsFault),
        typeof(bool),
        typeof(GcValveIslandValve),
        new FrameworkPropertyMetadata(false));

    public bool IsFault
    {
        get => (bool)GetValue(IsFaultProperty);
        set => SetValue(IsFaultProperty, value);
    }

    /// <summary>是否被阀岛选中（当前操作位）。</summary>
    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
        nameof(IsSelected),
        typeof(bool),
        typeof(GcValveIslandValve),
        new FrameworkPropertyMetadata(false));

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    #endregion

    #region 事件

    /// <summary>开关状态变化（冒泡）。</summary>
    public static readonly RoutedEvent IsOpenChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(IsOpenChanged),
        RoutingStrategy.Bubble,
        typeof(RoutedPropertyChangedEventHandler<bool>),
        typeof(GcValveIslandValve));

    public event RoutedPropertyChangedEventHandler<bool> IsOpenChanged
    {
        add => AddHandler(IsOpenChangedEvent, value);
        remove => RemoveHandler(IsOpenChangedEvent, value);
    }

    #endregion

    /// <summary>切换开关；故障工位不响应。</summary>
    public void Toggle()
    {
        if (IsFault || !IsEnabled)
        {
            return;
        }

        IsOpen = !IsOpen;
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GcValveIslandValve valve)
        {
            valve.RaiseEvent(new RoutedPropertyChangedEventArgs<bool>(
                (bool)e.OldValue,
                valve.IsOpen,
                IsOpenChangedEvent));
        }
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        Focus();
        Toggle();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Space || e.Key == Key.Enter)
        {
            Toggle();
            e.Handled = true;
        }
    }
}
