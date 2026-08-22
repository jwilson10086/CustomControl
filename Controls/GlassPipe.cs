using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

// System.IO 与 System.Windows.Shapes 都有 Path 类型，各取别名避免 CS0104
// （IO 侧别名已随源码回写逻辑迁往 GlassDesignPersist）
using WpfPath = System.Windows.Shapes.Path;

namespace CustomControl.Controls;

/// <summary>管内介质流动方向（沿 Points 书写顺序为正向）。</summary>
public enum GlassFlowDirection
{
    /// <summary>正向：虚线从起点流向终点。</summary>
    Forward,

    /// <summary>反向：虚线从终点流向起点。</summary>
    Reverse
}

/// <summary>
/// 工业管路控件：直线或任意拐弯的玻璃管段 + 流动虚线动画。
///
/// 【两种用法】
/// 1. 直管（默认）：只设 Width（Height 固定 16），等价于旧版水平管段；
/// 2. 拐弯管：设置 Points（中心线像素坐标，如 "4,8 100,8 100,60"），
///    控件按坐标自适应尺寸，拐角自动生成圆弧弯头。
///
/// 【可视化编辑】
/// IsEditing=true 进入编辑模式，无需手写坐标：
/// - 直接用鼠标拖拽端点/拐点改变走向与长度（水平/垂直自动吸附对齐其它顶点）；
/// - 在管身上按住拖动即可就地"拉出"拐点并弯管（双击等效）；
/// - 鼠标滚轮就地调节管路粗细 PipeWidth（3~24px）；
/// - 右键单击顶点手柄删除该拐点（至少保留 2 个点）。
///
/// 【源码回写】
/// DEBUG 编译且附加调试器时，操作结束约 0.5 秒后自动把 Points / PipeWidth /
/// Direction 写回项目 *.xaml 中本控件标签的对应属性（优先按 x:Name
/// 定位，其次按初始 Points 指纹定位），下次 F5 直接生效；宽度/方向仅在偏离
/// 默认值时才写入属性，保持 XAML 干净；Release 或未调试时为纯空操作。
///
/// 【无缝衔接】
/// - 直管模式两端中心线各向外伸 3px，伸进相邻控件（阀门短管/泵端口）下方，
///   消除拼接处的发丝缝；绘制顺序上管子放在阀门之前即可被压在下面；
/// - 拐弯管模式请让首末点落在目标控件的端口面上（留 4px 给线宽半径）。
///
/// 【流动效果】
/// IsFlowing=true 时虚线沿路径循环位移；流向 = Points 的书写顺序方向。
/// </summary>
public class GlassPipe : Control
{
    static GlassPipe()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassPipe), new FrameworkPropertyMetadata(typeof(GlassPipe)));
    }

    public GlassPipe()
    {
        SizeChanged += (_, _) => Rebuild();
        Loaded += (_, _) =>
        {
            Rebuild();

#if DEBUG
            // 记录初始 Points 作为源码定位指纹（仅第一次）
            _persistSeedPoints ??= Points;

            // 写回成功后把指纹同步为新值，同一会话内继续编辑仍能定位
            GlassDesignPersist.WrittenBack += OnPersisted;
#endif
        };
    }
#if DEBUG
    private void OnPersisted(FrameworkElement el, IReadOnlyList<KeyValuePair<string, string>> updates)
    {
        foreach (var u in updates)
        {
            if (u.Key == nameof(Points))
            {
                _persistSeedPoints = u.Value;
            }
        }
    }
