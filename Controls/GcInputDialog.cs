using System.Windows;
using System.Windows.Controls;

namespace GeneralControl.Controls;

/// <summary>
/// 毛玻璃输入对话框：含标题、提示信息、文本输入框、确认/取消按钮。
///
/// 【InputText vs InputValue 的区别】
/// - InputText：输入框的默认显示文本（初始值）。
/// - InputValue：用户确认后返回的值（结果）。
/// 这种"输入值/结果值"分离的设计有两个好处：
/// (1) 可以预填默认值；
/// (2) 对话框关闭后从 InputValue 读取用户选择，类似 MessageBox.ShowDialog() 的返回值模式。
///
/// 【与 GcDialog 的关系】
/// 结构几乎一致，只是把 GcListBox 换成了 GcTextBox，
/// 多了 Message（提示信息）和 InputText/InputPlaceholder（输入相关）属性。
/// </summary>
public class GcInputDialog : ContentControl
{
    private System.Windows.Controls.TextBox? _inputBox;
    private GcPopupOverlay? _overlay;

    static GcInputDialog()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcInputDialog), new FrameworkPropertyMetadata(typeof(GcInputDialog)));
    }

    /// <summary>对话框标题。</summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(GcInputDialog), new PropertyMetadata("请输入"));

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>提示信息，显示在标题下方、输入框上方。</summary>
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(GcInputDialog), new PropertyMetadata(""));

    public string Message { get => (string)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }

    /// <summary>输入框的初始文本（打开对话框时预填）。</summary>
    public static readonly DependencyProperty InputTextProperty =
        DependencyProperty.Register(nameof(InputText), typeof(string), typeof(GcInputDialog), new PropertyMetadata(""));

    public string InputText { get => (string)GetValue(InputTextProperty); set => SetValue(InputTextProperty, value); }

    /// <summary>输入框的水印提示（为空且未输入时显示）。</summary>
    public static readonly DependencyProperty InputPlaceholderProperty =
        DependencyProperty.Register(nameof(InputPlaceholder), typeof(string), typeof(GcInputDialog), new PropertyMetadata(""));

    public string InputPlaceholder { get => (string)GetValue(InputPlaceholderProperty); set => SetValue(InputPlaceholderProperty, value); }

    /// <summary>用户确认后的输入结果。</summary>
    public static readonly DependencyProperty InputValueProperty =
        DependencyProperty.Register(nameof(InputValue), typeof(string), typeof(GcInputDialog), new PropertyMetadata(""));

    public string InputValue { get => (string)GetValue(InputValueProperty); set => SetValue(InputValueProperty, value); }

    /// <summary>确认按钮文字。</summary>
    public static readonly DependencyProperty ConfirmTextProperty =
        DependencyProperty.Register(nameof(ConfirmText), typeof(string), typeof(GcInputDialog), new PropertyMetadata("确认"));

    public string ConfirmText { get => (string)GetValue(ConfirmTextProperty); set => SetValue(ConfirmTextProperty, value); }

    /// <summary>取消按钮文字。</summary>
    public static readonly DependencyProperty CancelTextProperty =
        DependencyProperty.Register(nameof(CancelText), typeof(string), typeof(GcInputDialog), new PropertyMetadata("取消"));

    public string CancelText { get => (string)GetValue(CancelTextProperty); set => SetValue(CancelTextProperty, value); }

    /// <summary>
    /// 模板应用完成：获取输入框并挂接按钮事件。
    /// 同时把 InputText 预填到输入框，实现"默认值"功能。
    /// </summary>
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _inputBox = GetTemplateChild("PART_InputBox") as System.Windows.Controls.TextBox;
        if (_inputBox != null && string.IsNullOrEmpty(_inputBox.Text))
            _inputBox.Text = InputText;

        if (GetTemplateChild("PART_ConfirmButton") is System.Windows.Controls.Button confirmBtn)
            confirmBtn.Click += OnConfirm;
        if (GetTemplateChild("PART_CancelButton") is System.Windows.Controls.Button cancelBtn)
            cancelBtn.Click += OnCancel;
    }

    /// <summary>确认：读取输入框内容写入 InputValue，标记结果为 true 并关闭。</summary>
    private void OnConfirm(object sender, RoutedEventArgs e)
    {
        if (_inputBox != null)
            InputValue = _inputBox.Text;

        _dialogResult = true;
        RaiseEvent(new RoutedEventArgs(ConfirmedEvent));
        Close();
    }

    /// <summary>取消：不修改 InputValue，标记结果为 false 并关闭。</summary>
    private void OnCancel(object sender, RoutedEventArgs e)
    {
        _dialogResult = false;
        RaiseEvent(new RoutedEventArgs(CancelledEvent));
        Close();
    }

    /// <summary>非模态显示。</summary>
    public void Show()
    {
        _overlay = new GcPopupOverlay();
        _overlay.Content = this;
        _overlay.Show();
    }

    /// <summary>
    /// 模态显示，返回对话框结果：
    /// true=确认（读取 InputValue），false=取消，null=意外关闭。
    /// </summary>
    public bool? ShowDialog()
    {
        _overlay = new GcPopupOverlay();
        _overlay.Content = this;
        _overlay.ShowDialog();
        return _dialogResult;
    }

    private bool? _dialogResult;

    /// <summary>关闭对话框（通过关闭 GcPopupOverlay 实现）。</summary>
    public void Close()
    {
        _overlay?.Close();
    }

    /// <summary>确认路由事件（冒泡）。</summary>
    public static readonly RoutedEvent ConfirmedEvent = EventManager.RegisterRoutedEvent("Confirmed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(GcInputDialog));

    /// <summary>取消路由事件（冒泡）。</summary>
    public static readonly RoutedEvent CancelledEvent = EventManager.RegisterRoutedEvent("Cancelled", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(GcInputDialog));

    public event RoutedEventHandler Confirmed { add => AddHandler(ConfirmedEvent, value); remove => RemoveHandler(ConfirmedEvent, value); }
    public event RoutedEventHandler Cancelled { add => AddHandler(CancelledEvent, value); remove => RemoveHandler(CancelledEvent, value); }
}









