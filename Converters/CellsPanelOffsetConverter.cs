using System;
using System.Globalization;
using System.Windows.Data;

namespace ProjectManager.Converters
{
    /// <summary>
    /// Converts CellsPanelHorizontalOffset to ensure it's never negative for Width binding
    /// </summary>
    public class CellsPanelOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                // Ensure the value is never negative, as negative widths are invalid
                return Math.Max(0, doubleValue);
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}