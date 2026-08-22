using System.Windows;
using System.Windows.Controls;

namespace CustomControl.Controls;

/// <summary>
/// 毛玻璃下拉选择项。
///
/// 【注意事项】
/// ComboBoxItem 在弹出列表中的渲染可能受到 Popup 的影响（如透明度失效），
/// 在实际开发中可能需要额外处理 Popup 的 AllowsTransparency 属性。
/// </summary>
public class GlassComboBoxItem : ComboBoxItem
{
    static GlassComboBoxItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassComboBoxItem), new FrameworkPropertyMetadata(typeof(GlassComboBoxItem)));
    }
}
