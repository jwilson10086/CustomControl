using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace CustomControl.Controls;

/// <summary>
/// 毛玻璃对话框：列表选择式，含标题、列表、确认/取消按钮。
///
/// 【设计思路】
/// 这是一个基于 ContentControl 的自定义控件，遵循 WPF 自定义控件的开发范式：
/// 1. 继承 ContentControl（而非 UserControl）—— ContentControl 更轻量，UI 完全由模板（Template）定义
/// 2. 使用 DefaultStyleKey —— 让 WPF 在查找样式时能找到对应的 Style
/// 3. 使用 PART_ 前缀的命名部件 —— WPF 自定义控件的约定，表示"模板中的关键部件"
///
/// 【为什么不用 Window？】
/// 直接用 Window 会创建独立窗口，无法实现毛玻璃覆盖层效果。
/// 通过 PopupOverlay 包装成无边框透明窗口，可以自由控制外观。
/// </summary>
public class GlassDialog : ContentControl
{
    /// <summary>
    /// 模板中的 ListBox 引用。
    /// 通过 OnApplyTemplate 获取，因为控件模板在构造函数之后才应用。
    /// 用下划线前缀表示私有字段，? 表示可能为 null（模板应用前为 null）。
    /// </summary>
    private ListBox? _listBox;

    /// <summary>
    /// 弹出层引用，用于管理对话框的显示和关闭。
    /// </summary>
    private PopupOverlay? _overlay;

