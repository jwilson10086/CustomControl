using System.Windows;
using System.Windows.Controls.Primitives;

namespace CustomControl.Controls;

/// <summary>
/// 毛玻璃开关按钮（ToggleButton 风格）。
///
/// 【GlassButton 与 GlassToggleButton 的区别】
/// Button：点击后立即弹起，不会保持状态。
/// ToggleButton：点击后保持按压/弹起状态（IsChecked 属性），适合做二态开关。
///
/// ToggleButton 是 System.Windows.Controls.Primitives 命名空间中的控件，
/// CheckBox 和 RepeatButton 等也继承自它。
/// </summary>
public class GlassToggleButton : ToggleButton
{
    static GlassToggleButton()
    {
        // 与 GlassButton 同理，为 ToggleButton 注册毛玻璃主题样式。
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassToggleButton), new FrameworkPropertyMetadata(typeof(GlassToggleButton)));
    }
}
