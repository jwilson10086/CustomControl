using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CustomControl.Controls;

/// <summary>
/// 附加属性：给任意控件开启可视化编辑——
/// - 拖动控件本体：调整位置（Canvas.Left / Canvas.Top，取整对齐）；
/// - 拖右侧中点手柄：改宽度；下侧中点：改高度；右下角：宽高一起缩放。
/// DEBUG 编译且附加调试器时，操作结束约 0.5 秒自动把
/// Canvas.Left / Canvas.Top / Width / Height 写回项目源 *.xaml。
///
/// 用法（父容器必须是 Canvas）：
/// <controls:GlassPump controls:GlassEditor.Editing="True" ... />
/// </summary>
public static class GlassEditor
{
    public static bool GetEditing(DependencyObject obj)
        => (bool)obj.GetValue(EditingProperty);

    public static void SetEditing(DependencyObject obj, bool value)
        => obj.SetValue(EditingProperty, value);

    public static readonly DependencyProperty EditingProperty =
        DependencyProperty.RegisterAttached("Editing", typeof(bool), typeof(GlassEditor),
            new PropertyMetadata(false, OnEditingChanged));

    private static readonly DependencyProperty StateProperty =
        DependencyProperty.RegisterAttached("State", typeof(EditorState), typeof(GlassEditor),
            new PropertyMetadata(null));

    private static void OnEditingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement el || DesignerProperties.GetIsInDesignMode(el))
        {
            return;
        }

        // 先清理旧状态，保证反复开关不叠加处理器
        if (el.GetValue(StateProperty) is EditorState old)
        {
            old.Detach();
            el.SetValue(StateProperty, null);
        }

        if ((bool)e.NewValue)
        {
            var state = new EditorState(el);
            el.SetValue(StateProperty, state);
            state.Attach();
        }
    }
}

/// <summary>单个被编辑控件的全部编辑行为：本体拖动 + 三手柄缩放 + 回写。</summary>
internal sealed class EditorState
{
    private const double HandleSize = 10;
    private const double MinSize = 12;

    private readonly FrameworkElement _el;
    private readonly Dictionary<string, string> _seeds = new();

    private bool _attached;
    private bool _moving;
    private Point _dragOrigin;
    private double _origLeft;
    private double _origTop;
    private GlassEditorAdorner? _adorner;

    internal EditorState(FrameworkElement el)
    {
        _el = el;
    }

    internal void Attach()
    {
        if (_attached)
        {
            return;
        }

        _attached = true;
        _el.Loaded += OnLoaded;
        _el.Unloaded += OnUnloaded;

        // 编辑模式下拦截点击：拖动本体，同时屏蔽控件自身交互
        _el.PreviewMouseLeftButtonDown += OnMoveBegin;
        _el.PreviewMouseMove += OnMoveDelta;
        _el.PreviewMouseLeftButtonUp += OnMoveEnd;
        _el.LostMouseCapture += (_, _) => _moving = false;

        GlassDesignPersist.WrittenBack += OnWrittenBack;
    }

