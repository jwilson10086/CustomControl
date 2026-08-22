using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace CustomControl.Controls;

/// <summary>
/// 毛玻璃密码输入框的附加属性（Attached Properties）。
///
/// 【为什么用附加属性而不是子类化？】
/// WPF 中的 PasswordBox 是一个密封类（sealed），不允许被继承。
/// 这是因为 PasswordBox 内部出于安全考虑做了特殊设计：
///   - Password 属性没有依赖属性支持（无法绑定，防止密码被意外暴露）。
///   - 类被标记为 sealed，防止子类绕过安全限制。
///
/// 因此，要为 PasswordBox 添加 Placeholder 等额外功能，
/// 唯一的扩展方式就是使用附加属性（Attached Properties）。
///
/// 【附加属性的本质】
/// 附加属性允许"在别人的控件上附加自己的属性"，
/// 典型的例子就是 Grid.Row、Canvas.Left 等。
/// 附加属性本质上也是依赖属性，只是注册方式不同（RegisterAttached）。
/// 使用时需要配套提供 Get/Set 静态方法。
///
/// 【与之搭配的使用方式】
/// 在 XAML 中：
///   &lt;PasswordBox controls:GlassPasswordBox.Placeholder="请输入密码"
///                  controls:GlassPasswordBox.PlaceholderBrush="{Binding ...}"
///                  Style="{StaticResource GlassPasswordBoxStyle}" /&gt;
/// </summary>
public static class GlassPasswordBox
{
    /// <summary>
    /// 密码框占位提示文本的附加属性。
    /// 【RegisterAttached vs Register】
    /// RegisterAttached 注册的属性可以附加到任何 DependencyObject 上，
    /// 而 Register 注册的属性只能用于定义它的那个类型及其派生类。
    /// </summary>
    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.RegisterAttached(
            "Placeholder", typeof(string), typeof(GlassPasswordBox), new PropertyMetadata(string.Empty));

    /// <summary>
    /// 密码框占位提示文本颜色的附加属性。
    /// 默认使用 Brushes.Transparent（完全透明），即初始不显示颜色。
    /// 控件模板中可根据文本框是否为空来决定是否显示该画刷。
    /// </summary>
    public static readonly DependencyProperty PlaceholderBrushProperty =
        DependencyProperty.RegisterAttached(
            "PlaceholderBrush", typeof(Brush), typeof(GlassPasswordBox), new PropertyMetadata(Brushes.Transparent));

    /// <summary>获取占位提示文本。</summary>
    public static string GetPlaceholder(DependencyObject obj) => (string)obj.GetValue(PlaceholderProperty);
    /// <summary>设置占位提示文本。</summary>
    public static void SetPlaceholder(DependencyObject obj, string value) => obj.SetValue(PlaceholderProperty, value);

    /// <summary>获取占位提示文本颜色。</summary>
    public static Brush GetPlaceholderBrush(DependencyObject obj) => (Brush)obj.GetValue(PlaceholderBrushProperty);
    /// <summary>设置占位提示文本颜色。</summary>
    public static void SetPlaceholderBrush(DependencyObject obj, Brush value) => obj.SetValue(PlaceholderBrushProperty, value);

    /// <summary>
    /// 密码是否可见的附加属性（用于"显示/隐藏密码"切换功能）。
    ///
    /// 【工作原理】
    /// 当 IsPasswordVisible 为 true 时，控件模板中的 Trigger 会将
    /// PasswordBox 的 PasswordChar 属性清空（显示明文）；
    /// 为 false 时，恢复为 "*" 或 "●" 等遮挡字符。
    /// 因为 PasswordBox.Password 不支持绑定，所以这个切换
    /// 通常需要在后台代码中通过 VisualStateManager 或 Trigger 处理。
    /// </summary>
    public static readonly DependencyProperty IsPasswordVisibleProperty =
        DependencyProperty.RegisterAttached(
            "IsPasswordVisible", typeof(bool), typeof(GlassPasswordBox),
            new PropertyMetadata(false, OnIsPasswordVisibleChanged));

    /// <summary>获取密码是否可见。</summary>
    public static bool GetIsPasswordVisible(DependencyObject obj) => (bool)obj.GetValue(IsPasswordVisibleProperty);
    /// <summary>设置密码是否可见。</summary>
    public static void SetIsPasswordVisible(DependencyObject obj, bool value) => obj.SetValue(IsPasswordVisibleProperty, value);

