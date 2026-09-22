using System.Windows;
using System.Windows.Controls;

namespace GeneralControl.Controls;

/// <summary>
/// 毛玻璃分组容器。
///
/// 【GcGroupBox 的作用】
/// GcGroupBox 在视觉上将一组相关的控件用边框和标题包裹起来，
/// 提供逻辑分组的视觉暗示。在毛玻璃风格中，
/// 边框和标题区域会呈现半透明玻璃效果。
/// </summary>
public class GcGroupBox : System.Windows.Controls.GroupBox
{
    static GcGroupBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcGroupBox), new FrameworkPropertyMetadata(typeof(GcGroupBox)));
    }
}





