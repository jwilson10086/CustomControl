using System.Windows;
using System.Windows.Controls;

namespace CustomControl.Controls;

/// <summary>
/// 毛玻璃按钮控件。
///
/// 【设计目的】
/// 在 WPF 自定义控件开发中，通常有两种方式应用自定义样式：
///   1. 通过 Generic.xaml 中的隐式样式（Keyless Style）自动应用；
///   2. 在 XAML 中显式指定 Style="{StaticResource ...}"。
///
/// 为了让控件自动找到并应用自己专属的样式，需要在静态构造函数中重写
/// DefaultStyleKey 元数据——这是 WPF 主题样式查找机制的核心。
///
/// 【为什么继承 Button 而不是直接使用 Button？】
/// 派生出一个空的子类，目的是在 XAML 中使用语义化标签（如 &lt;GlassButton&gt;）
/// 并且可以独立地为它定义专属样式，而不会影响全局 Button 的外观。
/// 这种"空子类+专属样式"的模式在 WPF 控件库中非常常见。
/// </summary>
public class GlassButton : Button
{
    /// <summary>
    /// 静态构造函数——在整个应用程序生命周期内只会执行一次。
    /// 【作用】覆盖 DefaultStyleKey 元数据，告诉 WPF 的主题系统：
    /// "当查找 GlassButton 的默认样式时，请去主题资源中找类型为 GlassButton 的 Style。"
    /// </summary>
    static GlassButton()
    {
        // OverrideMetadata 将 DefaultStyleKey 的值替换为当前类型，
        // 运行时框架会根据这个 Key 去查找匹配的隐式样式。
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassButton), new FrameworkPropertyMetadata(typeof(GlassButton)));
    }
}