    /// <summary>
    /// IsPasswordVisible 变更回调。
    /// 当设置为 true 时，给 PasswordBox 注册 Loaded 事件，
    /// 在模板加载完成后找到眼睛按钮并绑定点击事件。
    /// </summary>
    private static void OnIsPasswordVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PasswordBox passwordBox && e.NewValue is true)
        {
            passwordBox.Loaded -= PasswordBox_Loaded;
            passwordBox.Loaded += PasswordBox_Loaded;
        }
    }

    private static void PasswordBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox passwordBox) return;

         passwordBox.Dispatcher.BeginInvoke(new Action(() =>
    {
        var toggleButton = FindChild<Button>(passwordBox, "PART_TogglePasswordButton");
        var plainTextBox = FindChild<TextBox>(passwordBox, "PART_PlainTextBox");
        var contentHost = FindChild<ScrollViewer>(passwordBox, "PART_ContentHost");
        if (toggleButton == null || plainTextBox == null) return;

        // 重置 TextBox——清掉默认 Style 避免布局干扰
        plainTextBox.SetValue(Control.StyleProperty, null);
        plainTextBox.Background = Brushes.Transparent;
        plainTextBox.BorderBrush = Brushes.Transparent;
        plainTextBox.BorderThickness = new Thickness(0);
        plainTextBox.Margin = passwordBox.Margin;
        plainTextBox.Padding = passwordBox.Padding;
        plainTextBox.VerticalContentAlignment = passwordBox.VerticalContentAlignment;
        plainTextBox.Foreground = passwordBox.Foreground;
        plainTextBox.FontFamily = passwordBox.FontFamily;
        plainTextBox.FontSize = passwordBox.FontSize;
        plainTextBox.FontWeight = passwordBox.FontWeight;
        plainTextBox.Width = passwordBox.ActualWidth;
        plainTextBox.Height = passwordBox.ActualHeight;
        plainTextBox.Margin = new Thickness(0); // 清掉 Margin，让它直接叠在 ContentHost 上

        var originalChar = passwordBox.PasswordChar;
        bool isShowingPlain = false;

        toggleButton.PreviewMouseLeftButtonDown += (s, me) =>
            {
                if (!isShowingPlain)
                {
                    plainTextBox.Text = passwordBox.Password;
                    plainTextBox.Visibility = Visibility.Visible;
                    plainTextBox.IsHitTestVisible = true;
                    plainTextBox.Focusable = true;
                    if (contentHost != null) contentHost.Visibility = Visibility.Collapsed;
                    plainTextBox.Focus();
                    if (contentHost != null)
                    {
                        var hostPos = contentHost.TranslatePoint(new Point(0, 0), passwordBox);
                        var textPos = plainTextBox.TranslatePoint(new Point(0, 0), passwordBox);
                        Debug.WriteLine($"ContentHost vs TextBox: Host={hostPos}, Text={textPos}");
                    }
                    plainTextBox.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        System.Diagnostics.Debug.WriteLine("===== PasswordBox vs PlainTextBox =====");
                        System.Diagnostics.Debug.WriteLine($"[Margin]        PB={passwordBox.Margin}  TB={plainTextBox.Margin}");
                        System.Diagnostics.Debug.WriteLine($"[Padding]       PB={passwordBox.Padding}  TB={plainTextBox.Padding}");
                        System.Diagnostics.Debug.WriteLine($"[ActualWidth]   PB={passwordBox.ActualWidth}  TB={plainTextBox.ActualWidth}");
                        System.Diagnostics.Debug.WriteLine($"[ActualHeight]  PB={passwordBox.ActualHeight}  TB={plainTextBox.ActualHeight}");
                        System.Diagnostics.Debug.WriteLine($"[RenderSize]    PB={passwordBox.RenderSize}  TB={plainTextBox.RenderSize}");
                        System.Diagnostics.Debug.WriteLine($"[VertAlign]     PB={passwordBox.VerticalAlignment}  TB={plainTextBox.VerticalAlignment}");
                        System.Diagnostics.Debug.WriteLine($"[VertContent]   PB={passwordBox.VerticalContentAlignment}  TB={plainTextBox.VerticalContentAlignment}");
                        System.Diagnostics.Debug.WriteLine($"[Location]      PB={passwordBox.TranslatePoint(new Point(0,0), (UIElement)passwordBox.Parent)}  TB={plainTextBox.TranslatePoint(new Point(0,0), (UIElement)plainTextBox.Parent)}");
                        System.Diagnostics.Debug.WriteLine("========================================");
                    }), DispatcherPriority.Render);
                }
                else
                {
                    passwordBox.Password = plainTextBox.Text;
                    plainTextBox.Visibility = Visibility.Collapsed;
                    plainTextBox.IsHitTestVisible = false;
                    plainTextBox.Focusable = false;
                    plainTextBox.Text = string.Empty;
                    if (contentHost != null) contentHost.Visibility = Visibility.Visible;
                    passwordBox.Focus();
                }

                isShowingPlain = !isShowingPlain;

                var eyeBorder = FindChild<Border>(toggleButton, "EyeBorder");
                if (eyeBorder != null) eyeBorder.Opacity = isShowingPlain ? 1.0 : 0.6;

                me.Handled = true;
            };
        }), DispatcherPriority.Loaded);
    }

    /// <summary>
    /// 递归在视觉树中查找指定名称的子元素。
    /// 用于跨模板 namescope 查找元素（FindName 只能在同一 namescope 内查找）。
    /// </summary>
    private static T? FindChild<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T element && element.Name == name)
                return element;
            var result = FindChild<T>(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
