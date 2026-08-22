using System.Windows;
using System.Windows.Controls;

namespace CustomControl.Controls;

/// <summary>
/// 毛玻璃文本块（半透明白色正文）。
///
/// 【TextBlock vs TextBox】
/// - TextBlock：只读文本显示控件，轻量，适合展示静态文本。
/// - TextBox：可编辑文本输入控件，重量级，支持用户输入。
/// 毛玻璃文本块主要用于信息展示，不需要编辑功能。
/// </summary>
public class GlassTextBlock : TextBlock
{
    static GlassTextBlock()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassTextBlock), new FrameworkPropertyMetadata(typeof(GlassTextBlock)));
    }
}
