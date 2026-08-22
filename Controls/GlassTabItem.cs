using System.Windows;
using System.Windows.Controls;

namespace CustomControl.Controls;

/// <summary>毛玻璃选项卡页。</summary>
public class GlassTabItem : TabItem
{
    static GlassTabItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassTabItem), new FrameworkPropertyMetadata(typeof(GlassTabItem)));
    }
}
