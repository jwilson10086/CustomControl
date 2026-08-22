using System.Runtime.InteropServices;

namespace CustomControl.Interop;

/// <summary>
/// 窗口亚克力（毛玻璃）配置策略，对应 SetWindowCompositionAttribute 的 ACCENT_POLICY。
/// 参考：https://github.com/achampagne1/AcrylicBackgroundLib (MIT)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct AccentPolicy
{
    public int AccentState;
    public uint AccentFlags;
    public uint GradientColor;
}

/// <summary>Win10 支持的亚克力背景状态。</summary>
internal enum AccentState
{
    ACCENT_DISABLED = 0,
    ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
}

internal enum WindowCompositionAttribute
{
    WCA_ACCENT_POLICY = 19,
}

[StructLayout(LayoutKind.Sequential)]
internal struct WindowCompositionAttributeData
{
    public WindowCompositionAttribute Attribute;
    public IntPtr Data;
    public int SizeOfData;
}

/// <summary>DWM 窗口属性（Windows 10/11）。</summary>
internal static class DwmAttributes
{
    /// <summary>DWMWA_SYSTEMBACKDROP_TYPE (38) - Windows 11 系统背景材质。</summary>
    public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
}

/// <summary>Windows 11 系统背景材质类型 (DWMWA_SYSTEMBACKDROP_TYPE)。</summary>
internal enum SystemBackdropType
{
    Auto = 0,
    None = 1,
    Mica = 2,
    Acrylic = 3,
}
