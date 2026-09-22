using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GeneralControl.Controls;

/// <summary>
/// 附加属性：给任意控件开启可视化编辑（父容器必须是 Canvas）——
/// - 拖动控件本体：调整位置（Canvas.Left / Canvas.Top，取整对齐）；
/// - 拖右侧中点手柄：直接改宽度；下侧中点：直接改高度；
/// - 拖右下角手柄：按控件"当前长宽比"等比缩放（每次按下时锁定比例，
///   例如先拉宽成 1:4 后再拖斜角，就保持 1:4 缩放，不会跳回初始比例）。
/// 三个手柄直接画在父 Canvas 上（覆盖层），与控件同层移动，天然跟随。
/// DEBUG 编译且附加调试器时，操作结束约 0.5 秒自动把
/// Canvas.Left / Canvas.Top / Width / Height 写回项目源 *.xaml。
///
/// 用法：
/// <controls:GcPump controls:GcEditor.Editing="True" ... />
/// </summary>
public static class GcEditor
{
    public static bool GetEditing(DependencyObject obj)
        => (bool)obj.GetValue(EditingProperty);

    public static void SetEditing(DependencyObject obj, bool value)
        => obj.SetValue(EditingProperty, value);

    public static readonly DependencyProperty EditingProperty =
        DependencyProperty.RegisterAttached("Editing", typeof(bool), typeof(GcEditor),
            new PropertyMetadata(false, OnEditingChanged));

    private static readonly DependencyProperty StateProperty =
        DependencyProperty.RegisterAttached("State", typeof(EditorState), typeof(GcEditor),
            new PropertyMetadata(null));

    private static void OnEditingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement el || DesignerProperties.GetIsInDesignMode(el))
        {
            return;
        }

        // 非调试环境（直接运行 exe）不进入编辑模式：无手柄、无拖拽拦截
        if (!GcDesignPersist.IsEditingSupported)
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

/// <summary>
/// 单个被编辑控件的编辑行为：本体拖动 + 三个 Canvas 覆盖层手柄缩放 + XAML 回写。
/// </summary>
internal sealed class EditorState
{
    private enum HandleRole
    {
        East,
        South,
        Corner,
    }

    private const double HandleSize = 10;
    private const double MinSize = 12;

    private readonly FrameworkElement _el;
    private readonly Dictionary<string, string> _seeds = new();
    private readonly Dictionary<HandleRole, Rectangle> _handles = new();

    private bool _attached;
    private bool _moving;
    private Point _dragOrigin;
    private double _origLeft;
    private double _origTop;

    private HandleRole? _resizing;
    private double _resizeOriginWidth;
    private double _resizeOriginHeight;
    private Point _resizeOriginPoint;

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

        // 布局变化（拖动/缩放/外部改尺寸）后重算手柄位置
        _el.LayoutUpdated += OnLayoutUpdated;

        GcDesignPersist.WrittenBack += OnWrittenBack;
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
        _el.LayoutUpdated -= OnLayoutUpdated;

        GcDesignPersist.WrittenBack -= OnWrittenBack;

        RemoveHandles();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => EnsureHandles();

    private void OnUnloaded(object? sender, RoutedEventArgs e) => RemoveHandles();

    private void OnLayoutUpdated(object? sender, EventArgs e) => UpdateHandles();

    private Canvas? GetCanvas() => VisualTreeHelper.GetParent(_el) as Canvas;

    #region 手柄（覆盖层：画在父 Canvas 上，与控件同层，天然跟随）

    private void EnsureHandles()
    {
        if (_handles.Count > 0)
        {
            return;
        }

        var canvas = GetCanvas();
        if (canvas is null || DesignerProperties.GetIsInDesignMode(_el))
        {
            return;
        }

        CreateHandle(HandleRole.East, Cursors.SizeWE, "拖动调宽度");
        CreateHandle(HandleRole.South, Cursors.SizeNS, "拖动调高度");
        CreateHandle(HandleRole.Corner, Cursors.SizeNWSE, "拖动按当前比例等比缩放");

        UpdateHandles();
    }

    private void RemoveHandles()
    {
        if (GetCanvas() is { } canvas)
        {
            foreach (var h in _handles.Values)
            {
                canvas.Children.Remove(h);
            }
        }

        _handles.Clear();
    }

