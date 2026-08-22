using System.Windows;

namespace CustomControl.Controls;

/// <summary>
/// 弹窗附加属性：在 XAML 中直接配置 GlassDialog / GlassInputDialog 的参数。
/// </summary>
/// <remarks>
/// 【什么是附加属性？】
/// 附加属性（Attached Property）是 WPF 独有的概念，允许"在别人身上"设置属性。
/// 例如 Button 自身没有 DialogTitle 属性，但通过这个静态类，可以在 XAML 中写：
///   &lt;Button local:DialogProperties.DialogTitle="提示" .../&gt;
/// 这样做的好处是：不需要为每个控件都定义弹窗相关属性，实现了关注点分离。
///
/// 【为什么用附加属性而非继承？】
/// GlassDialog 和 GlassInputDialog 是两种不同的对话框，它们的属性（标题、消息、选项）各有不同。
/// 如果把这些属性放在一个公共基类里，会导致两个类耦合。
/// 用附加属性，可以在 XAML 中按需组合，更加灵活。
/// </remarks>
public static class DialogProperties
{
    /// <summary>
    /// 附加属性：对话框标题。
    ///
    /// 【为什么用 DependencyProperty 而非普通属性？】
    /// WPF 的数据绑定、动画、样式等机制都依赖依赖属性系统。
    /// 依赖属性支持：值继承、变更通知、默认值管理、内存优化（只在设置时占用内存）。
    ///
    /// 【RegisterAttached vs Register 的区别】
    /// RegisterAttached 注册的是附加属性，需要配套的 Get/Set 静态方法，
    /// 这些方法的签名是固定的（第一个参数必须是 DependencyObject），这样 XAML 解析器才能识别。
    /// </summary>
    public static readonly DependencyProperty DialogTitleProperty =
        DependencyProperty.RegisterAttached("DialogTitle", typeof(string), typeof(DialogProperties), new PropertyMetadata(""));

    public static string GetDialogTitle(DependencyObject obj) => (string)obj.GetValue(DialogTitleProperty);
    public static void SetDialogTitle(DependencyObject obj, string value) => obj.SetValue(DialogTitleProperty, value);

    /// <summary>
    /// 附加属性：对话框消息内容。
    /// 用于 GlassInputDialog 中显示提示信息。
    /// </summary>
    public static readonly DependencyProperty DialogMessageProperty =
        DependencyProperty.RegisterAttached("DialogMessage", typeof(string), typeof(DialogProperties), new PropertyMetadata(""));

    public static string GetDialogMessage(DependencyObject obj) => (string)obj.GetValue(DialogMessageProperty);
    public static void SetDialogMessage(DependencyObject obj, string value) => obj.SetValue(DialogMessageProperty, value);

    /// <summary>
    /// 附加属性：对话框选项列表，逗号分隔。
    ///
    /// 【为什么用字符串而非集合？】
    /// 附加属性在 XAML 中使用时，字符串是最简单的类型。
    /// 如果用 ObservableCollection&lt;string&gt;，XAML 语法会变得冗长：
    ///   &lt;local:DialogProperties.DialogItems&gt;
    ///       &lt;x:String&gt;选项1&lt;/x:String&gt;
    ///       ...
    ///   &lt;/local:DialogProperties.DialogItems&gt;
    /// 用逗号分隔的字符串，一行搞定，使用体验更好。
    /// 内部使用时再 Split 成数组即可。
    /// </summary>
    public static readonly DependencyProperty DialogItemsProperty =
        DependencyProperty.RegisterAttached("DialogItems", typeof(string), typeof(DialogProperties), new PropertyMetadata(""));

    /// <summary>逗号分隔的选项列表，如 "选项1,选项2,选项3"。</summary>
    public static string GetDialogItems(DependencyObject obj) => (string)obj.GetValue(DialogItemsProperty);
    public static void SetDialogItems(DependencyObject obj, string value) => obj.SetValue(DialogItemsProperty, value);
}
