using System.Windows;
using System.Windows.Controls.Primitives;

namespace CustomControl.Controls;

/// <summary>
/// 毛玻璃开关（胶囊形切换开关 / Toggle Switch）。
///
/// 【为什么继承 ToggleButton 而不是 CheckBox？】
/// ToggleButton 和 CheckBox 都提供 IsChecked 属性，
/// 但 ToggleButton 的默认模板更接近"按钮"的视觉隐喻，
/// 更适合改造成 iOS/Android 风格的滑动开关（Toggle Switch）。
/// 通过控件模板中的 Border + Thumb 组合实现胶囊形状的滑动效果。
///
/// 【GlassToggleButton vs GlassToggleSwitch 的区别】
/// GlassToggleButton：保持原生 ToggleButton 的按压态外观。
/// GlassToggleSwitch：完全重新定义外观，呈现为胶囊形滑动开关。
/// </summary>
public class GlassToggleSwitch : ToggleButton
{
    static GlassToggleSwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassToggleSwitch), new FrameworkPropertyMetadata(typeof(GlassToggleSwitch)));
    }
}
