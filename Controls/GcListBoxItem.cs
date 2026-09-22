using System.Windows;
using System.Windows.Controls;

namespace GeneralControl.Controls;

/// <summary>毛玻璃列表项（悬停高亮、选中时玻璃亮起）。</summary>
public class GcListBoxItem : System.Windows.Controls.ListBoxItem
{
    static GcListBoxItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcListBoxItem), new FrameworkPropertyMetadata(typeof(GcListBoxItem)));
    }
}





