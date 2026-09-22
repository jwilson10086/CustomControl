using System.Windows;
using System.Windows.Controls;

namespace GeneralControl.Controls;

/// <summary>
/// 毛玻璃分隔线。
///
/// 【GcSeparator 的用途】
/// 用于在视觉上分隔不同的内容区域。
/// 在毛玻璃风格中，分隔线会呈现为一条半透明的细线，
/// 与整体玻璃质感保持一致。
/// </summary>
public class GcSeparator : System.Windows.Controls.Separator
{
    static GcSeparator()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcSeparator), new FrameworkPropertyMetadata(typeof(GcSeparator)));
    }
}





