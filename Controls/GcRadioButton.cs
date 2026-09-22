using System.Windows;
using System.Windows.Controls;

namespace GeneralControl.Controls;

/// <summary>
/// 毛玻璃单选按钮。
///
/// 【GcRadioButton 的分组机制】
/// 同一组 GcRadioButton 通过 GroupName 属性或位于同一父容器中自动互斥：
/// 选中一个时，同组的其他自动取消选中。
/// 毛玻璃样式会影响其圆形选择框的外观。
/// </summary>
public class GcRadioButton : System.Windows.Controls.RadioButton
{
    static GcRadioButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcRadioButton), new FrameworkPropertyMetadata(typeof(GcRadioButton)));
    }
}





