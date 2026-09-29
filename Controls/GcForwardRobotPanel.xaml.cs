using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace GeneralControl.Controls;

/// <summary>
/// <see cref="GcForwardRobot"/> 的右键浮出控制面板。
///
/// 参照演示区第一个机械手 WaferRobot 的 FloatingPanel：独立置顶的无边框浮窗，
/// 颜色自带、不依赖主题字典。面板与本体双向联动：
/// - 姿态下拉 / 快捷按钮 → 写 <see cref="GcForwardRobot.Pose"/>；
/// - 复选框 → 写 HasWafer / X1HasWafer / X2HasWafer；
/// - 本体侧改动姿态或夹片时，勾选与下拉同步回面板
///   （靠 DependencyPropertyDescriptor 监听 DP，不要求本体实现 INotifyPropertyChanged）。
/// 关节角读数用 50ms 定时器轮询，过渡动画期间数值连续变化。
/// </summary>
public partial class GcForwardRobotPanel : Window
{
    private readonly GcForwardRobot _robot;
    private readonly DispatcherTimer _angleTimer;
    private readonly List<DependencyPropertyDescriptor> _watched = new();
    private bool _syncing;

    public GcForwardRobotPanel(GcForwardRobot robot)
    {
        _robot = robot ?? throw new ArgumentNullException(nameof(robot));

        InitializeComponent();

        CmbPose.ItemsSource = Enum.GetNames(typeof(GcForwardRobotPose));
        CmbPose.SelectedItem = _robot.Pose.ToString();

        ChkMagazine.IsChecked = _robot.HasWafer;
        ChkX1.IsChecked = _robot.X1HasWafer;
        ChkX2.IsChecked = _robot.X2HasWafer;

        // 本体侧改姿态或夹片（例如代码里直接设 Pose）时，把面板同步回来
        foreach (var dp in new[]
                 {
                     GcForwardRobot.PoseProperty,
                     GcForwardRobot.HasWaferProperty,
                     GcForwardRobot.X1HasWaferProperty,
                     GcForwardRobot.X2HasWaferProperty,
                 })
        {
            var descriptor = DependencyPropertyDescriptor.FromProperty(dp, typeof(GcForwardRobot));
            descriptor.AddValueChanged(_robot, OnRobotDpChanged);
            _watched.Add(descriptor);
        }

        _angleTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(50),
        };
        _angleTimer.Tick += (_, _) => RefreshAngles();
        _angleTimer.Start();

        RefreshAngles();
    }

    private void OnRobotDpChanged(object? sender, EventArgs e)
    {
        if (_syncing || !IsLoaded)
        {
            return;
        }

        _syncing = true;
        try
        {
            CmbPose.SelectedItem = _robot.Pose.ToString();
            ChkMagazine.IsChecked = _robot.HasWafer;
            ChkX1.IsChecked = _robot.X1HasWafer;
            ChkX2.IsChecked = _robot.X2HasWafer;
        }
        finally
        {
            _syncing = false;
        }
    }

    private void RefreshAngles()
    {
        TxtX1.Text = $"{_robot.X1Angle:F1}°";
        TxtX2.Text = $"{_robot.X2Angle:F1}°";
        TxtX3.Text = $"{_robot.X3Angle:F1}°";
        TxtX4.Text = $"{_robot.X4Angle:F1}°";
    }

    private void OnPoseSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing || CmbPose.SelectedItem is not string name)
        {
            return;
        }

        _syncing = true;
        try
        {
            _robot.Pose = Enum.Parse<GcForwardRobotPose>(name);
        }
        finally
        {
            _syncing = false;
        }
    }

    private void OnQuickPose(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string name })
        {
            CmbPose.SelectedItem = name;
        }
    }

    private void OnWaferToggled(object sender, RoutedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }

        _syncing = true;
        try
        {
            _robot.HasWafer = ChkMagazine.IsChecked == true;
            _robot.X1HasWafer = ChkX1.IsChecked == true;
            _robot.X2HasWafer = ChkX2.IsChecked == true;
        }
        finally
        {
            _syncing = false;
        }
    }

    private void OnDragBarMouseDown(object sender, MouseButtonEventArgs e)
    {
        // 只有按住标题栏才拖动，点面板其它位置不移动窗口
        if (e.ButtonState == MouseButtonState.Pressed && e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        _angleTimer.Stop();
        foreach (var descriptor in _watched)
        {
            descriptor.RemoveValueChanged(_robot, OnRobotDpChanged);
        }

        _watched.Clear();
        base.OnClosed(e);
    }
}
