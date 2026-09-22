using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

using GeneralControl.Controls;

namespace GeneralControl.Interop;

/// <summary>
/// Win32/DWM/GDI 原生调用集合：窗口拖动、缩放、亚克力背景、系统背景材质、屏幕捕获。
/// </summary>
internal static class NativeMethods
{
    // --- 窗口消息 ---
    public const int WM_NCLBUTTONDOWN = 0x00A1;
    public const int WM_SYSCOMMAND = 0x0112;
    public const int HT_CAPTION = 2;
    public const int SC_SIZE = 0xF000;

    // --- 缩放方向 ---
    public const int WMSZ_LEFT = 1;
    public const int WMSZ_RIGHT = 2;
    public const int WMSZ_TOP = 3;
    public const int WMSZ_TOPLEFT = 4;
    public const int WMSZ_TOPRIGHT = 5;
    public const int WMSZ_BOTTOM = 6;
    public const int WMSZ_BOTTOMLEFT = 7;
    public const int WMSZ_BOTTOMRIGHT = 8;

    // --- 虚拟屏幕指标 ---
    public const int SM_XVIRTUALSCREEN = 76;
    public const int SM_YVIRTUALSCREEN = 77;
    public const int SM_CXVIRTUALSCREEN = 78;
    public const int SM_CYVIRTUALSCREEN = 79;

    public const int SW_HIDE = 0;
    public const int SW_SHOWNOACTIVATE = 4;

    // --- GDI ---
    public const int SRCCOPY = 0x00CC0020;

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    public static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")]
    public static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int width, int height);

    [DllImport("gdi32.dll")]
    public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    public static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int width, int height, IntPtr hdcSrc, int xSrc, int ySrc, int rop);

    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    /// <summary>从 WPF 元素直接发出系统命令拖动窗口。</summary>
    public static void DragMove(GcWindow window)
    {
        if (window is null)
        {
            return;
        }

        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        _ = ReleaseCapture();
        _ = SendMessage(hwnd, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
    }

    public static void BeginResize(GcWindow window, int direction)
    {
        if (window is null)
        {
            return;
        }

        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        _ = SendMessage(hwnd, WM_SYSCOMMAND, (IntPtr)(SC_SIZE | direction), IntPtr.Zero);
    }

    /// <summary>Win10/11 亚克力模糊（SetWindowCompositionAttribute）。</summary>
    public static bool SetAcrylic(GcWindow window, bool enabled, byte opacity, byte r, byte g, byte b)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        var accent = new AccentPolicy
        {
            AccentState = enabled ? (int)AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND : (int)AccentState.ACCENT_DISABLED,
            AccentFlags = 2, // ACCENT_FLAG_DRAW_ALL_BORDERS 兼容
            GradientColor = ((uint)opacity << 24) | ((uint)r << 16) | ((uint)g << 8) | b,
        };

        var data = new WindowCompositionAttributeData
        {
            Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
            Data = Marshal.AllocHGlobal(Marshal.SizeOf<AccentPolicy>()),
            SizeOfData = Marshal.SizeOf<AccentPolicy>(),
        };

        try
        {
            Marshal.StructureToPtr(accent, data.Data, false);
            return SetWindowCompositionAttribute(hwnd, ref data) == 0;
        }
        finally
        {
            Marshal.FreeHGlobal(data.Data);
        }
    }

    /// <summary>Windows 11 系统背景材质（Mica / Acrylic / Tabbed）。</summary>
    public static bool SetSystemBackdrop(GcWindow window, SystemBackdropType type)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        int value = (int)type;
        return DwmSetWindowAttribute(hwnd, DwmAttributes.DWMWA_SYSTEMBACKDROP_TYPE, ref value, sizeof(int)) == 0;
    }

}



