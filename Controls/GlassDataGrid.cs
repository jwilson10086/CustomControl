using System.Windows;
using System.Windows.Controls;

namespace CustomControl.Controls;

/// <summary>
/// 毛玻璃数据表格。
///
/// 【DataGrid 的复杂性】
/// DataGrid 是 WPF 中最复杂的控件之一，包含多个可视部分：
///   - HeaderRow（列标题行）
///   - RowPresenter（数据行容器）
///   - ScrollViewer（滚动条）
///   - 各种单元格模板（CellTemplate / CellEditingTemplate）
///
/// 要为 DataGrid 实现毛玻璃效果，通常需要重写以下几部分的样式：
///   - DataGrid 本身的背景和边框
///   - DataGridColumnHeader 的标题栏样式
///   - DataGridRow 的行样式（包括悬停和选中状态）
///   - DataGridCell 的单元格样式
/// 这使得 GlassDataGrid 的样式文件通常是最长的。
/// </summary>
public class GlassDataGrid : DataGrid
{
    static GlassDataGrid()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassDataGrid), new FrameworkPropertyMetadata(typeof(GlassDataGrid)));
    }
}
