using System.Windows;
using System.Windows.Controls;

namespace CustomControl.Controls;

/// <summary>
/// 毛玻璃单选按钮。
///
/// 【RadioButton 的分组机制】
/// 同一组 RadioButton 通过 GroupName 属性或位于同一父容器中自动互斥：
/// 选中一个时，同组的其他自动取消选中。
/// 毛玻璃样式会影响其圆形选择框的外观。
/// </summary>
public class GlassRadioButton : RadioButton
{
    static GlassRadioButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassRadioButton), new FrameworkPropertyMetadata(typeof(GlassRadioButton)));
    }
}
