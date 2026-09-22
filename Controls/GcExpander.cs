using System.Windows;
using System.Windows.Controls;

namespace GeneralControl.Controls;

/// <summary>
/// 毛玻璃折叠面板（GcExpander）。
///
/// 【GcExpander 的行为】
/// GcExpander 可以展开/折叠显示其内容区域，
/// 通过 IsExpanded 属性（bool）控制当前状态。
/// 毛玻璃样式会影响标题栏（Header）和展开内容区域的外观。
/// 展开/折叠的动画也可以在控件模板中自定义。
/// </summary>
public class GcExpander : System.Windows.Controls.Expander
{
    static GcExpander()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcExpander), new FrameworkPropertyMetadata(typeof(GcExpander)));
    }
}





