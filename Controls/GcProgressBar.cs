using System.Windows;
using System.Windows.Controls;

namespace GeneralControl.Controls;

/// <summary>
/// 毛玻璃进度条。
///
/// 【GcProgressBar 的行为】
/// GcProgressBar 支持两种模式：
///   1. 确定模式（IsIndeterminate=false）：显示确切的进度百分比。
///   2. 不确定模式（IsIndeterminate=true）：显示循环滚动的动画。
/// 毛玻璃样式需要为两种模式提供不同的视觉表现。
/// </summary>
public class GcProgressBar : System.Windows.Controls.ProgressBar
{
    static GcProgressBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcProgressBar), new FrameworkPropertyMetadata(typeof(GcProgressBar)));
    }
}





