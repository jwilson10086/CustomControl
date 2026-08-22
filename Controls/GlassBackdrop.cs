namespace CustomControl.Controls;

/// <summary>毛玻璃背景模式。</summary>
public enum GlassBackdrop
{
    /// <summary>自动：优先真实系统亚克力，失败时回退到模拟玻璃。</summary>
    Auto,

    /// <summary>纯 XAML 模拟玻璃（半透明渐变 + 模糊边缘高光），任何系统都可用，支持阴影。</summary>
    Simulated,

    /// <summary>液体玻璃：像素着色器 + 桌面背景捕获，最具 3D 质感的折射/色散效果。</summary>
    Glassy,

    /// <summary>Windows 10/11 真实系统亚克力（需允许 OS 背景材质）。</summary>
    Acrylic,

    /// <summary>Windows 11 亚克力 Mica 材质。</summary>
    Mica,
}
