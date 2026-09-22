using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace GeneralControl.Controls;

/// <summary>弹出式键盘控件：包含预览框、按键网格、确认/取消按钮。</summary>
public class GcKeyboardPopup : ContentControl
{
    private Popup? _popup;
    private System.Windows.Controls.TextBox? _previewBox;
    private string _inputValue = "";
    private FrameworkElement? _target;
    private bool _isUpperCase;
    private bool _isSymbolMode;
    private System.Windows.Controls.Panel? _keysHost;
    private System.Windows.Controls.Slider? _rangeSlider;
    private bool _syncingSlider;
    private readonly List<System.Windows.Controls.Button> _alphaKeys = new();

    static GcKeyboardPopup()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcKeyboardPopup), new FrameworkPropertyMetadata(typeof(GcKeyboardPopup)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _previewBox = GetTemplateChild("PART_PreviewBox") as System.Windows.Controls.TextBox;

        if (GetTemplateChild("PART_ConfirmButton") is System.Windows.Controls.Button confirmBtn)
            confirmBtn.Click += OnConfirm;
        if (GetTemplateChild("PART_CancelButton") is System.Windows.Controls.Button cancelBtn)
            cancelBtn.Click += OnCancel;
        if (GetTemplateChild("PART_ClearButton") is System.Windows.Controls.Button clearBtn)
            clearBtn.Click += OnClear;

        SetupKeys();
        UpdatePreview();
    }

    public void ShowFor(FrameworkElement target, GcKeyboardType type, double? min, double? max, int decimalPlaces)
    {
        _target = target;
        _inputValue = target is System.Windows.Controls.TextBox tb ? tb.Text : "";
        KeyboardMode = type;
        MinValue = min;
        MaxValue = max;
        DecimalPlaces = decimalPlaces;
        _isUpperCase = false;
        _isSymbolMode = false;
        UpdatePreview();

        _popup = new Popup
        {
            Child = this,
            PlacementTarget = target,
            Placement = PlacementMode.Bottom,
            AllowsTransparency = true,
            StaysOpen = true,
            PopupAnimation = PopupAnimation.Fade,
        };
        _popup.IsOpen = true;
        Focus();
    }

    public void ClosePopup()
    {
        if (_popup != null && _popup.IsOpen)
            _popup.IsOpen = false;
    }

    private void SetupKeys()
    {
        _keysHost = GetTemplateChild("PART_KeysHost") as System.Windows.Controls.Panel;
        if (_keysHost == null) return;
        _keysHost.Children.Clear();
        _alphaKeys.Clear();

        if (KeyboardMode == GcKeyboardType.Numeric)
            BuildNumericKeys(_keysHost);
        else
            BuildAlphaKeys(_keysHost);
    }

    private void RebuildAlphaKeys()
    {
        if (_keysHost == null) return;
        _keysHost.Children.Clear();
        _alphaKeys.Clear();
        BuildAlphaKeys(_keysHost);
    }

    private void BuildNumericKeys(System.Windows.Controls.Panel host)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });


        var keys = new[,] {
            { "1", "2", "3" },
            { "4", "5", "6" },
            { "7", "8", "9" },
            { "-", "0", "." },
        };
        for (var r = 0; r < 4; r++)
        {
            for (var c = 0; c < 3; c++)
            {
                var val = keys[r, c];
                if (val == "." && DecimalPlaces <= 0) continue;
                var btn = CreateKey(val);
                btn.HorizontalAlignment = HorizontalAlignment.Stretch;
                btn.VerticalAlignment = VerticalAlignment.Stretch;
                Grid.SetRow(btn, r + 1);
                Grid.SetColumn(btn, c);
                grid.Children.Add(btn);
            }
        }

        var backBtn = new GcButton
        {
            Content = "\u232B \u9000\u683C",
            Tag = "backspace",
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
            Cursor = Cursors.Hand,
            FocusVisualStyle = null,
            Margin = new Thickness(2),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        ApplyKeyStyle(backBtn);
        backBtn.Click += OnBackspace;
        Grid.SetRow(backBtn, 1);
        Grid.SetColumn(backBtn, 3);
        grid.Children.Add(backBtn);

        var minVal = MinValue ?? 0d;
        var maxVal = MaxValue ?? 100d;

        var minLabel = new System.Windows.Controls.TextBlock
        {
            Text = minVal.ToString("G"),
            Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetRow(minLabel, 1);
        Grid.SetColumn(minLabel, 4);
        grid.Children.Add(minLabel);

        _rangeSlider = new System.Windows.Controls.Slider
        {
            Minimum = minVal,
            Maximum = maxVal,
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(0, 4, 0, 4),
            MinHeight = 60,
            Style = Application.Current.TryFindResource("GcNumericSliderStyle") as Style,
        };
        _rangeSlider.ValueChanged += OnSliderValueChanged;
        Grid.SetRow(_rangeSlider, 2);
        Grid.SetRowSpan(_rangeSlider, 2);
        Grid.SetColumn(_rangeSlider, 4);
        grid.Children.Add(_rangeSlider);

        var maxLabel = new System.Windows.Controls.TextBlock
        {
            Text = maxVal.ToString("G"),
            Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetRow(maxLabel, 4);
        Grid.SetColumn(maxLabel, 4);
        grid.Children.Add(maxLabel);

        host.Children.Add(grid);
    }

    private void ApplyKeyStyle(System.Windows.Controls.Button btn)
    {
        var rootFactory = new FrameworkElementFactory(typeof(Border), "Root");
        rootFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        rootFactory.SetValue(Border.SnapsToDevicePixelsProperty, true);
        rootFactory.SetBinding(Border.BackgroundProperty, new Binding(nameof(System.Windows.Controls.Button.Background)) { RelativeSource = RelativeSource.TemplatedParent });
        rootFactory.SetBinding(Border.BorderBrushProperty, new Binding(nameof(System.Windows.Controls.Button.BorderBrush)) { RelativeSource = RelativeSource.TemplatedParent });
        rootFactory.SetBinding(Border.BorderThicknessProperty, new Binding(nameof(System.Windows.Controls.Button.BorderThickness)) { RelativeSource = RelativeSource.TemplatedParent });

        var innerGrid = new FrameworkElementFactory(typeof(Grid));
        var cpFactory = new FrameworkElementFactory(typeof(ContentPresenter));
        cpFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        cpFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        innerGrid.AppendChild(cpFactory);
        rootFactory.AppendChild(innerGrid);

        var template = new ControlTemplate(typeof(System.Windows.Controls.Button)) { VisualTree = rootFactory };

        var triggerOver = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        triggerOver.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)), "Root"));
        template.Triggers.Add(triggerOver);

        var triggerPress = new Trigger { Property = ButtonBase.IsPressedProperty, Value = true };
        triggerPress.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(120, 79, 182, 255)), "Root"));
        template.Triggers.Add(triggerPress);

        btn.Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
        btn.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
        btn.BorderThickness = new Thickness(1);
        btn.Template = template;
    }

    private void BuildAlphaKeys(System.Windows.Controls.Panel host)
    {
        if (_isSymbolMode)
        {
            var symRows = new[] {
                "1234567890",
                "-./,;:'@#$%",
                "[]{}()+=_&",
            };
            foreach (var row in symRows)
            {
                var sp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
                foreach (var ch in row)
                    sp.Children.Add(CreateKey(ch.ToString()));
                host.Children.Add(sp);
            }

            var bottomRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            bottomRow.Children.Add(CreateToggleKey("\u2190\u2192", "ABC", OnToggleSymbol, 64));
            bottomRow.Children.Add(CreateSpaceKey());
            host.Children.Add(bottomRow);
        }
        else
        {
            string[] rows;
            if (_isUpperCase)
            {
                rows = new[] {
                    "QWERTYUIOP",
                    " ASDFGHJKL",
                    "  ZXCVBNM",
                };
            }
            else
            {
                rows = new[] {
                    "qwertyuiop",
                    " asdfghjkl",
                    "  zxcvbnm",
                };
            }

            foreach (var row in rows)
            {
                var sp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
                foreach (var ch in row)
                {
                    if (ch == ' ') { sp.Children.Add(new System.Windows.Controls.Border { Width = 12, Background = Brushes.Transparent }); continue; }
                    sp.Children.Add(CreateKey(ch.ToString()));
                }
                host.Children.Add(sp);
            }

            var bottomRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            bottomRow.Children.Add(CreateToggleKey("\u21E7", "shift", OnToggleShift, 64));
            bottomRow.Children.Add(CreateSpaceKey());
            bottomRow.Children.Add(CreateToggleKey("123", "123", OnToggleSymbol, 64));
            host.Children.Add(bottomRow);
        }
    }

    private System.Windows.Controls.Button CreateSpaceKey()
    {
        var rootFactory = new FrameworkElementFactory(typeof(Border), "Root");
        rootFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        rootFactory.SetValue(Border.SnapsToDevicePixelsProperty, true);
        rootFactory.SetBinding(Border.BackgroundProperty, new Binding(nameof(System.Windows.Controls.Button.Background)) { RelativeSource = RelativeSource.TemplatedParent });
        rootFactory.SetBinding(Border.BorderBrushProperty, new Binding(nameof(System.Windows.Controls.Button.BorderBrush)) { RelativeSource = RelativeSource.TemplatedParent });
        rootFactory.SetBinding(Border.BorderThicknessProperty, new Binding(nameof(System.Windows.Controls.Button.BorderThickness)) { RelativeSource = RelativeSource.TemplatedParent });

        var innerGrid = new FrameworkElementFactory(typeof(Grid));
        var cpFactory = new FrameworkElementFactory(typeof(ContentPresenter));
        cpFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        cpFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        innerGrid.AppendChild(cpFactory);
        rootFactory.AppendChild(innerGrid);

        var template = new ControlTemplate(typeof(System.Windows.Controls.Button)) { VisualTree = rootFactory };

        var triggerOver = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        triggerOver.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)), "Root"));
        template.Triggers.Add(triggerOver);

        var triggerPress = new Trigger { Property = ButtonBase.IsPressedProperty, Value = true };
        triggerPress.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(120, 79, 182, 255)), "Root"));
        template.Triggers.Add(triggerPress);

        var btn = new GcButton
        {
            Content = " ",
            Tag = " ",
            Width = 300,
            Height = 48,
            Margin = new Thickness(3),
            FontSize = 16,
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            FocusVisualStyle = null,
            Template = template,
        };

        btn.Click += OnKeyClick;
        return btn;
    }

    private void OnToggleShift(object? sender, RoutedEventArgs e)
    {
        _isUpperCase = !_isUpperCase;
        RebuildAlphaKeys();
    }

    private void OnToggleSymbol(object? sender, RoutedEventArgs e)
    {
        _isSymbolMode = !_isSymbolMode;
        RebuildAlphaKeys();
    }

    private System.Windows.Controls.Button CreateToggleKey(string display, string tag, RoutedEventHandler handler, double width = 80)
    {
        var btn = new GcButton
        {
            Content = display,
            Tag = tag,
            Width = width,
            Height = 48,
            Margin = new Thickness(3),
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
            Cursor = Cursors.Hand,
            FocusVisualStyle = null,
        };

        var rootFactory = new FrameworkElementFactory(typeof(Border), "Root");
        rootFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        rootFactory.SetValue(Border.SnapsToDevicePixelsProperty, true);
        rootFactory.SetBinding(Border.BackgroundProperty, new Binding(nameof(System.Windows.Controls.Button.Background)) { RelativeSource = RelativeSource.TemplatedParent });
        rootFactory.SetBinding(Border.BorderBrushProperty, new Binding(nameof(System.Windows.Controls.Button.BorderBrush)) { RelativeSource = RelativeSource.TemplatedParent });
        rootFactory.SetBinding(Border.BorderThicknessProperty, new Binding(nameof(System.Windows.Controls.Button.BorderThickness)) { RelativeSource = RelativeSource.TemplatedParent });

        var innerGrid = new FrameworkElementFactory(typeof(Grid));

        var cpFactory = new FrameworkElementFactory(typeof(ContentPresenter));
        cpFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        cpFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        innerGrid.AppendChild(cpFactory);

        rootFactory.AppendChild(innerGrid);

        var template = new ControlTemplate(typeof(System.Windows.Controls.Button)) { VisualTree = rootFactory };

        var triggerOver = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        triggerOver.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)), "Root"));
        template.Triggers.Add(triggerOver);

        var triggerPress = new Trigger { Property = ButtonBase.IsPressedProperty, Value = true };
        triggerPress.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(120, 79, 182, 255)), "Root"));
        template.Triggers.Add(triggerPress);

        btn.Background = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
        btn.BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
        btn.BorderThickness = new Thickness(1);
        btn.Template = template;
        btn.Click += handler;
        return btn;
    }

    private GcButton CreateKey(string content)
    {
        var rootFactory = new FrameworkElementFactory(typeof(Border), "Root");
        rootFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        rootFactory.SetValue(Border.SnapsToDevicePixelsProperty, true);
        rootFactory.SetBinding(Border.BackgroundProperty, new Binding(nameof(System.Windows.Controls.Button.Background)) { RelativeSource = RelativeSource.TemplatedParent });
        rootFactory.SetBinding(Border.BorderBrushProperty, new Binding(nameof(System.Windows.Controls.Button.BorderBrush)) { RelativeSource = RelativeSource.TemplatedParent });
        rootFactory.SetBinding(Border.BorderThicknessProperty, new Binding(nameof(System.Windows.Controls.Button.BorderThickness)) { RelativeSource = RelativeSource.TemplatedParent });

        var innerGrid = new FrameworkElementFactory(typeof(Grid));

        var cpFactory = new FrameworkElementFactory(typeof(ContentPresenter));
        cpFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        cpFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        innerGrid.AppendChild(cpFactory);

        rootFactory.AppendChild(innerGrid);

        var template = new ControlTemplate(typeof(System.Windows.Controls.Button)) { VisualTree = rootFactory };

        var triggerOver = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        triggerOver.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)), "Root"));
        template.Triggers.Add(triggerOver);

        var triggerPress = new Trigger { Property = ButtonBase.IsPressedProperty, Value = true };
        triggerPress.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(120, 79, 182, 255)), "Root"));
        template.Triggers.Add(triggerPress);

        var btn = new GcButton
        {
            Content = content,
            Tag = content,
            Width = 64,
            Height = 48,
            Margin = new Thickness(3),
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            FocusVisualStyle = null,
            Template = template,
        };

        btn.Click += OnKeyClick;
        return btn;
    }

    private void OnKeyClick(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string key)
        {
            if (key == "00")
            {
                if (_inputValue.Length == 0 || _inputValue == "0")
                    _inputValue = "0";
                else if (_inputValue.All(c => c == '0' || c == '-'))
                    _inputValue += "0";
                else
                    _inputValue += "00";
                UpdatePreview();
                return;
            }

            if (key == "-" && _inputValue.Contains('-')) return;
            if (key == "." && _inputValue.Contains('.')) return;

            if (key == "." && DecimalPlaces > 0)
            {
                var dotIndex = _inputValue.IndexOf('.');
                if (dotIndex >= 0 && _inputValue.Length - dotIndex > DecimalPlaces) return;
            }

            _inputValue += key;

            if (KeyboardMode == GcKeyboardType.Numeric)
                _inputValue = StripLeadingZeros(_inputValue);

            UpdatePreview();
        }
    }

    private static string StripLeadingZeros(string input)
    {
        if (string.IsNullOrEmpty(input) || input == "-") return input;

        var negative = input.StartsWith('-');
        var s = negative ? input[1..] : input;

        var dotIndex = s.IndexOf('.');
        var intPart = dotIndex >= 0 ? s[..dotIndex] : s;
        var fracPart = dotIndex >= 0 ? s[dotIndex..] : "";

        intPart = intPart.TrimStart('0');
        if (intPart.Length == 0) intPart = "0";

        return (negative ? "-" : "") + intPart + fracPart;
    }

    private void OnBackspace(object sender, RoutedEventArgs e)
    {
        if (_inputValue.Length > 0)
            _inputValue = _inputValue[..^1];
        UpdatePreview();
    }

    private void OnClear(object sender, RoutedEventArgs e)
    {
        _inputValue = "";
        UpdatePreview();
    }

    private void OnConfirm(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_inputValue))
        {
            _inputValue = "0";
        }
        else if (_inputValue.StartsWith('.'))
        {
            _inputValue = "0" + _inputValue;
        }
        else if (_inputValue.StartsWith('-') && _inputValue.Length > 1 && _inputValue[1] == '.')
        {
            _inputValue = "-0" + _inputValue[1..];
        }

        if (double.TryParse(_inputValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var val))
        {
            if (MinValue.HasValue && val < MinValue.Value) { ClosePopup(); return; }
            if (MaxValue.HasValue && val > MaxValue.Value) { ClosePopup(); return; }
        }

        if (_target is System.Windows.Controls.TextBox tb)
            tb.Text = _inputValue;

        RaiseEvent(new RoutedEventArgs(ConfirmedEvent));
        ClosePopup();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(CancelledEvent));
        ClosePopup();
    }

    private void UpdatePreview()
    {
        if (_previewBox != null)
            _previewBox.Text = _inputValue;
        SyncSliderFromInput();
    }

    private void SyncSliderFromInput()
    {
        if (_rangeSlider == null || KeyboardMode != GcKeyboardType.Numeric) return;
        if (double.TryParse(_inputValue, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var v))
        {
            var clamped = Math.Max(_rangeSlider.Minimum, Math.Min(_rangeSlider.Maximum, v));
            if (Math.Abs(_rangeSlider.Value - clamped) > 0.0001)
            {
                _syncingSlider = true;
                _rangeSlider.Value = clamped;
                _syncingSlider = false;
            }
        }
    }

    private void OnSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_rangeSlider == null || _syncingSlider) return;
        string formatted;
        if (_inputValue.Contains('.'))
            formatted = e.NewValue.ToString($"F{DecimalPlaces}", System.Globalization.CultureInfo.InvariantCulture);
        else
            formatted = e.NewValue.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture);
        if (_inputValue != formatted)
        {
            _inputValue = formatted;
            if (_previewBox != null)
                _previewBox.Text = _inputValue;
        }
    }

    public string InputValue { get => _inputValue; set { _inputValue = value; UpdatePreview(); } }
    public GcKeyboardType KeyboardMode { get; set; }
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public int DecimalPlaces { get; set; } = 2;

    public static readonly RoutedEvent ConfirmedEvent = EventManager.RegisterRoutedEvent("Confirmed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(GcKeyboardPopup));
    public static readonly RoutedEvent CancelledEvent = EventManager.RegisterRoutedEvent("Cancelled", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(GcKeyboardPopup));
    public event RoutedEventHandler Confirmed { add => AddHandler(ConfirmedEvent, value); remove => RemoveHandler(ConfirmedEvent, value); }
    public event RoutedEventHandler Cancelled { add => AddHandler(CancelledEvent, value); remove => RemoveHandler(CancelledEvent, value); }
}

















