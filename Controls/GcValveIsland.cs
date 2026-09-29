using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace GeneralControl.Controls;

/// <summary>阀岛排列方向。</summary>
public enum GcValveIslandOrientation
{
    /// <summary>横向（每行一个，默认）。</summary>
    Horizontal = 0,

    /// <summary>纵向（每列一个）。</summary>
    Vertical = 1,
}

/// <summary>阀岛切换事件处理器。</summary>
public delegate void ValveToggledEventHandler(object sender, GcValveToggledEventArgs e);

/// <summary>
/// 阀岛切换事件参数。
/// </summary>
public class GcValveToggledEventArgs : RoutedEventArgs
{
    public GcValveToggledEventArgs(RoutedEvent routedEvent, object source, int index, bool newState)
        : base(routedEvent, source)
    {
        Index = index;
        NewState = newState;
    }

    /// <summary>被切换的工位序号。</summary>
    public int Index { get; }

    /// <summary>切换后的状态：true 打开。</summary>
    public bool NewState { get; }
}

/// <summary>
/// 阀岛：一组可点击切换的电磁阀工位。
/// <para>阀门数量由 <see cref="ValveCount" /> 决定（1~64），工位自动生成，
/// 开关状态保存在 <see cref="States" /> 中；点击或聚焦后按空格即可切换。</para>
/// </summary>
[TemplatePart(Name = RowPartName, Type = typeof(UniformGrid))]
public class GcValveIsland : Control
{
    /// <summary>工位容器部件名。</summary>
    public const string RowPartName = "PART_ValveRow";

    /// <summary>允许的最大工位数。</summary>
    public const int MaxValveCount = 64;

    private const int MinValveCount = 1;

    private static readonly DependencyPropertyKey StatesPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(States),
        typeof(ObservableCollection<bool>),
        typeof(GcValveIsland),
        new FrameworkPropertyMetadata(null));

    private readonly ObservableCollection<bool> _states = new();

    private UniformGrid? _row;

    private readonly List<GcValveIslandValve> _valves = new();

    static GcValveIsland()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(GcValveIsland),
            new FrameworkPropertyMetadata(typeof(GcValveIsland)));
    }

    public GcValveIsland()
    {
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
    }

    #region 属性

    /// <summary>所有工位的开关状态，索引与工位序号一致。只读集合，内容可双向绑定。</summary>
    public static readonly DependencyProperty StatesProperty = StatesPropertyKey.DependencyProperty;

    public ObservableCollection<bool> States => _states;

    /// <summary>工位数量，取值 1~64。</summary>
    public static readonly DependencyProperty ValveCountProperty = DependencyProperty.Register(
        nameof(ValveCount),
        typeof(int),
        typeof(GcValveIsland),
        new FrameworkPropertyMetadata(4, OnValveCountChanged, CoerceValveCount));

    public int ValveCount
    {
        get => (int)GetValue(ValveCountProperty);
        set => SetValue(ValveCountProperty, value);
    }

    /// <summary>排列方向。</summary>
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation),
        typeof(GcValveIslandOrientation),
        typeof(GcValveIsland),
        new FrameworkPropertyMetadata(GcValveIslandOrientation.Horizontal, OnOrientationChanged));

    public GcValveIslandOrientation Orientation
    {
        get => (GcValveIslandOrientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>当前选中的工位序号，-1 表示未选中。</summary>
    public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(
        nameof(SelectedIndex),
        typeof(int),
        typeof(GcValveIsland),
        new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedIndexChanged));

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    #endregion

    #region 事件

    /// <summary>某个工位被点击切换后触发。</summary>
    public static readonly RoutedEvent ValveToggledEvent = EventManager.RegisterRoutedEvent(
        nameof(ValveToggled),
        RoutingStrategy.Bubble,
        typeof(ValveToggledEventHandler),
        typeof(GcValveIsland));

    public event ValveToggledEventHandler ValveToggled
    {
        add => AddHandler(ValveToggledEvent, value);
        remove => RemoveHandler(ValveToggledEvent, value);
    }

    #endregion

    /// <summary>代码切换指定工位。</summary>
    public void Toggle(int index) => SetOpen(index, !GetOpen(index));

    /// <summary>代码设置指定工位状态。</summary>
    public void SetOpen(int index, bool open)
    {
        if (index < 0 || index >= ValveCount)
        {
            return;
        }

        if (_states[index] != open)
        {
            _states[index] = open;
        }

        if (index < _valves.Count)
        {
            _valves[index].IsOpen = open;
        }
    }

    /// <summary>读取指定工位状态。</summary>
    public bool GetOpen(int index) => index >= 0 && index < _states.Count && _states[index];

    /// <summary>标记某个工位故障。</summary>
    public void SetFault(int index, bool fault)
    {
        if (index >= 0 && index < _valves.Count)
        {
            _valves[index].IsFault = fault;
        }
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _row = GetTemplateChild(RowPartName) as UniformGrid;
        Rebuild();
    }

    private static object CoerceValveCount(DependencyObject d, object baseValue)
    {
        var count = (int)baseValue;
        return Math.Min(MaxValveCount, Math.Max(MinValveCount, count));
    }

    private static void OnValveCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var island = (GcValveIsland)d;
        island.SyncStates();
        island.Rebuild();
    }

    private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var island = (GcValveIsland)d;
        island.ApplyOrientation();
    }

    private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var island = (GcValveIsland)d;
        var index = (int)e.NewValue;
        for (var i = 0; i < island._valves.Count; i++)
        {
            island._valves[i].IsSelected = i == index;
        }
    }

    /// <summary>按 <see cref="ValveCount" /> 增删状态位，保留已有状态。</summary>
    private void SyncStates()
    {
        while (_states.Count > ValveCount)
        {
            _states.RemoveAt(_states.Count - 1);
        }

        while (_states.Count < ValveCount)
        {
            _states.Add(false);
        }
    }

    private void ApplyOrientation()
    {
        if (_row is null)
        {
            return;
        }

        // 行列都显式给值，不留 0 让 UniformGrid 自行推算，避免数量变化后布局不刷新
        if (Orientation == GcValveIslandOrientation.Vertical)
        {
            _row.Rows = Math.Max(1, ValveCount);
            _row.Columns = 1;
        }
        else
        {
            _row.Rows = 1;
            _row.Columns = Math.Max(1, ValveCount);
        }
    }

    private void Rebuild()
    {
        if (_row is null)
        {
            return;
        }

        foreach (var valve in _valves)
        {
            valve.IsOpenChanged -= OnValveIsOpenChanged;
            _row.Children.Remove(valve);
        }

        _valves.Clear();
        ApplyOrientation();

        for (var i = 0; i < ValveCount; i++)
        {
            var valve = new GcValveIslandValve
            {
                Index = i,
                IsOpen = GetOpen(i),
                IsSelected = i == SelectedIndex,
            };

            valve.IsOpenChanged += OnValveIsOpenChanged;
            _row.Children.Add(valve);
            _valves.Add(valve);
        }
    }

    private void OnValveIsOpenChanged(object sender, RoutedPropertyChangedEventArgs<bool> e)
    {
        if (sender is not GcValveIslandValve valve)
        {
            return;
        }

        var index = valve.Index;
        if (index < 0)
        {
            return;
        }

        if (index < _states.Count)
        {
            _states[index] = e.NewValue;
        }

        SelectedIndex = index;
        RaiseEvent(new GcValveToggledEventArgs(ValveToggledEvent, this, index, e.NewValue));
    }
}
