using System.Windows;
using System.Windows.Controls;

namespace CustomControl.Controls;

/// <summary>毛玻璃选项卡控件。</summary>
public class GlassTabControl : TabControl
{
    static GlassTabControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassTabControl), new FrameworkPropertyMetadata(typeof(GlassTabControl)));
    }
}
