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
    /// Computes the rotation angle for the arrow at the target end of a connection.
    /// Bindings: SourceX, SourceY, TargetX, TargetY, SourceDir, TargetDir
    /// SourceDir: +1 = port faces right (normal output), -1 = port faces left (flipped output)
    /// TargetDir: -1 = port faces left (normal input), +1 = port faces right (flipped input)
    /// </summary>
    public class ConnectionArrowAngleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 6 &&
                values[0] is double sx && values[1] is double sy &&
                values[2] is double tx && values[3] is double ty &&
                values[4] is double sDir && values[5] is double tDir)
            {
                // The Bezier's last control point determines arrow direction
                var dx = Math.Abs(tx - sx) * 0.5;
                if (dx < 50) dx = 50;
                // Control point 2 extends in TargetDir direction from target
                var c2x = tx + dx * tDir;
                // Tangent at target = (tx - c2x, ty - ty) = (-dx * tDir, 0) if flat
                // But for non-flat curves, also account for Y
                var tanX = tx - c2x;
                var tanY = 0.0; // Bezier endpoint tangent is horizontal in our control point scheme
                var angle = Math.Atan2(tanY, tanX) * 180.0 / Math.PI;
                return angle;
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Multibinding converter for Bezier path geometry.
    /// Bindings: SourceX, SourceY, TargetX, TargetY, SourceDir, TargetDir
    /// SourceDir: +1 = port faces right, -1 = port faces left
    /// TargetDir: -1 = port faces left,  +1 = port faces right
    /// Control points extend outward from each port in the direction the port faces.
    /// </summary>
    public class ConnectionPathConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 6 &&
                values[0] is double sx && values[1] is double sy &&
                values[2] is double tx && values[3] is double ty &&
                values[4] is double sDir && values[5] is double tDir)
            {
                var inv = CultureInfo.InvariantCulture;
                var dx = Math.Abs(tx - sx) * 0.5;
                if (dx < 50) dx = 50;

                // Control point 1: extends from source in source port's facing direction
                var c1x = sx + dx * sDir;
                var c1y = sy;
                // Control point 2: extends from target in target port's facing direction
                var c2x = tx + dx * tDir;
                var c2y = ty;

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