    /// <summary>
    /// 静态构造函数：重写 DefaultStyleKey 元数据。
    ///
    /// 【为什么需要这一步？】
    /// WPF 查找控件样式的规则是：使用控件的 DefaultStyleKey 来查找。
    /// 重写后，WPF 会去主题资源中查找键为 "GlassDialog" 的 Style。
    /// 如果不重写，WPF 会用 ContentControl 的默认样式，自定义外观就失效了。
    ///
    /// 【为什么用静态构造函数？】
    /// 静态构造函数只执行一次，适合做"一次性配置"。
    /// 依赖属性的 OverrideMetadata 必须在使用前完成，放在静态构造函数中最合适。
    /// </summary>
    static GlassDialog()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassDialog), new FrameworkPropertyMetadata(typeof(GlassDialog)));
    }

    /// <summary>
    /// 对话框标题，通过数据绑定显示在模板的标题区域。
    /// 默认值为"请选择"，符合列表选择场景的语义。
    /// </summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(GlassDialog), new PropertyMetadata("请选择"));

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>
    /// 对话框选项列表，绑定到 ListBox 的 ItemsSource。
    ///
    /// 【为什么用 IEnumerable&lt;string&gt;？】
    /// - 比 IList 更宽松，调用方可以传数组、List、LINQ 查询结果等任意实现了 IEnumerable 的集合
    /// - 对话框只需要读取列表，不需要修改，所以不需要 IList 的增删功能
    /// - 更好的兼容性：ObservableCollection、List&lt;string&gt;、string[] 都能用
    /// </summary>
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(nameof(Items), typeof(IEnumerable<string>), typeof(GlassDialog), new PropertyMetadata(null));

    public IEnumerable<string> Items { get => (IEnumerable<string>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>
    /// 当前选中的项。用户点击确认后，选中的值会写入此属性。
    /// 调用方可通过 TwoWay 绑定读取结果，或在 Confirmed 事件中读取。
    /// </summary>
    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(string), typeof(GlassDialog), new PropertyMetadata(null));

    public string? SelectedItem { get => (string?)GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }

    /// <summary>确认按钮的文字，默认"确认"。便于国际化或自定义显示。</summary>
    public static readonly DependencyProperty ConfirmTextProperty =
        DependencyProperty.Register(nameof(ConfirmText), typeof(string), typeof(GlassDialog), new PropertyMetadata("确认"));

    public string ConfirmText { get => (string)GetValue(ConfirmTextProperty); set => SetValue(ConfirmTextProperty, value); }

    /// <summary>取消按钮的文字，默认"取消"。</summary>
    public static readonly DependencyProperty CancelTextProperty =
        DependencyProperty.Register(nameof(CancelText), typeof(string), typeof(GlassDialog), new PropertyMetadata("取消"));

    public string CancelText { get => (string)GetValue(CancelTextProperty); set => SetValue(CancelTextProperty, value); }

    /// <summary>
    /// 控件模板应用完成后的回调。
    ///
    /// 【为什么不用构造函数获取模板子元素？】
    /// WPF 控件的模板是在控件实例化之后才应用的（通过 ApplyTemplate 方法）。
    /// 构造函数执行时，模板尚未应用，GetTemplateChild 会返回 null。
    /// OnApplyTemplate 保证模板已应用，是获取模板子元素的唯一可靠时机。
    ///
    /// 【PART_ 前缀约定】
    /// WPF 自定义控件约定：模板中的关键部件用 "PART_" 前缀命名。
    /// 这不是强制的，但遵循这个约定能让工具（如 Blend）更好地识别。
    ///
    /// 【为什么用 as + null 检查而非直接转换？】
    /// 如果模板中缺少某个部件（比如开发者修改了模板），直接转换会抛出 InvalidCastException。
    /// 用 as + null 检查更加健壮，允许模板中缺少非核心部件。
    /// </summary>
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _listBox = GetTemplateChild("PART_ListBox") as ListBox;

        if (GetTemplateChild("PART_ConfirmButton") is Button confirmBtn)
            confirmBtn.Click += OnConfirm;
        if (GetTemplateChild("PART_CancelButton") is Button cancelBtn)
            cancelBtn.Click += OnCancel;
    }

    /// <summary>
    /// 确认按钮点击事件处理。
    ///
    /// 【执行流程】
    /// 1. 从 ListBox 中获取当前选中项，赋值给 SelectedItem 属性
    /// 2. 设置 _dialogResult = true，标记用户确认了操作
    /// 3. 触发 Confirmed 路由事件，通知外部"用户点了确认"
    /// 4. 关闭对话框
    ///
    /// 【为什么先赋值再触发事件？】
    /// 确保事件处理器执行时，SelectedItem 已经是最新的值。
    /// 如果反过来，事件处理器读到的 SelectedItem 可能还是旧值。
    /// </summary>
    private void OnConfirm(object sender, RoutedEventArgs e)
    {
        if (_listBox?.SelectedItem is string item)
            SelectedItem = item;

        _dialogResult = true;
        RaiseEvent(new RoutedEventArgs(ConfirmedEvent));
        Close();
    }

    /// <summary>
    /// 取消按钮点击事件处理。
    /// 设置 _dialogResult = false，触发 Cancelled 事件，然后关闭对话框。
    /// 不修改 SelectedItem，保持原值不变。
    /// </summary>
    private void OnCancel(object sender, RoutedEventArgs e)
    {
        _dialogResult = false;
        RaiseEvent(new RoutedEventArgs(CancelledEvent));
        Close();
    }

    /// <summary>
    /// 非模态方式显示对话框。
    ///
    /// 【Show vs ShowDialog 的区别】
    /// Show：创建新窗口并调用 ShowDialog，虽然是模态窗口，
    /// 但这里的设计意图是"非阻塞"——调用方不需要返回值，只是展示选择界面。
    ///
    /// 【为什么需要 PopupOverlay？】
    /// 直接把 GlassDialog 作为 Content 放入透明窗口，
    /// PopupOverlay 负责设置半透明背景、居中布局等通用弹出层逻辑，
    /// GlassDialog 只需要关心自己的内容（标题、列表、按钮），职责清晰。
    /// </summary>
    public void Show()
    {
        _overlay = new PopupOverlay();
        _overlay.Content = this;
        _overlay.Show();
    }

    /// <summary>
    /// 模态方式显示对话框，返回对话框结果。
    ///
    /// 【返回值说明】
    /// - true：用户点击了确认按钮
    /// - false：用户点击了取消按钮
    /// - null：对话框被意外关闭（如按 ESC 键触发取消）
    ///
    /// 【为什么返回 bool? 而非 bool？】
    /// 使用 Nullable&lt;bool&gt; 可以区分三种状态：确认(true)、取消(false)、未完成(null)。
    /// 这与 WPF 内置的 MessageBox.ShowDialog() 返回类型一致，保持 API 风格统一。
    /// </summary>
    public bool? ShowDialog()
    {
        _overlay = new PopupOverlay();
        _overlay.Content = this;
        _overlay.ShowDialog();
        return _dialogResult;
    }

    /// <summary>
    /// 对话框结果状态。在 OnConfirm/OnCancel 中设置，在 ShowDialog 中返回。
    /// 初始值为 null，表示用户尚未做出选择。
    /// </summary>
    private bool? _dialogResult;

    /// <summary>
    /// 关闭对话框。
    /// 通过关闭 PopupOverlay 内部创建的 Window 来实现。
    /// 使用 ?. 条件运算符，防止 _overlay 为 null 时抛出 NullReferenceException。
    /// </summary>
    public void Close()
    {
        _overlay?.Close();
    }

    /// <summary>
    /// 确认路由事件。
    ///
    /// 【为什么用路由事件而非普通事件？】
    /// 路由事件（Routed Event）支持沿可视树向上冒泡传播。
    /// 这意味着父容器可以监听子控件的 Confirmed 事件，无需在每个 GlassDialog 上单独注册。
    /// 例如：一个页面有多个 GlassDialog，父级 Grid 可以统一处理 Confirmed 事件。
    ///
    /// 【RoutingStrategy.Bubble 的含义】
    /// 冒泡策略：事件从触发源（GlassDialog）沿可视树向上冒泡，直到根元素。
    /// 与之相对的是 Tunnel（隧道，从根向下）和 Direct（直接，仅目标元素）。
    /// </summary>
    public static readonly RoutedEvent ConfirmedEvent = EventManager.RegisterRoutedEvent("Confirmed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(GlassDialog));

    /// <summary>取消路由事件，与 ConfirmedEvent 类似，用于通知取消操作。</summary>
    public static readonly RoutedEvent CancelledEvent = EventManager.RegisterRoutedEvent("Cancelled", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(GlassDialog));

    /// <summary>
    /// Confirmed 事件的 CLR 包装器。
    ///
    /// 【为什么需要 add/remove 而非自动属性？】
    /// 路由事件的订阅机制与普通事件不同，需要通过 AddHandler/RemoveHandler 注册到路由事件系统。
    /// 简写形式：add 和 remove 访问器内部委托给 AddHandler 和 RemoveHandler。
    /// 这是 WPF 路由事件的标准写法。
    /// </summary>
    public event RoutedEventHandler Confirmed { add => AddHandler(ConfirmedEvent, value); remove => RemoveHandler(ConfirmedEvent, value); }

    /// <summary>Cancelled 事件的 CLR 包装器。</summary>
    public event RoutedEventHandler Cancelled { add => AddHandler(CancelledEvent, value); remove => RemoveHandler(CancelledEvent, value); }
}
