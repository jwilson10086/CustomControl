using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace GeneralControl.Controls;

/// <summary>
/// 鱼眼效果行为：鼠标悬停时放大，类似 Mac Dock 的放大效果。
/// 附加到 ItemsPanel 或 StackPanel 上，子元素根据鼠标距离动态缩放。
///
/// 【实现原理】
/// 本类使用 WPF 附加属性（Attached Property）机制，将鱼眼行为"附加"到任意 GcPanel 上。
/// 当 IsEnabled 设为 true 时，监听面板的 MouseMove / MouseLeave 事件，
/// 在 MouseMove 中遍历所有子元素，根据鼠标与子元素中心点的水平距离计算缩放比例，
/// 并通过动画平滑地应用 ScaleTransform（缩放）和 TranslateTransform（位移偏移）。
///
/// 【设计决策】
/// - 使用静态类而非继承：这样可以附加到任意 GcPanel，不需要修改被附加控件的继承链。
/// - 使用附加属性而非普通静态字段：这样可以通过 XAML 绑定语法在样式/模板中使用。
/// - 使用 Transform 而非直接改 Width/Height：Transform 不影响布局，性能更好，且不会触发重排。
/// </summary>
public static class GcFishEye
{
    // ═══════════════════════════════════════════════════════════════
    // 附加属性定义区域
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 附加属性：是否启用鱼眼效果。
    /// 当设为 true 时，自动为面板注册鼠标事件处理程序；
    /// 设为 false 时，移除事件处理程序以避免不必要的开销。
    /// 默认值为 false，即不启用。
    /// </summary>
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(GcFishEye),
            new PropertyMetadata(false, OnIsEnabledChanged));

    /// <summary>
    /// 设置 IsEnabled 附加属性值的静态方法。
    /// WPF 附加属性约定：必须提供 SetXxx 和 GetXxx 静态方法，
    /// 以便 XAML 编译器和 SetValue/GetValue 机制使用。
    /// </summary>
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    /// <summary>
    /// 获取 IsEnabled 附加属性值的静态方法。
    /// </summary>
    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

    /// <summary>
    /// 附加属性：最大缩放比例。
    /// 鼠标正下方的子元素会被缩放到此值，距离越远缩放越小，直到回到 1.0（原始大小）。
    /// 默认值为 1.5，即最大放大到 150%。
    /// </summary>
    public static readonly DependencyProperty MaxScaleProperty =
        DependencyProperty.RegisterAttached("MaxScale", typeof(double), typeof(GcFishEye),
            new PropertyMetadata(1.5));

    public static void SetMaxScale(DependencyObject obj, double value) => obj.SetValue(MaxScaleProperty, value);
    public static double GetMaxScale(DependencyObject obj) => (double)obj.GetValue(MaxScaleProperty);

    /// <summary>
    /// 附加属性：鱼眼效果的作用范围（像素）。
    /// 只有鼠标与此子元素中心点水平距离在 Range 以内的子元素才会被放大。
    /// 距离为 0 时缩放到 MaxScale，距离为 Range 时缩放到 1.0（无缩放）。
    /// 默认值为 150 像素。
    /// </summary>
    public static readonly DependencyProperty RangeProperty =
        DependencyProperty.RegisterAttached("Range", typeof(double), typeof(GcFishEye),
            new PropertyMetadata(150.0));

    public static void SetRange(DependencyObject obj, double value) => obj.SetValue(RangeProperty, value);
    public static double GetRange(DependencyObject obj) => (double)obj.GetValue(RangeProperty);

    /// <summary>
    /// 附加属性：缩放/位移动画的持续时间。
    /// 所有 Transform 动画都会使用此 Duration，使得缩放和位移平滑过渡，
    /// 而非突然跳变，提升视觉体验。
    /// 默认值为 200 毫秒。
    /// </summary>
    public static readonly DependencyProperty AnimationDurationProperty =
        DependencyProperty.RegisterAttached("AnimationDuration", typeof(Duration), typeof(GcFishEye),
            new PropertyMetadata(new Duration(TimeSpan.FromMilliseconds(200))));

    public static void SetAnimationDuration(DependencyObject obj, Duration value) => obj.SetValue(AnimationDurationProperty, value);
    public static Duration GetAnimationDuration(DependencyObject obj) => (Duration)obj.GetValue(AnimationDurationProperty);

    // ═══════════════════════════════════════════════════════════════
    // 事件回调区域
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// IsEnabled 属性变更回调。
    /// 当附加属性值改变时，根据新值决定是注册还是注销面板的鼠标事件。
    /// 这是典型的 WPF 附加属性生命周期管理模式：
    ///   属性变更 → 注册事件 / 属性移除 → 注销事件
    /// 确保不在不需要时浪费性能处理鼠标事件。
    /// </summary>
    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // 只对 GcPanel 类型的控件生效（如 StackPanel、UniformGrid、WrapPanel 等）
        if (d is System.Windows.Controls.Panel panel)
        {
            if ((bool)e.NewValue)
            {
                // 启用：注册鼠标移动和离开事件
                panel.MouseMove += OnMouseMove;
                panel.MouseLeave += OnMouseLeave;
            }
            else
            {
                // 禁用：注销事件，停止监听
                panel.MouseMove -= OnMouseMove;
                panel.MouseLeave -= OnMouseLeave;
            }
        }
    }

    /// <summary>
    /// 鼠标在面板内移动时的核心逻辑。
    /// 遍历面板所有子元素，根据每个子元素中心点与鼠标位置的水平距离，
    /// 计算缩放比例，并通过动画平滑地应用 ScaleTransform 和 TranslateTransform。
    ///
    /// 【算法说明】
    /// 距离公式: distance = |mouseX - childCenterX|
    /// 缩放公式: scale = 1.0 + (maxScale - 1.0) * max(0, 1 - distance / range)
    ///   - 距离为 0 时：scale = maxScale（最大放大）
    ///   - 距离 = range 时：scale = 1.0（原始大小）
    ///   - 距离 > range 时：scale = 1.0（不在作用范围内，不缩放）
    /// 这产生了一个线性衰减的鱼眼效果：越靠近鼠标中心，放大越多。
    ///
    /// 【位移偏移】
    /// 为了让视觉效果更自然，子元素在被放大的同时会沿水平方向向外侧偏移，
    /// 模拟 Mac Dock 中元素被"推开"的效果，避免放大后的元素相互重叠。
    /// </summary>
    private static void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not System.Windows.Controls.Panel panel) return;

        // 获取鼠标相对于面板的位置
        var mousePos = e.GetPosition(panel);
        // 读取附加属性配置值
        var maxScale = GetMaxScale(panel);
        var range = GetRange(panel);
        var duration = GetAnimationDuration(panel);

        // 遍历面板中每一个子元素，逐个计算并应用缩放
        foreach (System.Windows.UIElement child in panel.Children)
        {
            // 跳过非 FrameworkElement（无法获取 ActualWidth/Height 的元素）
            if (child is not FrameworkElement fe) continue;

            // 计算子元素中心点（包含 Margin 的偏移）
            var childCenter = new Point(
                fe.ActualWidth / 2 + fe.Margin.Left,
                fe.ActualHeight / 2 + fe.Margin.Top);

            // 获取现有的 RenderTransform 组（可能尚未初始化）
            var transform = fe.RenderTransform as TransformGroup;
            var translate = transform?.Children[0] as TranslateTransform;

            // 将子元素中心点坐标转换到面板坐标系下
            // TranslatePoint 会考虑所有父级的变换，返回正确的屏幕坐标
            var childPos = fe.TranslatePoint(new Point(fe.ActualWidth / 2, fe.ActualHeight / 2), panel);

            // 只计算水平方向的距离（简化模型：忽略垂直方向差异）
            // 这使得效果类似 Mac Dock 的水平鱼眼，而非全方位的球形鱼眼
            var distance = Math.Abs(mousePos.X - childPos.X);

            // 缩放比例计算：距离越近放大越多，超出 range 则不缩放
            // 线性插值公式：从 range 处的 1.0 渐变到距离 0 处的 maxScale
            var scale = 1.0 + (maxScale - 1.0) * Math.Max(0, 1.0 - distance / range);

            // 获取或创建该子元素的 ScaleTransform 和 TranslateTransform
            var scaleTransform = GetScaleTransform(fe);
            var tx = GetTranslateTransform(fe);

            // 创建三个动画：X 偏移动画、Y 偏移动画（固定为 0）、缩放动画
            // 使用 CubicEase + EaseOut 提供平滑的减速效果，
            // 比线性动画更自然，符合物理直觉（快速响应、缓慢到位）
            var animX = new DoubleAnimation(0, duration) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            var animY = new DoubleAnimation(0, duration) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            var animS = new DoubleAnimation(scale, duration) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };

            // 位移偏移：让被放大的子元素向远离鼠标的方向偏移
            // 乘以 0.3 是一个视觉调优系数，使偏移不至于太夸张
            // scale - 1 是缩放增量（例如 scale=1.5 时增量为 0.5）
            // 乘以 ActualWidth 确保偏移量与元素大小成比例
            if (childCenter.X < mousePos.X)
                // 鼠标在元素右侧 → 元素向左偏移（远离鼠标）
                animX.To = -(scale - 1) * fe.ActualWidth * 0.3;
            else
                // 鼠标在元素左侧 → 元素向右偏移（远离鼠标）
                animX.To = (scale - 1) * fe.ActualWidth * 0.3;

            // 将动画应用到对应的 Transform 属性上
            // BeginAnimation 会自动在当前值和目标值之间插值
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animS);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animS);
            tx.BeginAnimation(TranslateTransform.XProperty, animX);
        }
    }

    /// <summary>
    /// 鼠标离开面板时的重置逻辑。
    /// 将所有子元素的缩放和位移动画恢复到默认值（scale=1, translateX=0），
    /// 使面板平滑地恢复到初始状态。
    /// </summary>
    private static void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is not System.Windows.Controls.Panel panel) return;

        var duration = GetAnimationDuration(panel);

        foreach (System.Windows.UIElement child in panel.Children)
        {
            if (child is not FrameworkElement fe) continue;

            var scaleTransform = GetScaleTransform(fe);
            var tx = GetTranslateTransform(fe);

            // 缩放恢复到 1.0（原始大小）
            var animS = new DoubleAnimation(1.0, duration) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            // 水平位移恢复到 0（原始位置）
            var animX = new DoubleAnimation(0, duration) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animS);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animS);
            tx.BeginAnimation(TranslateTransform.XProperty, animX);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Transform 辅助方法区域
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 获取或为子元素创建 ScaleTransform（缩放变换）。
    ///
    /// 【为什么需要缓存 Transform？】
    /// WPF 的 RenderTransform 是只读引用，每次 BeginAnimation 会替换整个动画。
    /// 如果每次都创建新的 ScaleTransform，会导致动画中断和内存浪费。
    /// 通过 RenderTransform 缓存同一个 Transform 实例，
    /// 可以复用动画，且多次 MouseMove 事件之间动画可以平滑衔接。
    ///
    /// 【TransformGroup 顺序】
    /// 组内顺序为 [TranslateTransform, ScaleTransform]。
    /// WPF 按数组逆序应用变换，即先 Scale 后 Translate。
    /// 这意味着先在本地坐标系中缩放，再在缩放后的基础上平移，
    /// 确保平移量不会被缩放放大。
    /// </summary>
    private static ScaleTransform GetScaleTransform(FrameworkElement fe)
    {
        // 如果已经有 RenderTransform 且是 TransformGroup，尝试从中找到已有的 ScaleTransform
        if (fe.RenderTransform is TransformGroup group)
        {
            foreach (var t in group.Children)
                if (t is ScaleTransform st) return st;
        }

        // 首次调用：创建完整的 TransformGroup 并赋值给 RenderTransform
        var newGroup = new TransformGroup();
        var newScale = new ScaleTransform(1, 1);   // 初始缩放为 1:1（不变形）
        var newTranslate = new TranslateTransform(); // 初始平移为 (0, 0)
        newGroup.Children.Add(newTranslate);        // 先添加 Translate（变换时后应用）
        newGroup.Children.Add(newScale);            // 再添加 Scale（变换时先应用）
        fe.RenderTransform = newGroup;
        // RenderTransformOrigin 设为 (0.5, 0.5) 表示以元素中心为缩放原点
        // 如果不设置，缩放默认以左上角 (0, 0) 为中心，效果会偏移
        fe.RenderTransformOrigin = new Point(0.5, 0.5);
        return newScale;
    }

    /// <summary>
    /// 获取或为子元素创建 TranslateTransform（平移变换）。
    /// 与 GetScaleTransform 配合使用，确保两者共存于同一个 TransformGroup 中。
    /// </summary>
    private static TranslateTransform GetTranslateTransform(FrameworkElement fe)
    {
        // 如果已有 TransformGroup，尝试从中找到已有的 TranslateTransform
        if (fe.RenderTransform is TransformGroup group)
        {
            foreach (var t in group.Children)
                if (t is TranslateTransform tt) return tt;
        }

        // 如果没找到，调用 GetScaleTransform 确保 TransformGroup 已初始化
        GetScaleTransform(fe);
        // 从刚初始化的 TransformGroup 中取出第一个元素（即 TranslateTransform）
        // 使用 null-conditional 和 ?? 处理极端情况（理论上不会发生）
        return (fe.RenderTransform as TransformGroup)!.Children[0] as TranslateTransform
               ?? new TranslateTransform();
    }
}



