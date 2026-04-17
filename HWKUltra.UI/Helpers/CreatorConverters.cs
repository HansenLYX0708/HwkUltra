using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace HWKUltra.UI.Helpers
{
    /// <summary>
    /// Converts a hex color string to a SolidColorBrush
    /// </summary>
    public class StringToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorStr && !string.IsNullOrEmpty(colorStr))
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(colorStr);
                    return new SolidColorBrush(color);
                }
                catch { }
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Bool to Visibility converter
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Inverted Bool to Visibility converter
    /// </summary>
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? Visibility.Collapsed : Visibility.Visible;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Null to Visibility converter (visible when not null)
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Inverse null to Visibility converter (visible when null)
    /// </summary>
    public class InverseNullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Non-empty string to Visibility converter (visible when string is not empty)
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is string s && !string.IsNullOrEmpty(s) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Inverts a boolean value (true → false, false → true)
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return true;
        }
    }

    /// <summary>
    /// Converts a hex color string to a semi-transparent brush (for node backgrounds)
    /// </summary>
    public class StringToSemiTransparentBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorStr && !string.IsNullOrEmpty(colorStr))
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(colorStr);
                    color.A = 40; // semi-transparent
                    return new SolidColorBrush(color);
                }
                catch { }
            }
            return new SolidColorBrush(Color.FromArgb(40, 128, 128, 128));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Converts bool IsSelected to border thickness
    /// </summary>
    public class BoolToThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool selected && selected)
                return new Thickness(2);
            return new Thickness(1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Converts bool IsSelected to highlight color
    /// </summary>
    public class BoolToHighlightBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool selected && selected)
                return new SolidColorBrush(Color.FromRgb(0, 120, 215));
            return new SolidColorBrush(Color.FromArgb(100, 128, 128, 128));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Converts SourceX and TargetX into rotation angle for the arrow at the target end.
    /// Bindings: SourceX, SourceY, TargetX, TargetY → angle in degrees
    /// </summary>
    public class ConnectionArrowAngleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 4 &&
                values[0] is double sx && values[1] is double sy &&
                values[2] is double tx && values[3] is double ty)
            {
                // Compute tangent angle at target point of the Bezier
                // The last control point of our cubic Bezier is (tx - dx, ty), endpoint is (tx, ty)
                var dx = Math.Abs(tx - sx) * 0.5;
                if (dx < 30) dx = 30;
                // Last control point
                var cpx = tx - dx;
                var cpy = ty;
                // Tangent vector at endpoint = (tx - cpx, ty - cpy)
                var tanX = tx - cpx;
                var tanY = ty - cpy;
                var angle = Math.Atan2(tanY, tanX) * 180.0 / Math.PI;
                return angle;
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Multibinding converter for Bezier path geometry
    /// </summary>
    public class ConnectionPathConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 4 &&
                values[0] is double sx && values[1] is double sy &&
                values[2] is double tx && values[3] is double ty)
            {
                var inv = CultureInfo.InvariantCulture;
                var dx = Math.Abs(tx - sx) * 0.5;
                if (dx < 30) dx = 30;

                double c1x, c1y, c2x, c2y;
                if (sx <= tx)
                {
                    // Normal left-to-right flow
                    c1x = sx + dx; c1y = sy;
                    c2x = tx - dx; c2y = ty;
                }
                else
                {
                    // Right-to-left (flipped nodes or loopback): S-curve
                    c1x = sx + dx; c1y = sy;
                    c2x = tx - dx; c2y = ty;
                }

                return Geometry.Parse(
                    $"M {sx.ToString(inv)},{sy.ToString(inv)} " +
                    $"C {c1x.ToString(inv)},{c1y.ToString(inv)} " +
                    $"{c2x.ToString(inv)},{c2y.ToString(inv)} " +
                    $"{tx.ToString(inv)},{ty.ToString(inv)}");
            }
            return Geometry.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
