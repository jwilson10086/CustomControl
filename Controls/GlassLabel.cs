using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CustomControl.Controls;

/// <summary>
/// 毛玻璃标签（半透明玻璃胶囊背景），支持任意角度旋转。
///
/// 【GlassLabel 额外功能：旋转】
/// 普通 Label 不支持旋转，这里通过添加 Angle 依赖属性，
/// 并在属性变更回调中动态设置 RenderTransform 来实现旋转效果。
///
/// 【RenderTransform vs LayoutTransform】
/// - RenderTransform：在渲染阶段应用变换，不影响布局（其他控件的位置不变）。
/// - LayoutTransform：在布局阶段应用变换，会影响周围控件的排列。
/// 这里使用 RenderTransform，因为标签旋转时不应影响其他控件的布局位置。
/// </summary>
public class GlassLabel : Label
{
    static GlassLabel()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassLabel), new FrameworkPropertyMetadata(typeof(GlassLabel)));
    }

    /// <summary>
    /// 标签旋转角度（单位：度）的依赖属性。
    ///
    /// 【FrameworkPropertyMetadata vs PropertyMetadata】
    /// 这里使用 FrameworkPropertyMetadata 而非普通 PropertyMetadata，
    /// 是因为需要指定 FrameworkPropertyMetadataOptions.AffectsRender 标志。
    /// AffectsRender 告诉 WPF 布局系统："当这个属性变化时，需要重新渲染控件"，
    /// 但不会触发布局重算（不影响周围控件的位置）。
    /// </summary>
    public static readonly DependencyProperty AngleProperty =
        DependencyProperty.Register(nameof(Angle), typeof(double), typeof(GlassLabel),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, OnAngleChanged));

    /// <summary>标签旋转角度（度）。0 表示水平，正值顺时针旋转。</summary>
    public double Angle
    {
        get => (double)GetValue(AngleProperty);
        set => SetValue(AngleProperty, value);
    }

    /// <summary>
    /// Angle 属性变更回调。
    ///
    /// 【为什么使用静态方法？】
    /// WPF 的依赖属性变更回调必须是静态方法（避免实例引用导致内存泄漏）。
    /// 通过参数 DependencyObject d 可以安全地获取控件实例。
    ///
    /// 【TransformOrigin 的含义】
    /// RenderTransformOrigin = (0.5, 0.5) 表示旋转中心在控件的正中心。
    /// 坐标系：(0,0) 是左上角，(1,1) 是右下角，(0.5,0.5) 是中心点。
    /// </summary>
    private static void OnAngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var label = (GlassLabel)d;
        var angle = (double)e.NewValue;
        // 创建旋转变换并应用到控件的渲染变换属性上。
        label.RenderTransform = new RotateTransform(angle);
        // 设置旋转中心为控件正中心。
        label.RenderTransformOrigin = new Point(0.5, 0.5);
    }
}
