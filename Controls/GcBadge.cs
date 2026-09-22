using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace GeneralControl.Controls;

/// <summary>
/// 毛玻璃徽标（附加属性实现，参考 HandyControl 的 GcBadge）。
///
/// 【为什么用附加属性 + Adorner？】
/// 徽标需要"贴"在任意控件的右上角（按钮、头像、图标……）。
/// 如果做成包装控件，用户必须手动包一层；而附加属性可以直接写在目标元素上：
///     &lt;controls:GcButton controls:GcBadge.GcBadge="99+" /&gt;
/// 视觉部分由 Adorner（装饰器）承载：
/// Adorner 是覆盖在其他元素之上的独立图层，不占用布局空间，
/// 也不会影响被装饰元素的命中测试与排版。
///
/// 【显示规则】
/// 1. IsDot=true：只显示一个小红点（忽略 GcBadge 内容）；
/// 2. GcBadge 为空：隐藏；
/// 3. GcBadge 是数字且超过 MaxCount：显示 "99+" 形式。
/// </summary>
public static class GcBadge
{
    // ==================== 公开附加属性 ====================
    // 注意：附加属性的名称用字符串字面量注册。
    // 静态类里只有 Set*/Get* 方法，没有同名的实例成员可供 nameof 引用。

    /// <summary>徽标内容（数字或文字）。为空时隐藏徽标。</summary>
    public static readonly DependencyProperty BadgeProperty =
        DependencyProperty.RegisterAttached("GcBadge", typeof(object), typeof(GcBadge),
            new PropertyMetadata(null, OnBadgeChanged));

    /// <summary>是否以小红点模式显示（忽略 GcBadge 文本）。</summary>
    public static readonly DependencyProperty IsDotProperty =
        DependencyProperty.RegisterAttached("IsDot", typeof(bool), typeof(GcBadge),
            new PropertyMetadata(false, OnBadgeChanged));

    /// <summary>数字上限，超出后显示 "MaxCount+"。默认 99。</summary>
    public static readonly DependencyProperty MaxCountProperty =
        DependencyProperty.RegisterAttached("MaxCount", typeof(int), typeof(GcBadge),
            new PropertyMetadata(99, OnBadgeChanged));

