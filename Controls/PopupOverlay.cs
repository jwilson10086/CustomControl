using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CustomControl.Controls;

/// <summary>
/// 无边框弹窗底层：WindowStyle.None + AllowsTransparency + 半透明遮罩 + ESC 关闭。
///
/// 【为什么用 Window 而非 Popup？】
/// Popup 会被父窗口裁剪、无法模态化；独立 Window 可以：
/// - 设置 Owner，保证始终跟随主窗口（最小化/关闭联动）；
/// - ShowDialog() 实现真正的模态阻塞；
/// - AllowsTransparency + 无边框样式自由绘制毛玻璃卡片外观。
///
/// 【职责边界】
/// PopupOverlay 只负责"容器"：遮罩背景、居中、Owner、ESC 关闭。
/// 对话框内容（GlassDialog / GlassInputDialog）由外部作为 Content 传入，
/// 各自的样式模板负责自己的视觉呈现。
/// </summary>
public class PopupOverlay : Window
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
    public PopupOverlay()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0));
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SizeToContent = SizeToContent.WidthAndHeight;
        KeyDown += OnOverlayKeyDown;
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

    /// <summary>
    /// 来源初始化完成时设置 Owner 为当前活动窗口。
    ///
    /// 【为什么在这里设置 Owner？】
    /// 构造时外部还没有机会指定 Owner（GlassDialog 只 new 了 PopupOverlay 就 Show），
    /// 而 Owner 必须在窗口显示前设置才有效。
    /// OnSourceInitialized 在窗口句柄创建后、显示前触发，是最后的安全时机。
    /// 这里自动取 Application 中当前激活的窗口作为 Owner，
    /// 保证弹窗居中于主窗口且随其最小化/关闭。
    /// </summary>
    protected override void OnSourceInitialized(System.EventArgs e)
    {
        base.OnSourceInitialized(e);
        var owner = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.IsActive && !ReferenceEquals(w, this));
        if (owner != null)
            Owner = owner;
    }
}
