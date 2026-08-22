using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CustomControl.Effects;

/// <summary>
/// 3D 倾斜行为：让任意元素（卡片/按钮/面板）以 3D 透视感跟随鼠标倾斜，
/// 产生悬浮立体的玻璃质感。通过旋转 + 斜切 + 缩放的组合矩阵近似透视投影
/// （.NET Core WPF 已移除 PlanarProjection，此为等价实现）。
/// 用法：<c>CustomControl:Behavior.Tilt="True"</c>
/// </summary>
public static class TiltBehavior
{
    public static readonly DependencyProperty TiltProperty =
        DependencyProperty.RegisterAttached(
            "Tilt",
            typeof(bool),
            typeof(TiltBehavior),
            new PropertyMetadata(false, OnTiltChanged));

    public static readonly DependencyProperty MaxAngleProperty =
        DependencyProperty.RegisterAttached(
            "MaxAngle",
            typeof(double),
            typeof(TiltBehavior),
            new PropertyMetadata(8.0));

    public static readonly DependencyProperty MaxSkewProperty =
        DependencyProperty.RegisterAttached(
            "MaxSkew",
            typeof(double),
            typeof(TiltBehavior),
            new PropertyMetadata(9.0));

    public static readonly DependencyProperty HoverScaleProperty =
        DependencyProperty.RegisterAttached(
            "HoverScale",
            typeof(double),
            typeof(TiltBehavior),
            new PropertyMetadata(1.04));

    private static readonly DependencyProperty TiltStateProperty =
        DependencyProperty.RegisterAttached(
            "TiltState",
            typeof(TiltState),
            typeof(TiltBehavior),
            new PropertyMetadata(null));

    public static bool GetTilt(DependencyObject obj) => (bool)obj.GetValue(TiltProperty);
    public static void SetTilt(DependencyObject obj, bool value) => obj.SetValue(TiltProperty, value);

    public static double GetMaxAngle(DependencyObject obj) => (double)obj.GetValue(MaxAngleProperty);
    public static void SetMaxAngle(DependencyObject obj, double value) => obj.SetValue(MaxAngleProperty, value);

    public static double GetMaxSkew(DependencyObject obj) => (double)obj.GetValue(MaxSkewProperty);
    public static void SetMaxSkew(DependencyObject obj, double value) => obj.SetValue(MaxSkewProperty, value);

    public static double GetHoverScale(DependencyObject obj) => (double)obj.GetValue(HoverScaleProperty);
    public static void SetHoverScale(DependencyObject obj, double value) => obj.SetValue(HoverScaleProperty, value);

    private static void OnTiltChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            if (element.GetValue(TiltStateProperty) is null)
            {
                element.SetValue(TiltStateProperty, new TiltState(element));
            }
        }
        else
        {
            if (element.GetValue(TiltStateProperty) is TiltState state)
            {
                state.Detach();
                element.ClearValue(TiltStateProperty);
            }
        }
    }

    private sealed class TiltState
    {
        private readonly FrameworkElement _element;
        private readonly DispatcherTimer _timer;
        private readonly MatrixTransform _transform;

        private double _dx;
        private double _dy;
        private bool _inside;

        public TiltState(FrameworkElement element)
        {
            _element = element;
            _transform = new MatrixTransform(Matrix.Identity);
            _element.RenderTransform = _transform;
            _element.RenderTransformOrigin = new Point(0.5, 0.5);

            _timer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(16),
            };
            _timer.Tick += OnTick;
            _timer.Start();

            _element.MouseEnter += OnMouseEnter;
            _element.MouseMove += OnMouseMove;
            _element.MouseLeave += OnMouseLeave;
        }

        public void Detach()
        {
            _timer.Stop();
            _timer.Tick -= OnTick;
            _element.MouseEnter -= OnMouseEnter;
            _element.MouseMove -= OnMouseMove;
            _element.MouseLeave -= OnMouseLeave;
            _element.RenderTransform = null;
        }

        private void OnMouseEnter(object sender, MouseEventArgs e) => UpdateMouse(e);

        private void OnMouseMove(object sender, MouseEventArgs e) => UpdateMouse(e);

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            _inside = false;
            _dx = 0;
            _dy = 0;
        }

        private void UpdateMouse(MouseEventArgs e)
        {
            if (_element.ActualWidth <= 0 || _element.ActualHeight <= 0)
            {
                return;
            }

            var pos = e.GetPosition(_element);
            _dx = (pos.X / _element.ActualWidth - 0.5) * 2;
            _dy = (pos.Y / _element.ActualHeight - 0.5) * 2;
            _inside = true;
        }

        private void OnTick(object? sender, EventArgs e)
        {
            // 平滑回中
            if (!_inside)
            {
                _dx = Lerp(_dx, 0, 0.18);
                _dy = Lerp(_dy, 0, 0.18);
                if (Math.Abs(_dx) < 0.001 && Math.Abs(_dy) < 0.001)
                {
                    _dx = 0;
                    _dy = 0;
                }
            }

            var maxAngle = (double)_element.GetValue(MaxAngleProperty);
            var maxSkew = (double)_element.GetValue(MaxSkewProperty);
            var scale = _inside ? (double)_element.GetValue(HoverScaleProperty) : 1.0;

            // 旋转 + 斜切 + 缩放 组合矩阵，近似 3D 透视倾斜
            var rotate = Matrix.Identity;
            rotate.Rotate(_dx * maxAngle);
            var skew = Matrix.Identity;
            skew.Skew(-_dy * maxSkew, _dx * maxSkew);
            var scaleMatrix = Matrix.Identity;
            scaleMatrix.Scale(scale, scale);

            _transform.Matrix = rotate * skew * scaleMatrix;
        }

        private static double Lerp(double from, double to, double t) => from + (to - from) * t;
    }
}
