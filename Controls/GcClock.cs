using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GeneralControl.Controls;

/// <summary>
/// 实时时间时钟控件。
///
/// 【功能】
/// DispatcherTimer 每秒刷新，显示当前日期与时间；
/// Format 自定义时间格式；ShowDate 控制日期行显隐。
/// Unloaded 自动停止计时器，防止内存泄漏。
/// </summary>
public class GcClock : Control
{
    private const string PartTimeTextName = "PART_TimeText";
    private const string PartDateTextName = "PART_DateText";

    static GcClock()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GcClock), new FrameworkPropertyMetadata(typeof(GcClock)));
    }

    public static readonly DependencyProperty FormatProperty =
        DependencyProperty.Register(nameof(Format), typeof(string), typeof(GcClock),
            new PropertyMetadata("HH:mm:ss", OnFormatChanged));

    public static readonly DependencyProperty ShowDateProperty =
        DependencyProperty.Register(nameof(ShowDate), typeof(bool), typeof(GcClock),
            new PropertyMetadata(true, OnFormatChanged));

    /// <summary>时间显示格式（默认 HH:mm:ss）。</summary>
    public string Format
    {
        get => (string)GetValue(FormatProperty);
        set => SetValue(FormatProperty, value);
    }

    /// <summary>是否显示日期行。</summary>
    public bool ShowDate
    {
        get => (bool)GetValue(ShowDateProperty);
        set => SetValue(ShowDateProperty, value);
    }

    private TextBlock? _timeText;
    private TextBlock? _dateText;
    private DispatcherTimer? _timer;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _timeText = GetTemplateChild(PartTimeTextName) as TextBlock;
        _dateText = GetTemplateChild(PartDateTextName) as TextBlock;

        if (_timer is null)
        {
            _timer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(1),
            };
            _timer.Tick += (_, _) => UpdateTime();
        }

        UpdateTime();
        _timer.Start();

        Unloaded += (_, _) => _timer.Stop();
    }

    private static void OnFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((GcClock)d).UpdateTime();

    private void UpdateTime()
    {
        var now = DateTime.Now;
        if (_timeText is not null)
        {
            _timeText.Text = now.ToString(string.IsNullOrWhiteSpace(Format) ? "HH:mm:ss" : Format);
        }

        if (_dateText is not null)
        {
            _dateText.Text = ShowDate ? now.ToString("yyyy-MM-dd dddd") : string.Empty;
        }
    }
}