#endif

    public static readonly DependencyProperty IsFlowingProperty =
        DependencyProperty.Register(nameof(IsFlowing), typeof(bool), typeof(GlassPipe),
            new PropertyMetadata(false));

    public static readonly DependencyProperty PointsProperty =
        DependencyProperty.Register(nameof(Points), typeof(string), typeof(GlassPipe),
            new PropertyMetadata(null, OnPointsChanged));

    public static readonly DependencyProperty IsEditingProperty =
        DependencyProperty.Register(nameof(IsEditing), typeof(bool), typeof(GlassPipe),
            new PropertyMetadata(false, OnIsEditingChanged));

    public static readonly DependencyProperty PersistToSourceXamlProperty =
        DependencyProperty.Register(nameof(PersistToSourceXaml), typeof(bool), typeof(GlassPipe),
            new PropertyMetadata(true));

    public static readonly DependencyProperty PipeWidthProperty =
        DependencyProperty.Register(nameof(PipeWidth), typeof(double), typeof(GlassPipe),
            new PropertyMetadata(DefaultPipeWidth, OnPipeWidthChanged));

    // 注意：不能用 FlowDirection 这个名字——FrameworkElement 已有同名 DP，
    // new 隐藏会让 Style/Trigger 里的 Property="FlowDirection" 解析到基类
    // System.Windows.FlowDirection（值只有 Left/Right），导致 XAML 解析失败。
    // 故命名为 Direction，模板触发器按此匹配
    public static readonly DependencyProperty DirectionProperty =
        DependencyProperty.Register(nameof(Direction), typeof(GlassFlowDirection), typeof(GlassPipe),
            new PropertyMetadata(GlassFlowDirection.Forward));

    /// <summary>是否有介质流动（显示流动虚线动画）。</summary>
    public bool IsFlowing
    {
        get => (bool)GetValue(IsFlowingProperty);
        set => SetValue(IsFlowingProperty, value);
    }

    /// <summary>
    /// 管路中心线坐标点，格式 "x,y x,y ..."（空格分隔，像素坐标，相对控件左上角）。
    /// 至少 2 个点生效；为空则退化为随 Width 拉伸的水平直管。
    /// </summary>
    public string Points
    {
        get => (string)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    /// <summary>是否进入可视化编辑模式（拖拽顶点 / 拉出拐点 / 右键删点）。</summary>
    public bool IsEditing
    {
        get => (bool)GetValue(IsEditingProperty);
        set => SetValue(IsEditingProperty, value);
    }

    /// <summary>
    /// 编辑结束后自动把 Points 写回项目源 *.xaml。
    /// 仅在 DEBUG 编译且附加调试器时生效，其余环境为空操作。
    /// </summary>
    public bool PersistToSourceXaml
    {
        get => (bool)GetValue(PersistToSourceXamlProperty);
        set => SetValue(PersistToSourceXamlProperty, value);
    }

    /// <summary>管路粗细（外壁描边宽度，像素）。编辑模式滚轮可直接调节。</summary>
    public double PipeWidth
    {
        get => (double)GetValue(PipeWidthProperty);
        set => SetValue(PipeWidthProperty, value);
    }

    /// <summary>水流方向：正向沿 Points 书写顺序，反向相反。</summary>
    public GlassFlowDirection Direction
    {
        get => (GlassFlowDirection)GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    private const string PartPathName = "PART_Path";
    private const string PartInnerPathName = "PART_InnerPath";
    private const string PartFlowPathName = "PART_FlowPath";
    private const string PartEditLayerName = "PART_EditLayer";
    private const double DefaultPipeWidth = 8;
    private const double MinPipeWidth = 3;
    private const double MaxPipeWidth = 24;
    private const double CornerRadius = 10;
    private const double Overshoot = 3;
    private const double HandleSize = 10;
    private const double SnapTolerance = 7;

    private WpfPath? _path;
    private WpfPath? _innerPath;
    private WpfPath? _flowPath;
    private Canvas? _editLayer;
    private bool _templateWired;
    private int _dragIndex = -1;

    // 源码回写：初始 Points 指纹（用于无名控件定位标签）
    private string? _persistSeedPoints;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _path = GetTemplateChild(PartPathName) as WpfPath;
        _innerPath = GetTemplateChild(PartInnerPathName) as WpfPath;
        _flowPath = GetTemplateChild(PartFlowPathName) as WpfPath;
        _editLayer = GetTemplateChild(PartEditLayerName) as Canvas;
        WireTemplateEvents();
        ApplyWidths();
        Rebuild();
    }

    private void WireTemplateEvents()
    {
        if (_templateWired || _path is null)
        {
            return;
        }

        _templateWired = true;

        // 单击管身：在最近线段上就地插入拐点并立即进入拖动 —— "拉一下就弯"。
        // （端点/拐点手柄浮在编辑层，其事件自带 Handled，不会误触这里）
        _path.MouseLeftButtonDown += (_, e) =>
        {
            if (!IsEditing)
            {
                return;
            }

            InsertVertex(e.GetPosition(this));
            e.Handled = true;
        };

        // 编辑模式滚轮：就地调节管路粗细
        MouseWheel += (_, e) =>
        {
            if (!IsEditing)
            {
                return;
            }

            var delta = Math.Sign(e.Delta);
            if (delta == 0)
            {
                return;
            }

            PipeWidth = Math.Clamp(PipeWidth + delta, MinPipeWidth, MaxPipeWidth);
            e.Handled = true;
        };

        // 拖拽事件挂在控件级并在控件上捕获鼠标：
        // 手柄会随几何刷新被重建，捕获在手柄上会中途丢失
        MouseMove += (_, e) =>
        {
            if (_dragIndex < 0 || !IsEditing || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            MoveVertex(_dragIndex, e.GetPosition(this));
        };
        MouseLeftButtonUp += (_, _) =>
        {
            if (_dragIndex >= 0)
            {
                _dragIndex = -1;
                ReleaseMouseCapture();
            }
        };
        LostMouseCapture += (_, _) => _dragIndex = -1;
    }

    private static void OnPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((GlassPipe)d).Rebuild();

    private static void OnPipeWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var pipe = (GlassPipe)d;
        pipe.ApplyWidths();
        pipe.Rebuild();
        pipe.SchedulePersist();
    }

    private static void OnIsEditingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var pipe = (GlassPipe)d;
        if (e.NewValue is false)
        {
            pipe._dragIndex = -1;
        }

        pipe.UpdateEditLayer();
    }

    /// <summary>根据 Points（或直管退化逻辑）重建管路几何。</summary>
    private void Rebuild()
    {
        if (_path is null)
        {
            return;
        }

        var geo = BuildRoundedPath(GetEffectivePoints(), ElbowRadius());
        geo.Freeze();
        _path.Data = geo;
        if (_flowPath is not null)
        {
            _flowPath.Data = geo;
        }

        UpdateEditLayer();
    }

    /// <summary>按 PipeWidth 同步三层描边粗细（外壁 / 内芯 / 流动虚线）。</summary>
    private void ApplyWidths()
    {
        if (_path is not null)
        {
            _path.StrokeThickness = PipeWidth;
        }

        if (_innerPath is not null)
        {
            _innerPath.StrokeThickness = PipeWidth * 0.69;
        }

        if (_flowPath is not null)
        {
            _flowPath.StrokeThickness = PipeWidth * 0.375;
        }
    }

    /// <summary>弯头圆弧半径随管径缩放（默认 8px 管径对应原 10px 半径）。</summary>
    private double ElbowRadius() => Math.Clamp(PipeWidth * 1.25, 6, 18);

    /// <summary>取有效顶点：Points 为空时退化为横穿控件的水平直管（两端外伸搭接邻居）。</summary>
    private List<Point> GetEffectivePoints()
    {
        var pts = ParsePoints();
        if (pts.Count >= 2)
        {
            return pts;
        }

        var h = ActualHeight <= 0 ? 16 : ActualHeight;
        pts.Add(new Point(-Overshoot, h / 2));
        pts.Add(new Point(ActualWidth + Overshoot, h / 2));
        return pts;
    }

    private List<Point> ParsePoints()
    {
        var result = new List<Point>();
        var raw = Points;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return result;
        }

        foreach (var pair in raw.Split(new[] { ' ', ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split(',');
            if (parts.Length == 2
                && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
                && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
            {
                result.Add(new Point(x, y));
            }
        }

        return result;
    }

    /// <summary>
    /// 折线转圆角路径：在每个内角两侧各收回 r，用二次贝塞尔（控制点 = 角点）拟合弯头。
    /// r 受相邻两段长度一半约束，避免短段翻折。
    /// </summary>
    private static PathGeometry BuildRoundedPath(List<Point> pts, double radius)
    {
        var figure = new PathFigure { StartPoint = pts[0], IsClosed = false };
        for (var i = 1; i < pts.Count; i++)
        {
            var corner = pts[i];
            if (i < pts.Count - 1
                && TryCut(pts[i - 1], corner, pts[i + 1], radius, out var enter, out var exit))
            {
                figure.Segments.Add(new LineSegment(enter, true));
                figure.Segments.Add(new QuadraticBezierSegment(corner, exit, true));
            }
            else
            {
                figure.Segments.Add(new LineSegment(corner, true));
            }
        }

        return new PathGeometry(new[] { figure }) { FillRule = FillRule.Nonzero };
    }

    /// <summary>计算拐角两侧的切入/切出点；任一段过短则返回 false（保持尖角）。</summary>
    private static bool TryCut(Point prev, Point corner, Point next, double radius,
                               out Point enter, out Point exit)
    {
        enter = corner;
        exit = corner;

        var vIn = corner - prev;
        var vOut = next - corner;
        var lenIn = vIn.Length;
        var lenOut = vOut.Length;
        if (lenIn < 0.5 || lenOut < 0.5)
        {
            return false;
        }

        var r = Math.Min(radius, Math.Min(lenIn, lenOut) * 0.5);
        enter = corner - vIn * (r / lenIn);
        exit = corner + vOut * (r / lenOut);
        return true;
    }

    #region 可视化编辑

    /// <summary>按当前顶点重建/刷新编辑手柄层。</summary>
    private void UpdateEditLayer()
    {
        if (_editLayer is null)
        {
            return;
        }

        _editLayer.Children.Clear();
        if (!IsEditing)
        {
            return;
        }

        var pts = GetEffectivePoints();
        for (var i = 0; i < pts.Count; i++)
        {
            var handle = CreateHandle(i, pts[i]);
            _editLayer.Children.Add(handle);
        }
    }

    private Ellipse CreateHandle(int index, Point position)
    {
        var handle = new Ellipse
        {
            Width = HandleSize,
            Height = HandleSize,
            Fill = new SolidColorBrush(Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF)),
            Stroke = new SolidColorBrush(Color.FromArgb(0xE6, 0x35, 0xD0, 0x7F)),
            StrokeThickness = 1.5,
            Cursor = Cursors.Hand,
            ToolTip = "拖动移动；右键删除",
        };

        Canvas.SetLeft(handle, position.X - HandleSize / 2);
        Canvas.SetTop(handle, position.Y - HandleSize / 2);

        handle.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount != 1)
            {
                return;
            }

            BeginDrag(index);
            e.Handled = true;
        };
        handle.MouseRightButtonDown += (_, e) =>
        {
            DeleteVertex(index);
            e.Handled = true;
        };

        return handle;
    }

    private void BeginDrag(int index)
    {
        // 直管退化模式下首次拖拽：把隐含的水平直管线固化成显式 Points
        var pts = GetEffectivePoints();
        if (pts.Count >= 2 && string.IsNullOrWhiteSpace(Points))
        {
            WritePoints(pts);
        }

        _dragIndex = index;
        CaptureMouse();
    }

    private void MoveVertex(int index, Point position)
    {
        var pts = ParsePoints();
        if (index < 0 || index >= pts.Count)
        {
            return;
        }

        pts[index] = SnapToVertices(position, pts, index);
        WritePoints(pts);
    }

    private void InsertVertex(Point position)
    {
        var pts = GetEffectivePoints();

        // 找距离点击位置最近的线段，把新点插到该段之后
        var bestIndex = 0;
        var bestDist = double.MaxValue;
        for (var i = 0; i < pts.Count - 1; i++)
        {
            var d = DistanceToSegment(position, pts[i], pts[i + 1]);
            if (!(d < bestDist))
            {
                continue;
            }

            bestDist = d;
            bestIndex = i + 1;
        }

        pts.Insert(bestIndex, SnapToVertices(position, pts, -1));
        WritePoints(pts);

        // 新点立即进入拖动，跟随鼠标微调
        _dragIndex = bestIndex;
        CaptureMouse();
    }

    private void DeleteVertex(int index)
    {
        var pts = ParsePoints();
        if (pts.Count <= 2 || index < 0 || index >= pts.Count)
        {
            return;
        }

        pts.RemoveAt(index);
        _dragIndex = -1;
        WritePoints(pts);
    }

    /// <summary>吸附：与其它顶点横向/纵向接近时对齐，保证水平垂直管段。</summary>
    private static Point SnapToVertices(Point p, List<Point> pts, int exclude)
    {
        for (var i = 0; i < pts.Count; i++)
        {
            if (i == exclude)
            {
                continue;
            }

            var q = pts[i];
            if (Math.Abs(p.X - q.X) <= SnapTolerance)
            {
                p.X = q.X;
            }

            if (Math.Abs(p.Y - q.Y) <= SnapTolerance)
            {
                p.Y = q.Y;
            }
        }

        return p;
    }

    private void WritePoints(List<Point> pts)
    {
        var parts = new string[pts.Count];
        for (var i = 0; i < pts.Count; i++)
        {
            parts[i] = FormattableString.Invariant(
                $"{Math.Round(pts[i].X, 1):0.#},{Math.Round(pts[i].Y, 1):0.#}");
        }

        Points = string.Join(" ", parts);
        SchedulePersist();
    }

    /// <summary>防抖调度源码回写（仅 DEBUG + 调试器附加时真正启用，逻辑在共享引擎）。</summary>
    private void SchedulePersist()
    {
        if (!PersistToSourceXaml || string.IsNullOrWhiteSpace(Points))
        {
            return;
        }

        // Points 总是写；宽度 / 方向仅在偏离默认值时写入，保持 XAML 干净
        var updates = new List<KeyValuePair<string, string>> { new(nameof(Points), Points) };
        if (Math.Abs(PipeWidth - DefaultPipeWidth) > 0.05)
        {
            updates.Add(new(nameof(PipeWidth), FormattableString.Invariant($"{Math.Round(PipeWidth, 1):0.#}")));
        }

        if (Direction != GlassFlowDirection.Forward)
        {
            updates.Add(new(nameof(Direction), Direction.ToString()));
        }

        // 无名控件用初始 Points 指纹定位；有 x:Name 时指纹被忽略
        var fingerprints = new List<KeyValuePair<string, string>>();
        if (string.IsNullOrEmpty(Name) && !string.IsNullOrWhiteSpace(_persistSeedPoints))
        {
            fingerprints.Add(new(nameof(Points), _persistSeedPoints));
        }

        GlassDesignPersist.Schedule(this, updates, fingerprints);
    }

    private static double DistanceToSegment(Point p, Point a, Point b)
    {
        var ab = b - a;
        var lenSq = ab.LengthSquared;
        if (lenSq < 0.001)
        {
            return (p - a).Length;
        }

        var t = Math.Clamp(((p - a) * ab) / lenSq, 0.0, 1.0);
        return (p - (a + ab * t)).Length;
    }

    #endregion
}
