using System.Windows;
using System.Windows.Controls;

namespace GeneralControl.Controls;

/// <summary>
/// 毛玻璃复选框。
///
/// 【GcCheckBox 的行为】
/// GcCheckBox 继承自 GcToggleButton，有三态（ThreeState）：
///   Checked / Unchecked / Indeterminate（不确定状态）。
/// 通过 IsChecked 属性（bool? 类型）表示三种状态。
/// 毛玻璃样式会重定义其 CheckMark 和边框的视觉呈现。
/// </summary>
public class GcCheckBox : System.Windows.Controls.CheckBox
{
    static GcCheckBox()
    {
        // 为 GcCheckBox 注册毛玻璃主题样式。
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcCheckBox), new FrameworkPropertyMetadata(typeof(GcCheckBox)));
    }
}





