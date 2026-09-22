using System.Windows;
using System.Windows.Controls;

namespace GeneralControl.Controls;

/// <summary>
/// 毛玻璃右键菜单。
///
/// 【GcContextMenu 的渲染机制】
/// GcContextMenu 和 ToolTip 类似，也渲染在独立的 Popup 中，
/// 同样需要 AllowsTransparency=true 才能正确显示毛玻璃透明效果。
/// 另外，右键菜单中的 MenuItem 支持多级嵌套，
/// 每一级的子菜单都需要使用对应的样式。
/// </summary>
public class GcContextMenu : System.Windows.Controls.ContextMenu
{
    static GcContextMenu()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcContextMenu), new FrameworkPropertyMetadata(typeof(GcContextMenu)));
    }
}






