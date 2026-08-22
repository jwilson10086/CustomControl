using System.Windows;
using System.Windows.Controls;

namespace CustomControl.Controls;

/// <summary>毛玻璃列表项（悬停高亮、选中时玻璃亮起）。</summary>
public class GlassListBoxItem : ListBoxItem
{
    static GlassListBoxItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassListBoxItem), new FrameworkPropertyMetadata(typeof(GlassListBoxItem)));
    }
}
