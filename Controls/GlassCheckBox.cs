using System.Windows;
using System.Windows.Controls;

namespace CustomControl.Controls;

/// <summary>
/// 毛玻璃复选框。
///
/// 【CheckBox 的行为】
/// CheckBox 继承自 ToggleButton，有三态（ThreeState）：
///   Checked / Unchecked / Indeterminate（不确定状态）。
/// 通过 IsChecked 属性（bool? 类型）表示三种状态。
/// 毛玻璃样式会重定义其 CheckMark 和边框的视觉呈现。
/// </summary>
public class GlassCheckBox : CheckBox
{
    static GlassCheckBox()
    {
        // 为 CheckBox 注册毛玻璃主题样式。
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassCheckBox), new FrameworkPropertyMetadata(typeof(GlassCheckBox)));
    }
}
