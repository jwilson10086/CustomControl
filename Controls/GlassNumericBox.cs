using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace CustomControl.Controls;

/// <summary>
/// 毛玻璃数字输入框：文本输入 + 加减按钮（支持长按连发）。
///
/// 【为什么不继承 TextBox？】
/// 数字框 = 文本编辑区 + 步进按钮的组合控件。
/// 继承 Control 并在模板里内嵌一个原生 TextBox，
/// 可以自由排版按钮位置，也避免重写 TextBox 的大量键盘/剪贴板行为。
///
/// 【值同步策略】
/// - Value → Text：属性回调里格式化写入（带 _syncing 标志防止回环）；
/// - Text → Value：失焦或按回车时解析，解析失败回滚为上次的合法值；
/// - 所有赋值都经过 Clamp + Round，避免浮点累加误差（0.1+0.2 问题）。
/// </summary>
[TemplatePart(Name = PartTextBox, Type = typeof(TextBox))]
[TemplatePart(Name = PartDecreaseButton, Type = typeof(RepeatButton))]
[TemplatePart(Name = PartIncreaseButton, Type = typeof(RepeatButton))]
public class GlassNumericBox : Control
{
    private const string PartTextBox = "PART_TextBox";
    private const string PartDecreaseButton = "PART_DecreaseButton";
    private const string PartIncreaseButton = "PART_IncreaseButton";

    static GlassNumericBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassNumericBox), new FrameworkPropertyMetadata(typeof(GlassNumericBox)));
    }

    // ==================== 依赖属性 ====================

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(GlassNumericBox),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnValueChanged));

    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(GlassNumericBox),
            new PropertyMetadata(0.0, OnRangeChanged));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(GlassNumericBox),
            new PropertyMetadata(double.MaxValue, OnRangeChanged));

    public static readonly DependencyProperty IncrementProperty =
        DependencyProperty.Register(nameof(Increment), typeof(double), typeof(GlassNumericBox),
            new PropertyMetadata(1.0));

    public static readonly DependencyProperty DecimalPlacesProperty =
        DependencyProperty.Register(nameof(DecimalPlaces), typeof(int), typeof(GlassNumericBox),
            new PropertyMetadata(0, OnValueChanged));

    /// <summary>当前值。</summary>
    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>最小值。</summary>
    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>最大值。</summary>
    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>每次步进的增量。</summary>
    public double Increment
    {
        get => (double)GetValue(IncrementProperty);
        set => SetValue(IncrementProperty, value);
    }

    /// <summary>小数位数（0 表示整数模式）。</summary>
    public int DecimalPlaces
    {
        get => (int)GetValue(DecimalPlacesProperty);
        set => SetValue(DecimalPlacesProperty, value);
    }

    // ==================== 内部状态 ====================

    private TextBox? _textBox;
    private RepeatButton? _decreaseButton;
    private RepeatButton? _increaseButton;
    private bool _syncing;

    // ==================== 模板应用 ====================

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_textBox != null)
        {
            _textBox.LostFocus -= OnTextCommitted;
            _textBox.KeyDown -= OnTextBoxKeyDown;
        }

        if (_decreaseButton != null)
        {
            _decreaseButton.Click -= OnDecreaseClick;
        }

        if (_increaseButton != null)
        {
            _increaseButton.Click -= OnIncreaseClick;
        }

        _textBox = GetTemplateChild(PartTextBox) as TextBox;
        _decreaseButton = GetTemplateChild(PartDecreaseButton) as RepeatButton;
        _increaseButton = GetTemplateChild(PartIncreaseButton) as RepeatButton;

        if (_textBox != null)
        {
            _textBox.LostFocus += OnTextCommitted;
            _textBox.KeyDown += OnTextBoxKeyDown;
            SyncTextFromValue();
        }

        if (_decreaseButton != null)
        {
            _decreaseButton.Click += OnDecreaseClick;
        }

        if (_increaseButton != null)
        {
            _increaseButton.Click += OnIncreaseClick;
        }
    }

    // ==================== 属性回调 ====================

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var box = (GlassNumericBox)d;
        box.CoerceValue();
        box.SyncTextFromValue();
    }

    /// <summary>范围变化时把当前值夹回合法区间。</summary>
    private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var box = (GlassNumericBox)d;
        box.CoerceValue();
        box.SyncTextFromValue();
    }

    // ==================== 数值逻辑 ====================

    private void CoerceValue()
    {
        var clamped = Math.Clamp(Value, Minimum, Maximum);
        var rounded = Math.Round(clamped, DecimalPlaces);

        if (!AreClose(rounded, Value))
        {
            _syncing = true;
            try
            {
                SetCurrentValue(ValueProperty, rounded);
            }
            finally
            {
                _syncing = false;
            }
        }
    }

    private void StepValue(double direction)
    {
        var next = Math.Round(Value + direction * Increment, DecimalPlaces);
        Value = Math.Clamp(next, Minimum, Maximum);

        // 步进后让文本框保持全选，方便连续输入覆盖
        if (_textBox != null && IsKeyboardFocusWithin)
        {
            _textBox.SelectAll();
        }
    }

    private void SyncTextFromValue()
    {
        if (_textBox == null || _syncing)
        {
            return;
        }

        _syncing = true;
        try
        {
            _textBox.Text = Value.ToString($"F{DecimalPlaces}", CultureInfo.InvariantCulture);
        }
        finally
        {
            _syncing = false;
        }
    }

    private void CommitText()
    {
        if (_textBox == null)
        {
            return;
        }

        var raw = _textBox.Text.Trim().Replace(',', '.');

        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            Value = Math.Clamp(parsed, Minimum, Maximum);
        }

        // 无论成功与否都回写一次，失败即回滚显示旧值
        SyncTextFromValue();
    }

    private static bool AreClose(double a, double b) => Math.Abs(a - b) < 1e-9;

    // ==================== 事件处理 ====================

    private void OnIncreaseClick(object sender, RoutedEventArgs e) => StepValue(+1);

    private void OnDecreaseClick(object sender, RoutedEventArgs e) => StepValue(-1);

    private void OnTextCommitted(object sender, RoutedEventArgs e) => CommitText();

    private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                CommitText();
                e.Handled = true;
                break;
            case Key.Up:
                StepValue(+1);
                e.Handled = true;
                break;
            case Key.Down:
                StepValue(-1);
                e.Handled = true;
                break;
        }
    }
}
