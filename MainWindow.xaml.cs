using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CustomControl.Controls;

namespace CustomControl;

/// <summary>
/// 毛玻璃控件库演示窗口：分卡片展示全部 Glass* 控件与背景模式。
/// </summary>
public partial class MainWindow : GlassWindow
{
    public MainWindow()
    {
        InitializeComponent();

        // 给 DataGrid 演示卡填充模拟数据。
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

    // 以下六个按钮对应 GlassBackdrop 的六种背景模式。
    // 当前窗口通过 Backdrop 属性热切换；Acrylic / Mica 属于系统级材质，
    // 需在非分层窗口创建时指定（见 OpenModeWindow），因此以独立演示窗口打开。
    private void BackdropAuto_Click(object sender, RoutedEventArgs e) => Backdrop = GlassBackdrop.Auto;

    private void BackdropSimulated_Click(object sender, RoutedEventArgs e) => Backdrop = GlassBackdrop.Simulated;

    private void BackdropGlassy_Click(object sender, RoutedEventArgs e) => Backdrop = GlassBackdrop.Glassy;

    private void BackdropAcrylic_Click(object sender, RoutedEventArgs e) => OpenModeWindow(GlassBackdrop.Acrylic, "系统亚克力 Acrylic");

    private void BackdropMica_Click(object sender, RoutedEventArgs e) => OpenModeWindow(GlassBackdrop.Mica, "Mica");

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void ShowListDialog_Click(object sender, RoutedEventArgs e)
    {
        var btn = (System.Windows.Controls.Button)sender;
        var title = DialogProperties.GetDialogTitle(btn);
        var itemsStr = DialogProperties.GetDialogItems(btn);
        var items = string.IsNullOrEmpty(itemsStr) ? Array.Empty<string>() : itemsStr.Split(',').Select(s => s.Trim()).ToArray();

        var dlg = new GlassDialog
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
        var title = DialogProperties.GetDialogTitle(btn);
        var message = DialogProperties.GetDialogMessage(btn);

        var dlg = new GlassInputDialog
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
    private void OpenModeWindow(GlassBackdrop backdrop, string title)
    {
        var win = new GlassWindow
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

        var card = new GlassCard
        {
            Header = title,
            Width = 380,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            IsElevated = true,
        };
        var stack = new StackPanel();
        stack.Children.Add(new GlassTextBlock
        {
            Text = "这是一个使用 Backdrop 模式打开的演示窗口。",
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 12),
        });
        stack.Children.Add(new GlassButton { Content = "关闭窗口", Width = 120, HorizontalAlignment = System.Windows.HorizontalAlignment.Left });
        card.Content = stack;
        panel.Children.Add(card);
        return panel;
    }
}

/// <summary>演示数据行（DataGrid 卡片使用）。</summary>
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
