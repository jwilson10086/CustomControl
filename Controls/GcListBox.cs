using System.Windows;
using System.Windows.Controls;

namespace GeneralControl.Controls;

/// <summary>毛玻璃列表容器。</summary>
public class GcListBox : System.Windows.Controls.ListBox
{
    static GcListBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcListBox), new FrameworkPropertyMetadata(typeof(GcListBox)));
    }
}





