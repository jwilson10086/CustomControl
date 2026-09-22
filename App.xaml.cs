using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace GeneralControl;

public partial class App : Application
{
    private static readonly string LogPath = Path.Combine(Path.GetTempPath(), "GeneralControl.err.log");

    public App()
    {
        // 部分机器(如带虚拟显示适配器/远程桌面)上 WPF GPU 渲染路径异常会导致窗口空白，
        // 强制 WARP 软件渲染保证所有环境可见。
        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        File.WriteAllText(LogPath, e.Exception.ToString());
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        File.WriteAllText(LogPath, e.ExceptionObject?.ToString() ?? "unknown exception");
    }
}