    private void CreateHandle(HandleRole role, Cursor cursor, string tip)
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
            ToolTip = tip,
        };

        handle.PreviewMouseLeftButtonDown += (_, e) =>
        {
            _resizing = role;
            _resizeOriginWidth = EffectiveWidth;
            _resizeOriginHeight = EffectiveHeight;
            _resizeOriginPoint = GetCanvas() is { } c ? e.GetPosition(c) : new Point();

            NotifyResizeStarted(role != HandleRole.South, role != HandleRole.East);
            handle.CaptureMouse();
            e.Handled = true;
        };
        handle.PreviewMouseMove += (_, e) => OnHandleMove(handle, e);
        handle.PreviewMouseLeftButtonUp += (_, e) =>
        {
            if (_resizing is null)
            {
                return;
            }

            var current = _resizing.Value;
            _resizing = null;
            handle.ReleaseMouseCapture();

            PersistSize(current != HandleRole.South, current != HandleRole.East);
            e.Handled = true;
        };
        handle.LostMouseCapture += (_, _) => _resizing = null;

        _handles[role] = handle;
        GetCanvas()?.Children.Add(handle);
    }

    private void OnHandleMove(Rectangle handle, MouseEventArgs e)
    {
        if (_resizing is not { } current || !handle.IsMouseCaptured)
        {
            return;
        }

        var canvas = GetCanvas();
        if (canvas is null)
        {
            return;
        }

        var pos = e.GetPosition(canvas);
        var dx = pos.X - _resizeOriginPoint.X;
        var dy = pos.Y - _resizeOriginPoint.Y;

        if (current == HandleRole.East)
        {
            SetWidth(Math.Max(MinSize, Math.Round(_resizeOriginWidth + dx)));
        }
        else if (current == HandleRole.South)
        {
            SetHeight(Math.Max(MinSize, Math.Round(_resizeOriginHeight + dy)));
        }
        else
        {
            // 右下角：按"本次按下时"的长宽比等比缩放（比例被锁定，不随历史操作漂移）
            var ratio = _resizeOriginWidth / Math.Max(_resizeOriginHeight, 1);
            var dominantX = Math.Abs(dx) / Math.Max(_resizeOriginWidth, 1)
                            >= Math.Abs(dy) / Math.Max(_resizeOriginHeight, 1);
            var scale = dominantX
                ? 1 + dx / _resizeOriginWidth
                : 1 + dy / _resizeOriginHeight;

            var w = Math.Max(MinSize, Math.Round(_resizeOriginWidth * scale));
            var h = Math.Max(MinSize, Math.Round(_resizeOriginHeight * scale));
            SetWidth(w);
            SetHeight(h);
        }

        UpdateHandles();
        e.Handled = true;
    }

    /// <summary>按当前 Canvas.Left/Top + 实际尺寸重算三个手柄的位置。</summary>
    private void UpdateHandles()
    {
        if (_handles.Count == 0)
        {
            return;
        }

        var canvas = GetCanvas();
        if (canvas is null)
        {
            return;
        }

        var left = double.IsNaN(Canvas.GetLeft(_el)) ? 0 : Canvas.GetLeft(_el);
        var top = double.IsNaN(Canvas.GetTop(_el)) ? 0 : Canvas.GetTop(_el);
        var w = EffectiveWidth;
        var h = EffectiveHeight;

        foreach (var pair in _handles)
        {
            var (role, handle) = (pair.Key, pair.Value);
            var x = role == HandleRole.South ? left + w / 2 - HandleSize / 2 : left + w - HandleSize / 2;
            var y = role == HandleRole.East ? top + h / 2 - HandleSize / 2 : top + h - HandleSize / 2;
            Canvas.SetLeft(handle, x);
            Canvas.SetTop(handle, y);
        }
    }

    #endregion

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
        UpdateHandles();
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

    #endregion

    #region 尺寸访问与缩放落盘

    /// <summary>手柄开始缩放：记录该轴初始尺寸作为指纹。</summary>
    private void NotifyResizeStarted(bool widthInvolved, bool heightInvolved)
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

    private void SetWidth(double value) => _el.Width = value;

    private void SetHeight(double value) => _el.Height = value;

    private void PersistSize(bool widthChanged, bool heightChanged)
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

        GcDesignPersist.Schedule(_el, updates, fingerprints);
    }

    private void OnWrittenBack(
        FrameworkElement el,
        IReadOnlyList<KeyValuePair<string, string>> updates)
    {
        if (!ReferenceEquals(el, _el))
        {
            return;
        }

        // 指纹同步为最新已写入值，同一会话内继续操作仍可定位
        foreach (var u in updates)
        {
            _seeds[u.Key] = u.Value;
        }
    }

    #endregion
}




