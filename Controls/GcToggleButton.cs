using System.Windows;
using System.Windows.Controls.Primitives;

namespace GeneralControl.Controls;

/// <summary>
/// 毛玻璃开关按钮（GcToggleButton 风格）。
///
/// 【GcButton 与 GcToggleButton 的区别】
/// GcButton：点击后立即弹起，不会保持状态。
/// GcToggleButton：点击后保持按压/弹起状态（IsChecked 属性），适合做二态开关。
///
/// GcToggleButton 是 System.Windows.Controls.Primitives 命名空间中的控件，
/// GcCheckBox 和 RepeatButton 等也继承自它。
/// </summary>
public class GcToggleButton : System.Windows.Controls.Primitives.ToggleButton
{
    static GcToggleButton()
    {
        // 与 GcButton 同理，为 GcToggleButton 注册毛玻璃主题样式。
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcToggleButton), new FrameworkPropertyMetadata(typeof(GcToggleButton)));
    }
}






