using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CustomControl.Controls;

/// <summary>
/// 毛玻璃头像：圆形（或圆角方形）裁剪，支持图片与文字缩写两种内容，
/// 可选右下角在线状态点。
///
/// 【显示优先级】
/// Source 有值 → 显示图片；否则显示 Text 文字缩写。
///
/// 【为什么文字不自动缩放？】
/// 头像直径由 Diameter 控制，文字大小交给 FontSize（样式默认 14），
/// 用户可按需覆盖；避免在代码里做测量循环，保持控件轻量。
/// </summary>
public class GlassAvatar : Control
{
    static GlassAvatar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassAvatar), new FrameworkPropertyMetadata(typeof(GlassAvatar)));
    }

    // ==================== 依赖属性 ====================

    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(nameof(Source), typeof(ImageSource), typeof(GlassAvatar),
            new PropertyMetadata(null));

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(GlassAvatar),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DiameterProperty =
        DependencyProperty.Register(nameof(Diameter), typeof(double), typeof(GlassAvatar),
            new PropertyMetadata(40.0));

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(GlassAvatar),
            new PropertyMetadata(new CornerRadius(999)));

    public static readonly DependencyProperty IsStatusVisibleProperty =
        DependencyProperty.Register(nameof(IsStatusVisible), typeof(bool), typeof(GlassAvatar),
            new PropertyMetadata(false));

    public static readonly DependencyProperty StatusBrushProperty =
        DependencyProperty.Register(nameof(StatusBrush), typeof(Brush), typeof(GlassAvatar),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x4C, 0xD9, 0x64))));

    /// <summary>头像图片（设置后忽略 Text）。</summary>
    public ImageSource? Source
    {
        get => (ImageSource?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>文字缩写（如姓名首字），Source 为空时显示。</summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>头像直径（正方形边长）。</summary>
    public double Diameter
    {
        get => (double)GetValue(DiameterProperty);
        set => SetValue(DiameterProperty, value);
    }

    /// <summary>圆角半径。默认 999 即正圆；设小值可得圆角方形。</summary>
    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    /// <summary>是否显示在线状态点。</summary>
    public bool IsStatusVisible
    {
        get => (bool)GetValue(IsStatusVisibleProperty);
        set => SetValue(IsStatusVisibleProperty, value);
    }

    /// <summary>状态点颜色。默认在线绿。</summary>
    public Brush StatusBrush
    {
        get => (Brush)GetValue(StatusBrushProperty);
        set => SetValue(StatusBrushProperty, value);
    }
}