    internal void Detach()
    {
        if (!_attached)
        {
            return;
        }

        _attached = false;
        _el.Loaded -= OnLoaded;
        _el.Unloaded -= OnUnloaded;
        _el.PreviewMouseLeftButtonDown -= OnMoveBegin;
        _el.PreviewMouseMove -= OnMoveDelta;
        _el.PreviewMouseLeftButtonUp -= OnMoveEnd;

        GlassDesignPersist.WrittenBack -= OnWrittenBack;

        RemoveAdorner();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => AddAdorner();

    private void OnUnloaded(object? sender, RoutedEventArgs e) => RemoveAdorner();

    private void AddAdorner()
    {
        if (_adorner is not null || DesignerProperties.GetIsInDesignMode(_el))
        {
            return;
        }

        var layer = AdornerLayer.GetAdornerLayer(_el);
        if (layer is null)
        {
            return;
        }

        _adorner = new GlassEditorAdorner(_el, this);
        layer.Add(_adorner);
    }

    private void RemoveAdorner()
    {
        if (_adorner is null)
        {
            return;
        }

        var layer = AdornerLayer.GetAdornerLayer(_el);
        layer?.Remove(_adorner);
        _adorner = null;
    }

    #region 本体拖动

    private void OnMoveBegin(object sender, MouseButtonEventArgs e)
    {
        var canvas = GetCanvas();
        if (canvas is null)
        {
            return;
        }

        _dragOrigin = e.GetPosition(canvas);

        // NaN 视为 0 参与计算；拖动后总会写入显式值
        _origLeft = double.IsNaN(Canvas.GetLeft(_el)) ? 0 : Canvas.GetLeft(_el);
        _origTop = double.IsNaN(Canvas.GetTop(_el)) ? 0 : Canvas.GetTop(_el);

        CaptureSeeds("Canvas.Left", _origLeft, "0.#");
        CaptureSeeds("Canvas.Top", _origTop, "0.#");

        _moving = true;
        _el.CaptureMouse();
        _el.Cursor = Cursors.SizeAll;
        e.Handled = true;
    }

    private void OnMoveDelta(object sender, MouseEventArgs e)
    {
        if (!_moving || !_el.IsMouseCaptured)
        {
            return;
        }

        var canvas = GetCanvas();
        if (canvas is null)
        {
            return;
        }

        var pos = e.GetPosition(canvas);
        Canvas.SetLeft(_el, Math.Round(Math.Max(0, _origLeft + pos.X - _dragOrigin.X)));
        Canvas.SetTop(_el, Math.Round(Math.Max(0, _origTop + pos.Y - _dragOrigin.Y)));
        e.Handled = true;
    }

    private void OnMoveEnd(object sender, MouseButtonEventArgs e)
    {
        if (!_moving)
        {
            return;
        }

        _moving = false;
        _el.ReleaseMouseCapture();
        _el.Cursor = null;

        PersistPosition();
        e.Handled = true;
    }

    private Canvas? GetCanvas() => VisualTreeHelper.GetParent(_el) as Canvas;

    #endregion

    #region 缩放（由 Adorner 手柄回调）

    /// <summary>手柄开始缩放：记录该轴初始尺寸作为指纹。</summary>
    internal void NotifyResizeStarted(bool widthInvolved, bool heightInvolved)
    {
        if (widthInvolved)
        {
            CaptureSeeds("Width", EffectiveWidth, "0.#");
        }

        if (heightInvolved)
        {
            CaptureSeeds("Height", EffectiveHeight, "0.#");
        }
    }

    internal double EffectiveWidth => double.IsNaN(_el.Width) ? _el.ActualWidth : _el.Width;

    internal double EffectiveHeight => double.IsNaN(_el.Height) ? _el.ActualHeight : _el.Height;

    internal void SetWidth(double value) => _el.Width = value;

    internal void SetHeight(double value) => _el.Height = value;

    internal void PersistSize(bool widthChanged, bool heightChanged)
    {
        var updates = new List<KeyValuePair<string, string>>();
        var fingerprints = new List<KeyValuePair<string, string>>();

        if (widthChanged)
        {
            updates.Add(new("Width", FormattableString.Invariant($"{Math.Round(EffectiveWidth):0.#}")));
            AddFingerprint(fingerprints, "Width");
        }

        if (heightChanged)
        {
            updates.Add(new("Height", FormattableString.Invariant($"{Math.Round(EffectiveHeight):0.#}")));
            AddFingerprint(fingerprints, "Height");
        }

        Schedule(updates, fingerprints);
    }

    #endregion

    private void PersistPosition()
    {
        var left = double.IsNaN(Canvas.GetLeft(_el)) ? 0 : Canvas.GetLeft(_el);
        var top = double.IsNaN(Canvas.GetTop(_el)) ? 0 : Canvas.GetTop(_el);

        var updates = new List<KeyValuePair<string, string>>
        {
            new("Canvas.Left", FormattableString.Invariant($"{left:0.#}")),
            new("Canvas.Top", FormattableString.Invariant($"{top:0.#}")),
        };
        var fingerprints = new List<KeyValuePair<string, string>>();
        AddFingerprint(fingerprints, "Canvas.Left");
        AddFingerprint(fingerprints, "Canvas.Top");

        Schedule(updates, fingerprints);
    }

    /// <summary>指纹只记录首次捕获的旧值；写回成功后由 WrittenBack 同步为新值。</summary>
    private void CaptureSeeds(string attr, double value, string format)
    {
        if (!_seeds.ContainsKey(attr))
        {
            _seeds[attr] = value.ToString(format);
        }
    }

    private void AddFingerprint(List<KeyValuePair<string, string>> list, string attr)
    {
        if (_seeds.TryGetValue(attr, out var seed))
        {
            list.Add(new(attr, seed));
        }
    }

    private void Schedule(
        List<KeyValuePair<string, string>> updates,
        List<KeyValuePair<string, string>> fingerprints)
    {
        if (!string.IsNullOrEmpty(_el.Name))
        {
            fingerprints.Clear(); // 有 x:Name 时无需指纹
        }

        GlassDesignPersist.Schedule(_el, updates, fingerprints);
    }

    private void OnWrittenBack(
        FrameworkElement el,
        IReadOnlyList<KeyValuePair<string, string>> updates)
    {
        if (!ReferenceEquals(el, _el))
        {
            return;
        }

        // 指纹同步为最新已写入值，同一会话内继续拖动仍可定位
        foreach (var u in updates)
        {
            _seeds[u.Key] = u.Value;
        }
    }
}

/// <summary>
/// 编辑装饰层：虚线选框 + 三个缩放手柄（右缘=宽度、下缘=高度、右下角=宽高）。
/// </summary>
internal sealed class GlassEditorAdorner : Adorner
{
    private enum HandleRole
    {
        East,
        South,
        Corner,
    }

