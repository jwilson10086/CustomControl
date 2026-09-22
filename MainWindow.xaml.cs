using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using GeneralControl.Controls;

namespace GeneralControl;

/// <summary>
/// 毛玻璃控件库演示窗口：分卡片展示全部 Glass* 控件与背景模式。
/// </summary>
public partial class MainWindow : GeneralControl.Controls.GcWindow
{
    public MainWindow()
    {
        InitializeComponent();

        // 给 GcDataGrid 演示卡填充模拟数据。
        DemoGrid.ItemsSource = CreateDemoData();
    }

    /// <summary>
    /// 侧边栏分类切换：按索引显示对应分区、隐藏其余。
    /// 注意：XAML 解析期间首个项的 IsSelected 会提前触发本方法，
    /// 此时 SectionHost 可能尚未构建完成，需判空。
    /// </summary>
    private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NavList == null || SectionHost == null)
        {
            return;
        }

        var index = NavList.SelectedIndex;
        for (var i = 0; i < SectionHost.Children.Count; i++)
        {
            SectionHost.Children[i].Visibility = i == index ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    /// <summary>生成 15 条随机员工演示数据（姓名/部门/职位/薪资/入职日期）。</summary>
    private static ObservableCollection<DemoEmployee> CreateDemoData()
    {
        var data = new ObservableCollection<DemoEmployee>();
        var names = new[] { "张伟", "王芳", "李娜", "刘洋", "陈静", "杨光", "赵磊", "黄敏", "周涛", "吴霞", "徐强", "孙丽", "马俊", "朱婷", "胡兵" };
        var departments = new[] { "研发部", "产品部", "设计部", "市场部", "人事部", "财务部" };
        var roles = new[] { "工程师", "经理", "设计师", "专员", "主管", "分析师" };
        var rand = new Random(42);
        for (var i = 0; i < names.Length; i++)
        {
            data.Add(new DemoEmployee
            {
                Name = names[i],
                Department = departments[i % departments.Length],
                Role = roles[rand.Next(roles.Length)],
                Salary = 6000 + rand.Next(340) * 100,
                Joined = new DateTime(2019 + rand.Next(6), 1 + rand.Next(12), 1 + rand.Next(27)),
            });
        }
        return data;
    }

    // 以下六个按钮对应 GcBackdropMode 的六种背景模式。
    // 当前窗口通过 Backdrop 属性热切换；Acrylic / Mica 属于系统级材质，
    // 需在非分层窗口创建时指定（见 OpenModeWindow），因此以独立演示窗口打开。
    private void BackdropAuto_Click(object sender, RoutedEventArgs e) => Backdrop = GcBackdropMode.Auto;

    private void BackdropSimulated_Click(object sender, RoutedEventArgs e) => Backdrop = GcBackdropMode.Simulated;

    private void BackdropGlassy_Click(object sender, RoutedEventArgs e) => Backdrop = GcBackdropMode.Glassy;

    private void BackdropAcrylic_Click(object sender, RoutedEventArgs e) => OpenModeWindow(GcBackdropMode.Acrylic, "系统亚克力 Acrylic");

    private void BackdropMica_Click(object sender, RoutedEventArgs e) => OpenModeWindow(GcBackdropMode.Mica, "Mica");

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    // ==================== 电镀腔演示 ====================
    // 自动模拟与按钮缓动全部挂在 CompositionTarget.Rendering 上（与 vsync 同拍、
    // 连续时间累积）——取代 16ms DispatcherTimer（与渲染帧错位 → 动画发卡）。
    //   1. 盖板盖下          2. 下降30%并倾斜
    //   3. 旋转下降到底+水位55 4. 倾斜回正+水位75
    //   5. 保持旋转5秒        6. 停转上升+排液(12)
    //   7. 开盖
    // 升降/倾斜另有按钮缓动（GcButton：升↑/降↓/左倾/右倾/回正）。

    private EventHandler? _renderHandler;
    private long _lastStamp;
    private bool _autoActive;
    private bool _cycleOnce;
    private int _platingStep;
    private double _autoElapsed;
    private double _stageDuration;
    private double _liftCur, _liftTarget;
    private double _tiltCur, _tiltTarget;
    private double _levelCur, _levelTarget;

    private static readonly double[] StageDurations = { 0.6, 1.2, 1.5, 1.2, 5.0, 1.2, 0.6 };

    private void PlatingAuto_Checked(object sender, RoutedEventArgs e) => StartPlatingAuto(once: false);

    private void PlatingAuto_Unchecked(object sender, RoutedEventArgs e)
    {
        _autoActive = false;
        _cycleOnce = false;
        EnsureRenderIdle();
    }

    private void PlatingCycle_Click(object sender, RoutedEventArgs e) => StartPlatingAuto(once: true);

    private void StartPlatingAuto(bool once)
    {
        _autoActive = true;
        _cycleOnce = once;
        _platingStep = 0;
        _autoElapsed = 0;
        _lastStamp = System.Diagnostics.Stopwatch.GetTimestamp();
        StageTargets(DemoPlating.HeadLift, DemoPlating.Tilt, DemoPlating.Level, StageDurations[0]);
        EnsureRenderRunning();
    }

    private void EnsureRenderRunning()
    {
        if (_renderHandler == null)
        {
            _renderHandler = (_, _) => PlatingRenderTick();
            System.Windows.Media.CompositionTarget.Rendering += _renderHandler;
        }
    }

    private void EnsureRenderIdle()
    {
        if (!_autoActive && !_liftTweenActive && !_tiltTweenActive && _renderHandler != null)
        {
            System.Windows.Media.CompositionTarget.Rendering -= _renderHandler;
            _renderHandler = null;
        }
    }

    /// <summary>记录本段关键帧目标值（起点取控件当前值）。</summary>
    private void StageTargets(double lift, double tilt, double level, double duration)
    {
        _liftCur = DemoPlating.HeadLift;
        _liftTarget = lift;
        _tiltCur = DemoPlating.Tilt;
        _tiltTarget = tilt;
        _levelCur = DemoPlating.Level;
        _levelTarget = level;
        _stageDuration = duration;
        _autoElapsed = 0;
    }

    private void PlatingRenderTick()
    {
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        var dt = Math.Min(0.05, (now - _lastStamp) / (double)System.Diagnostics.Stopwatch.Frequency);
        _lastStamp = now;

        if (_autoActive)
        {
            _autoElapsed += dt;
            var k = Math.Min(1.0, _autoElapsed / _stageDuration);
            var eased = k * k * (3 - 2 * k); // smoothstep
            // 值不变（静止段）时跳过写 DP，避免每帧无谓触发布局
            var lift = _liftCur + (_liftTarget - _liftCur) * eased;
            var tilt = _tiltCur + (_tiltTarget - _tiltCur) * eased;
            var level = _levelCur + (_levelTarget - _levelCur) * eased;
            if (Math.Abs(lift - DemoPlating.HeadLift) > 1e-4)
            {
                DemoPlating.HeadLift = lift;
            }
            if (Math.Abs(tilt - DemoPlating.Tilt) > 1e-4)
            {
                DemoPlating.Tilt = tilt;
            }
            if (Math.Abs(level - DemoPlating.Level) > 1e-4)
            {
                DemoPlating.Level = level;
            }
            if (k >= 1.0)
            {
                NextPlatingStage();
            }
        }

        if (_liftTweenActive)
        {
            _liftTweenElapsed += dt;
            var k = Math.Min(1.0, _liftTweenElapsed / 0.8);
            var eased = k * k * (3 - 2 * k);
            var lift = _liftTweenFrom + (_liftTweenTo - _liftTweenFrom) * eased;
            if (Math.Abs(lift - DemoPlating.HeadLift) > 1e-4)
            {
                DemoPlating.HeadLift = lift;
            }
            if (k >= 1.0)
            {
                _liftTweenActive = false;
            }
        }

        if (_tiltTweenActive)
        {
            _tiltTweenElapsed += dt;
            var k = Math.Min(1.0, _tiltTweenElapsed / 0.6);
            var eased = k * k * (3 - 2 * k);
            var tilt = _tiltTweenFrom + (_tiltTweenTo - _tiltTweenFrom) * eased;
            if (Math.Abs(tilt - DemoPlating.Tilt) > 1e-4)
            {
                DemoPlating.Tilt = tilt;
            }
            if (k >= 1.0)
            {
                _tiltTweenActive = false;
            }
        }

        EnsureRenderIdle();
    }

    private void NextPlatingStage()
    {
        switch (_platingStep)
        {
            case 0: // 1. 盖板盖下（关盖），其余保持
                DemoPlating.HeadOpen = false;
                StageTargets(DemoPlating.HeadLift, DemoPlating.Tilt, DemoPlating.Level, StageDurations[0]);
                break;
            case 1: // 2. 下降一点（30%）并倾斜
                StageTargets(30, 10, DemoPlating.Level, StageDurations[1]);
                break;
            case 2: // 3. 旋转下降到底 + 水位上升一点
                DemoPlating.IsRotating = true;
                StageTargets(0, 10, 55, StageDurations[2]);
                break;
            case 3: // 4. 倾斜回正 + 水位再上升
                StageTargets(0, 0, 75, StageDurations[3]);
                break;
            case 4: // 5. 保持旋转 5 秒（值不变，只耗时长）
                StageTargets(DemoPlating.HeadLift, DemoPlating.Tilt, DemoPlating.Level, StageDurations[4]);
                break;
            case 5: // 6. 停止转动时机延后：先边旋转边上升 + 排液（液位下降）
                // IsRotating 保持 true —— 上升过程仍在旋转
                StageTargets(80, DemoPlating.Tilt, 12, StageDurations[5]);
                break;
            case 6: // 7. 上升到位后停转（连接柱/光带复位回最开始的样子），再开盖
                DemoPlating.IsRotating = false;
                DemoPlating.HeadOpen = true;
                StageTargets(DemoPlating.HeadLift, DemoPlating.Tilt, DemoPlating.Level, StageDurations[6]);
                break;
        }
        _platingStep = (_platingStep + 1) % 7;
        if (_cycleOnce && _platingStep == 0)
        {
            // 单次循环完成：停止
            _cycleOnce = false;
            _autoActive = false;
            EnsureRenderIdle();
        }
    }

    // ---- 升降 / 倾斜按钮缓动（平滑插值，非瞬跳） ----

    private bool _liftTweenActive;
    private double _liftTweenFrom, _liftTweenTo, _liftTweenElapsed;
    private bool _tiltTweenActive;
    private double _tiltTweenFrom, _tiltTweenTo, _tiltTweenElapsed;

    private void PlatingLiftUp_Click(object sender, RoutedEventArgs e)
        => PlatingLiftTo(Math.Min(100, DemoPlating.HeadLift + 20));

    private void PlatingLiftDown_Click(object sender, RoutedEventArgs e)
        => PlatingLiftTo(Math.Max(0, DemoPlating.HeadLift - 20));

    private void PlatingTiltLeft_Click(object sender, RoutedEventArgs e)
        => PlatingTiltTo(Math.Max(0, DemoPlating.Tilt - 5));

    private void PlatingTiltRight_Click(object sender, RoutedEventArgs e)
        => PlatingTiltTo(Math.Min(20, DemoPlating.Tilt + 5));

    private void PlatingTiltReset_Click(object sender, RoutedEventArgs e)
        => PlatingTiltTo(0);

    private void PlatingLiftTo(double target)
    {
        _liftTweenFrom = DemoPlating.HeadLift;
        _liftTweenTo = target;
        _liftTweenElapsed = 0;
        _liftTweenActive = true;
        _lastStamp = System.Diagnostics.Stopwatch.GetTimestamp();
        EnsureRenderRunning();
    }

    private void PlatingTiltTo(double target)
    {
        _tiltTweenFrom = DemoPlating.Tilt;
        _tiltTweenTo = target;
        _tiltTweenElapsed = 0;
        _tiltTweenActive = true;
        _lastStamp = System.Diagnostics.Stopwatch.GetTimestamp();
        EnsureRenderRunning();
    }

    private void PlatingOpen_Changed(object sender, RoutedEventArgs e)
    {
        if (DemoPlatingOpen.IsChecked == true)
        {
            DemoPlating.HeadOpen = !DemoPlating.HeadOpen;
        }
    }

    private void PlatingRotate_Changed(object sender, RoutedEventArgs e)
    {
        DemoPlating.IsRotating = DemoPlatingRotate.IsChecked == true;
    }

    // ==================== APT 腔演示（GcPlatingChamber 拷贝） ====================

    private void AptAuto_Checked(object sender, RoutedEventArgs e) => DemoApt.AutoSimulate = true;

    private void AptAuto_Unchecked(object sender, RoutedEventArgs e) => DemoApt.AutoSimulate = false;

    private void AptRotate_Changed(object sender, RoutedEventArgs e)
    {
        DemoApt.IsRotating = DemoAptRotate.IsChecked == true;
    }

    private void AptOpen_Changed(object sender, RoutedEventArgs e)
    {
        if (DemoAptOpen.IsChecked == true)
        {
            DemoApt.HeadOpen = !DemoApt.HeadOpen;
        }
    }

    private void AptSpray_Changed(object sender, RoutedEventArgs e)
    {
        DemoApt.Spray = DemoAptSpray.IsChecked == true;
    }

    // ==================== SRD 腔演示（GcAptChamber 拷贝） ====================

    private void SrdAuto_Checked(object sender, RoutedEventArgs e) => DemoSrd.AutoSimulate = true;

    private void SrdAuto_Unchecked(object sender, RoutedEventArgs e) => DemoSrd.AutoSimulate = false;

    private void SrdRotate_Changed(object sender, RoutedEventArgs e)
    {
        DemoSrd.IsRotating = DemoSrdRotate.IsChecked == true;
    }

    private void SrdSpray_Changed(object sender, RoutedEventArgs e)
    {
        DemoSrd.Spray = DemoSrdSpray.IsChecked == true;
    }

    private void SrdWafer_Changed(object sender, RoutedEventArgs e)
    {
        DemoSrd.HasWafer = DemoSrdWafer.IsChecked == true;
    }

    private void SrdOpen_Changed(object sender, RoutedEventArgs e)
    {
        DemoSrd.HeadOpen = DemoSrdOpen.IsChecked == true;
    }

    private void ShowListDialog_Click(object sender, RoutedEventArgs e)
    {
        var btn = (System.Windows.Controls.Button)sender;
        var title = GcDialogProperties.GetDialogTitle(btn);
        var itemsStr = GcDialogProperties.GetDialogItems(btn);
        var items = string.IsNullOrEmpty(itemsStr) ? Array.Empty<string>() : itemsStr.Split(',').Select(s => s.Trim()).ToArray();

        var dlg = new GcDialog
        {
            Title = title,
            Items = items,
        };
        var result = dlg.ShowDialog();
        DialogResultText.Text = result == true ? $"已选择：{dlg.SelectedItem}" : "已取消";
    }

    private void ShowInputDialog_Click(object sender, RoutedEventArgs e)
    {
        var btn = (System.Windows.Controls.Button)sender;
        var title = GcDialogProperties.GetDialogTitle(btn);
        var message = GcDialogProperties.GetDialogMessage(btn);

        var dlg = new GcInputDialog
        {
            Title = title,
            Message = message,
        };
        var result = dlg.ShowDialog();
        DialogResultText.Text = result == true ? $"输入内容：{dlg.InputValue}" : "已取消";
    }

    /// <summary>
    /// 系统级材质需要非分层窗口，无法在当前窗口显示后热切换，
    /// 因此以新窗口演示 Acrylic / Mica 模式。
    /// </summary>
    private void OpenModeWindow(GcBackdropMode backdrop, string title)
    {
        var win = new GeneralControl.Controls.GcWindow
        {
            Title = title,
            Backdrop = backdrop,
            Width = 620,
            Height = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            Content = BuildModeContent(title),
        };
        win.Show();
    }

    private static UIElement BuildModeContent(string title)
    {
        var panel = new StackPanel();
        var gradient = new System.Windows.Media.RadialGradientBrush
        {
            Center = new System.Windows.Point(0.5, 0.4),
            RadiusX = 1.2,
            RadiusY = 1.2,
        };
        gradient.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromRgb(0x2A, 0x4A, 0x7F), 0));
        gradient.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromRgb(0x1B, 0x2A, 0x4A), 0.55));
        gradient.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromRgb(0x10, 0x17, 0x27), 1));
        panel.Background = gradient;

        var card = new GcCard
        {
            Header = title,
            Width = 380,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            IsElevated = true,
        };
        var stack = new StackPanel();
        stack.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "这是一个使用 Backdrop 模式打开的演示窗口。",
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 12),
        });
        stack.Children.Add(new System.Windows.Controls.Button { Content = "关闭窗口", Width = 120, HorizontalAlignment = System.Windows.HorizontalAlignment.Left });
        card.Content = stack;
        panel.Children.Add(card);
        return panel;
    }
}

/// <summary>演示数据行（GcDataGrid 卡片使用）。</summary>
public sealed class DemoEmployee
{
    /// <summary>姓名。</summary>
    public string Name { get; init; } = "";
    /// <summary>部门。</summary>
    public string Department { get; init; } = "";
    /// <summary>职位。</summary>
    public string Role { get; init; } = "";
    /// <summary>月薪（元）。</summary>
    public decimal Salary { get; init; }
    /// <summary>入职日期。</summary>
    public DateTime Joined { get; init; }
}












