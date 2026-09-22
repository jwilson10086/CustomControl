using System.Windows;
using System.Windows.Controls;

namespace GeneralControl.Controls;

/// <summary>
/// 毛玻璃提示气泡（ToolTip）。
///
/// 【ToolTip 的特殊性】
/// ToolTip 不是可视化树中的常规子元素，
/// 它由 ToolTipService 管理，通常渲染在一个独立的 Popup 中。
/// 因此毛玻璃样式对 ToolTip 的应用需要确保 Popup 的
/// AllowsTransparency 属性为 true，否则透明效果可能失效。
/// </summary>
public class GcToolTip : System.Windows.Controls.ToolTip
{
    static GcToolTip()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcToolTip), new FrameworkPropertyMetadata(typeof(GcToolTip)));
    }
}








