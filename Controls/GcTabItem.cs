using System.Windows;
using System.Windows.Controls;

namespace GeneralControl.Controls;

/// <summary>毛玻璃选项卡页。</summary>
public class GcTabItem : System.Windows.Controls.TabItem
{
    static GcTabItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcTabItem), new FrameworkPropertyMetadata(typeof(GcTabItem)));
    }
}





