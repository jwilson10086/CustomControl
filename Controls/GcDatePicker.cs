using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace GeneralControl.Controls;

/// <summary>
/// 工业日历选择控件（日期筛选）。
///
/// 【用法】
/// 点击输入框弹出日历，选择日期后自动回填并关闭；
/// SelectedDate 可直接绑定查询条件；配合两个实例即可做时间范围筛选。
/// </summary>
[TemplatePart(Name = PartToggleName, Type = typeof(System.Windows.Controls.Primitives.ToggleButton))]
[TemplatePart(Name = PartPopupName, Type = typeof(Popup))]
[TemplatePart(Name = PartCalendarName, Type = typeof(Calendar))]
public class GcDatePicker : Control
{
    private const string PartToggleName = "PART_Toggle";
    private const string PartPopupName = "PART_Popup";
    private const string PartCalendarName = "PART_Calendar";
    private const string PartDisplayTextName = "PART_DisplayText";

    static GcDatePicker()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcDatePicker), new FrameworkPropertyMetadata(typeof(GcDatePicker)));
    }

    public static readonly DependencyProperty SelectedDateProperty =
        DependencyProperty.Register(nameof(SelectedDate), typeof(DateTime?), typeof(GcDatePicker),
            new FrameworkPropertyMetadata(DateTime.Today, OnSelectedChanged));

    public static readonly DependencyProperty DateFormatProperty =
        DependencyProperty.Register(nameof(DateFormat), typeof(string), typeof(GcDatePicker),
            new PropertyMetadata("yyyy-MM-dd", OnSelectedChanged));

    /// <summary>选中的日期。</summary>
    public DateTime? SelectedDate
    {
        get => (DateTime?)GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    /// <summary>显示格式（默认 yyyy-MM-dd，可选 yyyy-MM-dd HH:mm 等）。</summary>
    public string DateFormat
    {
        get => (string)GetValue(DateFormatProperty);
        set => SetValue(DateFormatProperty, value);
    }

    private System.Windows.Controls.Primitives.ToggleButton? _toggle;
    private Popup? _popup;
    private Calendar? _calendar;
    private TextBlock? _displayText;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_calendar is not null)
        {
            _calendar.SelectedDatesChanged -= OnCalendarSelectedDatesChanged;
        }

        _toggle = GetTemplateChild(PartToggleName) as System.Windows.Controls.Primitives.ToggleButton;
        _popup = GetTemplateChild(PartPopupName) as Popup;
        _calendar = GetTemplateChild(PartCalendarName) as Calendar;
        _displayText = GetTemplateChild(PartDisplayTextName) as TextBlock;

        if (_toggle is not null && _popup is not null)
        {
            _toggle.Click += (_, _) => _popup.IsOpen = true;
        }

        if (_popup is not null)
        {
            _popup.Closed += (_, _) => { if (_toggle is not null) _toggle.IsChecked = false; };
        }

        if (_calendar is not null)
        {
            _calendar.SelectedDate ??= SelectedDate ?? DateTime.Today;
            _calendar.SelectedDatesChanged += OnCalendarSelectedDatesChanged;
        }

        UpdateDisplay();
    }

    private void OnCalendarSelectedDatesChanged(object? sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_calendar?.SelectedDate is { } d)
        {
            SelectedDate = d;
        }

        if (_popup is not null)
        {
            _popup.IsOpen = false;
        }
    }

    private static void OnSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((GcDatePicker)d).UpdateDisplay();

    private void UpdateDisplay()
    {
        if (_displayText is null)
        {
            return;
        }

        _displayText.Text = SelectedDate?.ToString(DateFormat) ?? string.Empty;
    }
}