    /// <summary>徽标背景色。默认警示红。</summary>
    public static readonly DependencyProperty BackgroundProperty =
        DependencyProperty.RegisterAttached("Background", typeof(Brush), typeof(GcBadge),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0xE8, 0x55, 0x55)), OnBadgeChanged));

    /// <summary>徽标前景（文字）色。</summary>
    public static readonly DependencyProperty ForegroundProperty =
        DependencyProperty.RegisterAttached("Foreground", typeof(Brush), typeof(GcBadge),
            new PropertyMetadata(Brushes.White, OnBadgeChanged));

    // ==================== 内部附加属性 ====================

    /// <summary>缓存已挂到元素上的装饰器实例，避免重复创建。</summary>
    private static readonly DependencyProperty AdornerProperty =
        DependencyProperty.RegisterAttached("Adorner", typeof(BadgeAdorner), typeof(GcBadge),
            new PropertyMetadata(null));

    // ==================== CLR 包装 ====================

    public static void SetBadge(DependencyObject obj, object value) => obj.SetValue(BadgeProperty, value);
    public static object GetBadge(DependencyObject obj) => obj.GetValue(BadgeProperty);

    public static void SetIsDot(DependencyObject obj, bool value) => obj.SetValue(IsDotProperty, value);
    public static bool GetIsDot(DependencyObject obj) => (bool)obj.GetValue(IsDotProperty);

    public static void SetMaxCount(DependencyObject obj, int value) => obj.SetValue(MaxCountProperty, value);
    public static int GetMaxCount(DependencyObject obj) => (int)obj.GetValue(MaxCountProperty);

    public static void SetBackground(DependencyObject obj, Brush value) => obj.SetValue(BackgroundProperty, value);
    public static Brush GetBackground(DependencyObject obj) => (Brush)obj.GetValue(BackgroundProperty);

    public static void SetForeground(DependencyObject obj, Brush value) => obj.SetValue(ForegroundProperty, value);
    public static Brush GetForeground(DependencyObject obj) => (Brush)obj.GetValue(ForegroundProperty);

    // ==================== 属性变化处理 ====================

    private static void OnBadgeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // IsLoaded / Loaded 定义在 FrameworkElement 上（UIElement 没有）。
        if (d is not FrameworkElement element)
        {
            return;
        }

        // 元素尚未进入可视树时无法获取 AdornerLayer，
        // 等 Loaded 后再挂装饰器（用 tag 防止重复挂钩）。
        if (!element.IsLoaded)
        {
            if ((bool)element.GetValue(LoadedHookedProperty))
            {
                return;
            }
            element.SetValue(LoadedHookedProperty, true);
            element.Loaded += (_, _) => Refresh(element);
            return;
        }

        Refresh(element);
    }

    private static readonly DependencyProperty LoadedHookedProperty =
        DependencyProperty.RegisterAttached("LoadedHooked", typeof(bool), typeof(GcBadge),
            new PropertyMetadata(false));

    /// <summary>创建或更新目标元素上的徽标装饰器。</summary>
    private static void Refresh(FrameworkElement element)
    {
        var adorner = (BadgeAdorner?)element.GetValue(AdornerProperty);

        if (adorner == null)
        {
            var layer = AdornerLayer.GetAdornerLayer(element);
            if (layer == null)
            {
                return; // 不在可视树中（例如还在模板里），跳过
            }

            adorner = new BadgeAdorner(element);
            element.SetValue(AdornerProperty, adorner);
            layer.Add(adorner);
        }

        adorner.Refresh(
            GetIsDot(element),
            GetBadge(element)?.ToString(),
            GetMaxCount(element),
            GetBackground(element),
            GetForeground(element));
    }

    // ==================== 装饰器 ====================

    /// <summary>
    /// 徽标装饰器：右上角悬浮的圆角胶囊（或圆点）。
    /// 纯代码构建视觉树，避免引入额外的模板资源依赖。
    /// </summary>
    private sealed class BadgeAdorner : Adorner
    {
        private readonly Border _border = new();
        private readonly GcTextBlock _text = new()
        {
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
        };

        public BadgeAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
            _border.Child = _text;
            _border.CornerRadius = new CornerRadius(999);
            _border.HorizontalAlignment = HorizontalAlignment.Center;
            _border.VerticalAlignment = VerticalAlignment.Center;
            AddVisualChild(_border);

            // 关键：Adorner 位于独立装饰层，不会自动跟随被装饰元素显隐。
            // 目标元素（或其祖先）折叠时必须手动隐藏徽标，
            // 否则切换页面后徽标会"悬浮"在原位置（IsVisible 会连同祖先一起计算）。
            adornedElement.IsVisibleChanged += (_, _) => UpdateVisibility();
        }

        /// <summary>徽标内容是否可见（由 Refresh 决定）。</summary>
        private bool _contentVisible;

        /// <summary>综合"内容有效"与"目标元素可见"两个条件决定最终显隐。</summary>
        private void UpdateVisibility()
        {
            _border.Visibility = _contentVisible && AdornedElement.IsVisible
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        /// <summary>根据附加属性值刷新徽标外观与可见性。</summary>
        public void Refresh(bool isDot, string? text, int maxCount, Brush background, Brush foreground)
        {
            _border.Background = background;
            _text.Foreground = foreground;

            if (isDot)
            {
                // 圆点模式：固定 8x8
                _border.MinWidth = 8;
                _border.Height = 8;
                _border.Padding = new Thickness(0);
                _text.Text = string.Empty;
                _contentVisible = true;
            }
            else if (string.IsNullOrEmpty(text))
            {
                _contentVisible = false;
            }
            else
            {
                // 数字超限显示 "99+"
                var display = text!;
                if (int.TryParse(text, out var number) && number > maxCount)
                {
                    display = $"{maxCount}+";
                }

                _text.Text = display;
                _border.MinWidth = 16;
                _border.Height = 16;
                _border.Padding = new Thickness(4, 0, 4, 0);
                _contentVisible = true;
            }

            InvalidateMeasure();
            UpdateVisibility();
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index) => _border;

        protected override Size MeasureOverride(Size constraint)
        {
            _border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return base.MeasureOverride(constraint);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var size = _border.DesiredSize;
            // 悬挂在被装饰元素右上角：向右下各溢出约一半，压住角点
            var x = finalSize.Width - size.Width * 0.62;
            var y = -size.Height * 0.38;
            _border.Arrange(new Rect(new Point(x, y), size));
            return finalSize;
        }
    }
}




