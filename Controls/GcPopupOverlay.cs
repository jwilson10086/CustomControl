using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace GeneralControl.Controls;

/// <summary>
/// 无边框弹窗底层：WindowStyle.None + AllowsTransparency + 半透明遮罩 + ESC 关闭。
///
/// 【为什么用 GcWindow 而非 Popup？】
/// Popup 会被父窗口裁剪、无法模态化；独立 GcWindow 可以：
/// - 设置 Owner，保证始终跟随主窗口（最小化/关闭联动）；
/// - ShowDialog() 实现真正的模态阻塞；
/// - AllowsTransparency + 无边框样式自由绘制毛玻璃卡片外观。
///
/// 【职责边界】
/// GcPopupOverlay 只负责"容器"：遮罩背景、居中、Owner、ESC 关闭。
/// 对话框内容（GcDialog / GcInputDialog）由外部作为 Content 传入，
/// 各自的样式模板负责自己的视觉呈现。
/// </summary>
public class GcPopupOverlay : GcWindow
{
    /// <summary>
    /// 构造函数：配置无边框透明窗口的基础属性。
    ///
    /// 【关键属性说明】
    /// - WindowStyle.None：去掉系统标题栏和边框；
    /// - AllowsTransparency=true：允许窗口整体透明（必须配合 WindowStyle.None）；
    /// - Background 半透明黑 (alpha=128)：形成遮罩层，突出居中的对话框内容；
    /// - SizeToContent.WidthAndHeight：窗口大小随内容自适应；
    /// - ShowInTaskbar=false：不在任务栏显示，避免干扰。
    /// </summary>
    public GcPopupOverlay()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0));
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SizeToContent = SizeToContent.WidthAndHeight;
        KeyDown += OnOverlayKeyDown;

        // 构造期（尚未显示）即锁定 Owner——此时调用方窗口必然处于激活态，
        // 时序安全。原先放在 OnSourceInitialized 中设置：ShowDialog 的
        // 句柄创建阶段与窗口激活切换存在竞争，会偶发
        // "无法在显示 GcDialog 之后设置 Owner 属性" 异常。
        var owner = Application.Current.Windows
            .OfType<GcWindow>()
            .FirstOrDefault(w => w.IsActive && !ReferenceEquals(w, this))
            ?? (Application.Current.MainWindow != this ? Application.Current.MainWindow : null);
        if (owner != null)
            Owner = owner;
    }

    /// <summary>
    /// ESC 键关闭弹窗，符合用户习惯。
    /// 使用冒泡的 KeyDown：焦点在内部任何按钮/输入框上时按下 ESC 都能被这里捕获。
    /// </summary>
    private void OnOverlayKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }
}



