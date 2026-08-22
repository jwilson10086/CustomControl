using System.Windows;
using System.Windows.Controls;

namespace CustomControl.Controls;

/// <summary>毛玻璃列表容器。</summary>
public class GlassListBox : ListBox
{
    static GlassListBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassListBox), new FrameworkPropertyMetadata(typeof(GlassListBox)));
    }
}
