using System.Windows;
using System.Windows.Controls;

namespace GeneralControl.Controls;

/// <summary>
/// 毛玻璃卡片容器：半透明渐变玻璃底、边缘反光、圆角与投影。
/// 可配合 GcTiltBehavior 实现 3D 悬浮倾斜效果。
/// </summary>
public class GcCard : ContentControl
{
    static GcCard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcCard), new FrameworkPropertyMetadata(typeof(GcCard)));
    }

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(GcCard),
            new PropertyMetadata(new CornerRadius(16)));

    /// <summary>卡片圆角半径。</summary>
    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public static readonly DependencyProperty IsElevatedProperty =
        DependencyProperty.Register(nameof(IsElevated), typeof(bool), typeof(GcCard),
            new PropertyMetadata(false));

    /// <summary>是否启用投影（悬浮时增强投影）。</summary>
    public bool IsElevated
    {
        get => (bool)GetValue(IsElevatedProperty);
        set => SetValue(IsElevatedProperty, value);
    }

    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(string), typeof(GcCard),
            new PropertyMetadata(null));

    /// <summary>卡片标题（为空时自动隐藏标题区）。</summary>
    public string? Header
    {
        get => (string?)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }
}