    private const double HandleSize = 10;
    private const double MinSize = 12;

    private readonly EditorState _state;
    private readonly VisualCollection _visuals;
    private readonly Rectangle _frame;
    private readonly Dictionary<HandleRole, Rectangle> _handles = new();
    private HandleRole? _resizing;
    private double _resizeOriginWidth;
    private double _resizeOriginHeight;
    private Point _resizeOriginPoint;

    internal GlassEditorAdorner(FrameworkElement adornedElement, EditorState state)
        : base(adornedElement)
    {
        _state = state;
        _visuals = new VisualCollection(this);

        _frame = new Rectangle
        {
            Stroke = new SolidColorBrush(Color.FromArgb(0xB3, 0x35, 0xD0, 0x7F)),
            StrokeThickness = 1,
            StrokeDashArray = new DoubleCollection { 4, 2 },
            IsHitTestVisible = false,
        };
        _visuals.Add(_frame);

        CreateHandle(HandleRole.East, Cursors.SizeWE);
        CreateHandle(HandleRole.South, Cursors.SizeNS);
        CreateHandle(HandleRole.Corner, Cursors.SizeNWSE);

        adornedElement.LayoutUpdated += (_, _) => InvalidateArrange();
    }

    private void CreateHandle(HandleRole role, Cursor cursor)
    {
        var handle = new Rectangle
        {
            Width = HandleSize,
            Height = HandleSize,
            Fill = new SolidColorBrush(Color.FromArgb(0xE6, 0xFF, 0xFF, 0xFF)),
            Stroke = new SolidColorBrush(Color.FromArgb(0xE6, 0x35, 0xD0, 0x7F)),
            StrokeThickness = 1.5,
            RadiusX = 2,
            RadiusY = 2,
            Cursor = cursor,
            ToolTip = role switch
            {
                HandleRole.East => "拖动调宽度",
                HandleRole.South => "拖动调高度",
                _ => "拖动等比缩放宽高",
            },
        };

        handle.PreviewMouseLeftButtonDown += (_, e) =>
        {
            _resizing = role;
            _resizeOriginWidth = _state.EffectiveWidth;
            _resizeOriginHeight = _state.EffectiveHeight;
            _resizeOriginPoint = e.GetPosition(this);

            _state.NotifyResizeStarted(role != HandleRole.South, role != HandleRole.East);
            handle.CaptureMouse();
            e.Handled = true;
        };
        handle.PreviewMouseMove += (_, e) =>
        {
            if (_resizing is not { } current || !handle.IsMouseCaptured)
            {
                return;
            }

            var pos = e.GetPosition(this);
            var dx = pos.X - _resizeOriginPoint.X;
            var dy = pos.Y - _resizeOriginPoint.Y;

            if (current == HandleRole.East || current == HandleRole.Corner)
            {
                _state.SetWidth(Math.Max(MinSize, Math.Round(_resizeOriginWidth + dx)));
            }

            if (current == HandleRole.South || current == HandleRole.Corner)
            {
                _state.SetHeight(Math.Max(MinSize, Math.Round(_resizeOriginHeight + dy)));
            }

            e.Handled = true;
        };
        handle.PreviewMouseLeftButtonUp += (_, e) =>
        {
            if (_resizing is null)
            {
                return;
            }

            var current = _resizing.Value;
            _resizing = null;
            handle.ReleaseMouseCapture();

            _state.PersistSize(current != HandleRole.South, current != HandleRole.East);
            e.Handled = true;
        };
        handle.LostMouseCapture += (_, _) => _resizing = null;

        _handles[role] = handle;
        _visuals.Add(handle);
    }

    protected override int VisualChildrenCount => _visuals.Count;

    protected override Visual GetVisualChild(int index) => _visuals[index];

    protected override Size ArrangeOverride(Size finalSize)
    {
        var w = ((FrameworkElement)AdornedElement).ActualWidth;
        var h = ((FrameworkElement)AdornedElement).ActualHeight;

        _frame.Arrange(new Rect(0, 0, w, h));

        foreach (var pair in _handles)
        {
            var (role, handle) = (pair.Key, pair.Value);
            var x = role == HandleRole.South ? w / 2 - HandleSize / 2 : w - HandleSize / 2;
            var y = role == HandleRole.East ? h / 2 - HandleSize / 2 : h - HandleSize / 2;
            handle.Arrange(new Rect(x, y, HandleSize, HandleSize));
        }

        return finalSize;
    }
}
