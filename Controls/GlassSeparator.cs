using System.Windows;
using System.Windows.Controls;

namespace CustomControl.Controls;

/// <summary>
/// 毛玻璃分隔线。
///
/// 【Separator 的用途】
/// 用于在视觉上分隔不同的内容区域。
/// 在毛玻璃风格中，分隔线会呈现为一条半透明的细线，
/// 与整体玻璃质感保持一致。
/// </summary>
public class GlassSeparator : Separator
{
    static GlassSeparator()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassSeparator), new FrameworkPropertyMetadata(typeof(GlassSeparator)));
    }
}
