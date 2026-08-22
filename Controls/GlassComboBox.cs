using System.Windows;
using System.Windows.Controls;

namespace CustomControl.Controls;

/// <summary>
/// 毛玻璃下拉选择框。
///
/// 【ComboBox 的内部结构】
/// ComboBox 由两部分组成：
///   1. 顶部的可编辑/只读文本区域（显示当前选中项）；
///   2. 点击后弹出的下拉列表（Popup + ItemsPresenter）。
/// 毛玻璃样式需要同时重定义这两部分的视觉外观。
///
/// 【为什么需要 GlassComboBoxItem？】
/// ComboBox 的弹出列表项默认使用 ComboBoxItem 控件。
/// 要让下拉列表的每一项也有毛玻璃效果，
/// 必须单独为 ComboBoxItem 定义样式，因此需要独立的 GlassComboBoxItem。
/// </summary>
public class GlassComboBox : ComboBox
{
    static GlassComboBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassComboBox), new FrameworkPropertyMetadata(typeof(GlassComboBox)));
    }
}
