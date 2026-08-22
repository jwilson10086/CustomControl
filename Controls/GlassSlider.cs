using System.Windows;
using System.Windows.Controls;

namespace CustomControl.Controls;

/// <summary>
/// 毛玻璃滑块。
///
/// 【Slider 的内部结构】
/// Slider 由三部分组成：
///   - Track（轨道）：中间的长条背景。
///   - Thumb（滑块手柄）：可拖动的部分。
///   - RepeatButton（增减按钮）：轨道两端的可选点击区域。
/// 毛玻璃样式会重定义轨道和手柄的外观，使其呈现半透明毛玻璃质感。
/// </summary>
public class GlassSlider : Slider
{
    static GlassSlider()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassSlider), new FrameworkPropertyMetadata(typeof(GlassSlider)));
    }
}
