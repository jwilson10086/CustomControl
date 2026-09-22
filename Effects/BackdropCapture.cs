using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GeneralControl.Interop;

using GeneralControl.Controls;

namespace GeneralControl.Effects;

/// <summary>
/// 桌面背景捕获：捕获整个虚拟屏幕，并把窗口所在区域实时裁切为 ImageBrush，
/// 作为液体玻璃着色器的输入。
/// 参考：https://github.com/dragosniamtu/WPF-Liquid-Glass-Effect (MIT)
/// </summary>
internal sealed class GcBackdropCapture : IDisposable
{
    private static BitmapSource? _fullScreenSnapshot;
    private static int _virtualX;
    private static int _virtualY;
    private static int _virtualWidth;
    private static int _virtualHeight;

    private readonly GcWindow _window;
    private ImageBrush? _brush;
    private bool _disposed;

    public GcBackdropCapture(GcWindow window)
    {
        _window = window;
    }

    /// <summary>当前窗口背后的桌面快照画刷（按窗口位置实时裁切）。</summary>
    public ImageBrush Brush => _brush ??= new ImageBrush
    {
        Stretch = Stretch.Fill,
        AlignmentX = AlignmentX.Left,
        AlignmentY = AlignmentY.Top,
    };

    /// <summary>全屏快照（首次捕获后缓存）。</summary>
    public static BitmapSource? FullScreenSnapshot => _fullScreenSnapshot;

    /// <summary>捕获整个虚拟桌面。若窗口可见，先隐藏窗口再捕获以获得纯净背景。</summary>
    public void CaptureBehindWindow()
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(_window).Handle;
        if (hwnd != IntPtr.Zero)
        {
            _ = NativeMethods.ShowWindow(hwnd, NativeMethods.SW_HIDE);
            _window.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() => { }));
        }

        try
        {
            CaptureFullScreen();
        }
        finally
        {
            if (hwnd != IntPtr.Zero)
            {
                _ = NativeMethods.ShowWindow(hwnd, NativeMethods.SW_SHOWNOACTIVATE);
                _window.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() => { }));
            }
        }
    }

    /// <summary>
    /// 截取整个虚拟屏幕（含多显示器），保存为当前快照。
    /// 快照用于 GlassyBackdrop 在窗口移动/缩放时做窗口内容的模糊填充。
    /// </summary>
    public static void CaptureFullScreen()
    {
        _virtualX = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
        _virtualY = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);
        _virtualWidth = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
        _virtualHeight = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);

        var bitmap = CaptureScreen(_virtualX, _virtualY, _virtualWidth, _virtualHeight);
        if (bitmap is not null)
        {
            _fullScreenSnapshot = bitmap;
        }
    }

    /// <summary>按窗口当前屏幕位置更新背景画刷的裁剪区域。</summary>
    public void UpdateViewport()
    {
        if (_disposed || _fullScreenSnapshot is null || _window.WindowState == WindowState.Minimized)
        {
            return;
        }

        var topLeft = _window.PointToScreen(new Point(0, 0));
        var bottomRight = _window.PointToScreen(new Point(_window.ActualWidth, _window.ActualHeight));
        var x = (int)Math.Round(topLeft.X - _virtualX);
        var y = (int)Math.Round(topLeft.Y - _virtualY);
        var width = Math.Max(1, (int)Math.Round(bottomRight.X - topLeft.X));
        var height = Math.Max(1, (int)Math.Round(bottomRight.Y - topLeft.Y));

        if (x < 0) { width += x; x = 0; }
        if (y < 0) { height += y; y = 0; }
        if (x + width > _fullScreenSnapshot.PixelWidth) { width = _fullScreenSnapshot.PixelWidth - x; }
        if (y + height > _fullScreenSnapshot.PixelHeight) { height = _fullScreenSnapshot.PixelHeight - y; }
        if (width <= 0 || height <= 0)
        {
            return;
        }

        if (!ReferenceEquals(Brush.ImageSource, _fullScreenSnapshot))
        {
            Brush.ImageSource = _fullScreenSnapshot;
            Brush.ViewboxUnits = BrushMappingMode.Absolute;
        }

        Brush.Viewbox = new Rect(x, y, width, height);
    }

    private static BitmapSource? CaptureScreen(int x, int y, int width, int height)
    {
        var screenDc = IntPtr.Zero;
        var memDc = IntPtr.Zero;
        var hBitmap = IntPtr.Zero;
        var oldBitmap = IntPtr.Zero;

        try
        {
            screenDc = NativeMethods.GetDC(IntPtr.Zero);
            if (screenDc == IntPtr.Zero) return null;

            memDc = NativeMethods.CreateCompatibleDC(screenDc);
            if (memDc == IntPtr.Zero) return null;

            hBitmap = NativeMethods.CreateCompatibleBitmap(screenDc, width, height);
            if (hBitmap == IntPtr.Zero) return null;

            oldBitmap = NativeMethods.SelectObject(memDc, hBitmap);
            _ = NativeMethods.BitBlt(memDc, 0, 0, width, height, screenDc, x, y, NativeMethods.SRCCOPY);

            var bitmap = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(width, height));
            bitmap.Freeze();
            return bitmap;
        }
        finally
        {
            if (oldBitmap != IntPtr.Zero && memDc != IntPtr.Zero)
            {
                _ = NativeMethods.SelectObject(memDc, oldBitmap);
            }

            if (hBitmap != IntPtr.Zero) _ = NativeMethods.DeleteObject(hBitmap);
            if (memDc != IntPtr.Zero) _ = NativeMethods.DeleteDC(memDc);
            if (screenDc != IntPtr.Zero) _ = NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    public void Dispose()
    {
        _disposed = true;
    }
}



