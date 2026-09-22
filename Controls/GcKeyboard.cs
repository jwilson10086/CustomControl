using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GeneralControl.Controls;

/// <summary>键盘附加属性：为 GcTextBox/GcPasswordBox 添加图标触发弹出键盘。</summary>
public static class GcKeyboard
{
    public static readonly DependencyProperty ShowKeyboardProperty =
        DependencyProperty.RegisterAttached("ShowKeyboard", typeof(bool), typeof(GcKeyboard),
            new PropertyMetadata(false, OnShowKeyboardChanged));

    public static void SetShowKeyboard(DependencyObject obj, bool value) => obj.SetValue(ShowKeyboardProperty, value);
    public static bool GetShowKeyboard(DependencyObject obj) => (bool)obj.GetValue(ShowKeyboardProperty);

    public static readonly DependencyProperty KeyboardTypeProperty =
        DependencyProperty.RegisterAttached("GcKeyboardType", typeof(GcKeyboardType), typeof(GcKeyboard),
            new PropertyMetadata(GcKeyboardType.Alpha));

    public static void SetKeyboardType(DependencyObject obj, GcKeyboardType value) => obj.SetValue(KeyboardTypeProperty, value);
    public static GcKeyboardType GetKeyboardType(DependencyObject obj) => (GcKeyboardType)obj.GetValue(KeyboardTypeProperty);

    public static readonly DependencyProperty MinValueProperty =
        DependencyProperty.RegisterAttached("MinValue", typeof(double?), typeof(GcKeyboard),
            new PropertyMetadata(null));

    public static void SetMinValue(DependencyObject obj, double? value) => obj.SetValue(MinValueProperty, value);
    public static double? GetMinValue(DependencyObject obj) => (double?)obj.GetValue(MinValueProperty);

    public static readonly DependencyProperty MaxValueProperty =
        DependencyProperty.RegisterAttached("MaxValue", typeof(double?), typeof(GcKeyboard),
            new PropertyMetadata(null));

    public static void SetMaxValue(DependencyObject obj, double? value) => obj.SetValue(MaxValueProperty, value);
    public static double? GetMaxValue(DependencyObject obj) => (double?)obj.GetValue(MaxValueProperty);

    public static readonly DependencyProperty DecimalPlacesProperty =
        DependencyProperty.RegisterAttached("DecimalPlaces", typeof(int), typeof(GcKeyboard),
            new PropertyMetadata(2));

    public static void SetDecimalPlaces(DependencyObject obj, int value) => obj.SetValue(DecimalPlacesProperty, value);
    public static int GetDecimalPlaces(DependencyObject obj) => (int)obj.GetValue(DecimalPlacesProperty);

    private static void OnShowKeyboardChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is System.Windows.Controls.TextBox tb && e.NewValue is true)
            tb.Loaded += OnTextBoxLoaded;
        else if (d is System.Windows.Controls.TextBox tb2)
            tb2.Loaded -= OnTextBoxLoaded;
    }

    private static void OnTextBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox tb) return;
        var icon = FindChildByName(tb, "PART_KeyboardIcon") as System.Windows.Controls.Button;
        if (icon != null)
        {
            icon.Click += (s, args) => ShowKeyboardFor(tb);
            icon.Visibility = Visibility.Visible;
        }
    }

    private static DependencyObject? FindChildByName(DependencyObject parent, string name)
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is FrameworkElement fe && fe.Name == name)
                return child;
            var result = FindChildByName(child, name);
            if (result != null) return result;
        }
        return null;
    }

    public static void ShowKeyboardFor(System.Windows.Controls.TextBox tb)
    {
        var keyboard = new GcKeyboardPopup();
        keyboard.ShowFor(
            tb,
            GetKeyboardType(tb),
            GetMinValue(tb),
            GetMaxValue(tb),
            GetDecimalPlaces(tb));
    }
}





