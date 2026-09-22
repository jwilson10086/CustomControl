using System.Windows;
using System.Windows.Controls;

namespace GeneralControl.Controls;

/// <summary>
/// 毛玻璃下拉选择框。
///
/// 【GcComboBox 的内部结构】
/// GcComboBox 由两部分组成：
///   1. 顶部的可编辑/只读文本区域（显示当前选中项）；
///   2. 点击后弹出的下拉列表（Popup + ItemsPresenter）。
/// 毛玻璃样式需要同时重定义这两部分的视觉外观。
///
/// 【为什么需要 GcComboBoxItem？】
/// GcComboBox 的弹出列表项默认使用 GcComboBoxItem 控件。
/// 要让下拉列表的每一项也有毛玻璃效果，
/// 必须单独为 GcComboBoxItem 定义样式，因此需要独立的 GcComboBoxItem。
/// </summary>
public class GcComboBox : System.Windows.Controls.ComboBox
{
    static GcComboBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcComboBox), new FrameworkPropertyMetadata(typeof(GcComboBox)));
    }
}





