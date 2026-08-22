using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace CustomControl.Effects;

/// <summary>
/// 液体玻璃（Liquid Glass）像素着色器封装。
/// 着色器源自 dragosniamtu/WPF-Liquid-Glass-Effect (MIT)，详见 THIRD-PARTY-NOTICES.md。
/// 将 <see cref="GlassyEffect"/> 应用到任意元素即可获得带折射/色散/高光的液体玻璃质感。
/// </summary>
public sealed class GlassyEffect : ShaderEffect
{
    public static readonly DependencyProperty InputProperty =
        RegisterPixelShaderSamplerProperty(nameof(Input), typeof(GlassyEffect), 0);

    public static readonly DependencyProperty TextureSizeProperty =
        DependencyProperty.Register(
            nameof(TextureSize),
            typeof(Point),
            typeof(GlassyEffect),
            new UIPropertyMetadata(new Point(1.0, 1.0), PixelShaderConstantCallback(0)));

    public static readonly DependencyProperty GlassCenterProperty =
        DependencyProperty.Register(
            nameof(GlassCenter),
            typeof(Point),
            typeof(GlassyEffect),
            new UIPropertyMetadata(new Point(0.0, 0.0), PixelShaderConstantCallback(1)));

    public static readonly DependencyProperty GlassSizeProperty =
        DependencyProperty.Register(
            nameof(GlassSize),
            typeof(Point),
            typeof(GlassyEffect),
            new UIPropertyMetadata(new Point(120.0, 80.0), PixelShaderConstantCallback(2)));

    public static readonly DependencyProperty BlurIntensityProperty =
        DependencyProperty.Register(
            nameof(BlurIntensity),
            typeof(float),
            typeof(GlassyEffect),
            new UIPropertyMetadata(1.2f, PixelShaderConstantCallback(3)));

    public GlassyEffect()
    {
        PixelShader = new PixelShader
        {
            UriSource = new Uri("pack://application:,,,/CustomControl;component/Effects/GlassyEffect.ps", UriKind.Absolute),
        };

        UpdateShaderValue(InputProperty);
        UpdateShaderValue(TextureSizeProperty);
        UpdateShaderValue(GlassCenterProperty);
        UpdateShaderValue(GlassSizeProperty);
        UpdateShaderValue(BlurIntensityProperty);
    }

    /// <summary>输入的背景画刷（通常为桌面截图 ImageBrush 或宿主元素背景）。</summary>
    public Brush? Input
    {
        get => (Brush?)GetValue(InputProperty);
        set => SetValue(InputProperty, value);
    }

    /// <summary>纹理像素尺寸，等于着色器应用元素的像素尺寸。</summary>
    public Point TextureSize
    {
        get => (Point)GetValue(TextureSizeProperty);
        set => SetValue(TextureSizeProperty, value);
    }

    /// <summary>玻璃效果中心点（像素坐标）。</summary>
    public Point GlassCenter
    {
        get => (Point)GetValue(GlassCenterProperty);
        set => SetValue(GlassCenterProperty, value);
    }

    /// <summary>玻璃区域尺寸。</summary>
    public Point GlassSize
    {
        get => (Point)GetValue(GlassSizeProperty);
        set => SetValue(GlassSizeProperty, value);
    }

    /// <summary>模糊强度。</summary>
    public float BlurIntensity
    {
        get => (float)GetValue(BlurIntensityProperty);
        set => SetValue(BlurIntensityProperty, value);
    }
}
