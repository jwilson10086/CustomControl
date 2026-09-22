using System.Windows;
using System.Windows.Controls.Primitives;

namespace GeneralControl.Controls;

/// <summary>
/// 毛玻璃开关（胶囊形切换开关 / Toggle Switch）。
///
/// 【为什么继承 GcToggleButton 而不是 GcCheckBox？】
/// GcToggleButton 和 GcCheckBox 都提供 IsChecked 属性，
/// 但 GcToggleButton 的默认模板更接近"按钮"的视觉隐喻，
/// 更适合改造成 iOS/Android 风格的滑动开关（Toggle Switch）。
/// 通过控件模板中的 Border + Thumb 组合实现胶囊形状的滑动效果。
///
/// 【GcToggleButton vs GcToggleSwitch 的区别】
/// GcToggleButton：保持原生 GcToggleButton 的按压态外观。
/// GcToggleSwitch：完全重新定义外观，呈现为胶囊形滑动开关。
/// </summary>
public class GcToggleSwitch : GcToggleButton
{
    static GcToggleSwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcToggleSwitch), new FrameworkPropertyMetadata(typeof(GcToggleSwitch)));
    }
}



