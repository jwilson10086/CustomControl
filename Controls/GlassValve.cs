using System.Windows;
using System.Windows.Controls;

namespace CustomControl.Controls;

/// <summary>
/// 工业阀门控件（P&amp;ID 蝶阀符号）。
///
/// 【符号语义】
/// 对角蝶形 = 阀体；顶部方块 = 执行器（Actuator）；竖线 = 阀杆。
///
/// 【状态模型】
/// - IsOpen=true ：蝶形通道打开，填充主题强调色（介质可通过）；
/// - IsOpen=false：蝶形关闭，暗色填充；
/// - IsFault=true：整体红色警示（故障优先级最高，覆盖开关色）。
/// </summary>
public class GlassValve : Control
{
    static GlassValve()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassValve), new FrameworkPropertyMetadata(typeof(GlassValve)));
    }

    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(GlassValve),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsFaultProperty =
        DependencyProperty.Register(nameof(IsFault), typeof(bool), typeof(GlassValve),
            new PropertyMetadata(false));

    /// <summary>阀门是否打开。</summary>
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>是否故障（红色警示，优先于开关状态显示）。</summary>
    public bool IsFault
    {
        get => (bool)GetValue(IsFaultProperty);
        set => SetValue(IsFaultProperty, value);
    }
}
