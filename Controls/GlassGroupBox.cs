using System.Windows;
using System.Windows.Controls;

namespace CustomControl.Controls;

/// <summary>
/// 毛玻璃分组容器。
///
/// 【GroupBox 的作用】
/// GroupBox 在视觉上将一组相关的控件用边框和标题包裹起来，
/// 提供逻辑分组的视觉暗示。在毛玻璃风格中，
/// 边框和标题区域会呈现半透明玻璃效果。
/// </summary>
public class GlassGroupBox : GroupBox
{
    static GlassGroupBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassGroupBox), new FrameworkPropertyMetadata(typeof(GlassGroupBox)));
    }
}
