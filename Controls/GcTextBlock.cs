using System.Windows;
using System.Windows.Controls;

namespace GeneralControl.Controls;

/// <summary>
/// 毛玻璃文本块（半透明白色正文）。
///
/// 【GcTextBlock vs GcTextBox】
/// - GcTextBlock：只读文本显示控件，轻量，适合展示静态文本。
/// - GcTextBox：可编辑文本输入控件，重量级，支持用户输入。
/// 毛玻璃文本块主要用于信息展示，不需要编辑功能。
/// </summary>
public class GcTextBlock : System.Windows.Controls.TextBlock
{
    static GcTextBlock()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcTextBlock), new FrameworkPropertyMetadata(typeof(GcTextBlock)));
    }
}






