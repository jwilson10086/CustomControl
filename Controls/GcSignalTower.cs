using System.Windows;
using System.Windows.Controls;

namespace GeneralControl.Controls;

/// <summary>警示灯点亮段。</summary>
public enum GcTowerLight
{
    /// <summary>全灭。</summary>
    Off,

    /// <summary>绿灯（正常运行）。</summary>
    Green,

    /// <summary>黄灯（警告/待机）。</summary>
    Yellow,

    /// <summary>红灯（报警/停机）。</summary>
    Red
}

/// <summary>
/// 工业三色警示灯控件（信号塔 Andon Tower）。
///
/// 【符号语义】
/// 立式三段灯塔：上红 / 中黄 / 下绿，由 <see cref="Light"/> 选择点亮段，
/// IsBlinking=true 时整塔灯段呼吸闪烁（报警常见形态）。
/// </summary>
public class GcSignalTower : Control
{
    static GcSignalTower()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcSignalTower), new FrameworkPropertyMetadata(typeof(GcSignalTower)));
    }

    public static readonly DependencyProperty LightProperty =
        DependencyProperty.Register(nameof(Light), typeof(GcTowerLight), typeof(GcSignalTower),
            new PropertyMetadata(GcTowerLight.Off));

    public static readonly DependencyProperty IsBlinkingProperty =
        DependencyProperty.Register(nameof(IsBlinking), typeof(bool), typeof(GcSignalTower),
            new PropertyMetadata(false));

    /// <summary>点亮的灯段：Off / Green / Yellow / Red。</summary>
    public GcTowerLight Light
    {
        get => (GcTowerLight)GetValue(LightProperty);
        set => SetValue(LightProperty, value);
    }

    /// <summary>是否闪烁（呼吸明暗）。</summary>
    public bool IsBlinking
    {
        get => (bool)GetValue(IsBlinkingProperty);
        set => SetValue(IsBlinkingProperty, value);
    }
}



