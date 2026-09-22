using System.Windows;
using System.Windows.Controls;

namespace GeneralControl.Controls;

/// <summary>毛玻璃选项卡控件。</summary>
public class GcTabControl : System.Windows.Controls.TabControl
{
    static GcTabControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcTabControl), new FrameworkPropertyMetadata(typeof(GcTabControl)));
    }
}





