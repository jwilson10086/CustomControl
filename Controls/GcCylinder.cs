using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GeneralControl.Controls;

/// <summary>
/// 工业气缸控件（气动/电动缸侧视图）。
///
/// 【结构】
/// 缸体（圆角矩形）+ 活塞（缸内滑动）+ 活塞杆（穿出右端盖）。
///
/// 【位置模型】
/// Extension 0~100 表示伸出百分比：
/// - 0   = 完全缩回（杆几乎不可见）；
/// - 100 = 完全伸出。
/// 活塞与杆的位置由代码后置换算（纯 XAML 做不了区间映射），
/// 通过 TranslateTransform 移动活塞组实现，不触发布局重排。
/// </summary>
[TemplatePart(Name = PartPistonGroup, Type = typeof(FrameworkElement))]
public class GcCylinder : Control
{
    private const string PartPistonGroup = "PART_PistonGroup";

    static GcCylinder()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcCylinder), new FrameworkPropertyMetadata(typeof(GcCylinder)));
    }

    public static readonly DependencyProperty ExtensionProperty =
        DependencyProperty.Register(nameof(Extension), typeof(double), typeof(GcCylinder),
            new PropertyMetadata(0.0, OnExtensionChanged));

    /// <summary>伸出百分比 0~100。</summary>
    public double Extension
    {
        get => (double)GetValue(ExtensionProperty);
        set => SetValue(ExtensionProperty, value);
    }

    // ==================== 几何常量（与模板 Viewbox 坐标一致） ====================

    /// <summary>缸体内部可滑动区起点 x（左端盖右侧）。</summary>
    private const double TravelStart = 6;
    /// <summary>缸体内部可滑动区长度（到右端盖内壁）。</summary>
    private const double TravelLength = 39;

    private FrameworkElement? _pistonGroup;
    private TranslateTransform? _transform;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _pistonGroup = GetTemplateChild(PartPistonGroup) as FrameworkElement;
        UpdatePosition();
    }

    private static void OnExtensionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((GcCylinder)d).UpdatePosition();

    /// <summary>按伸出百分比平移活塞组（活塞 + 杆作为一个整体）。</summary>
    private void UpdatePosition()
    {
        if (_pistonGroup == null)
        {
            return;
        }

        var ratio = Math.Clamp(Extension, 0, 100) / 100;
        var offset = TravelStart + ratio * TravelLength;

        // 注意：模板里声明的 Transform 会被 WPF 冻结（只读），不能原地修改；
        // 必须换成自建的可变实例。
        _transform ??= new TranslateTransform();
        _pistonGroup.RenderTransform = _transform;
        _transform.X = offset;
    }
}



