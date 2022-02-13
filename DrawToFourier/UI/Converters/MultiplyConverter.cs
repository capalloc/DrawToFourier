using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DrawToFourier.UI.Converters
{
    public class MultiplyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value.GetType() != typeof(int) || parameter.GetType() != typeof(double))
                throw new ArgumentException();

            return (int)value * (double)parameter;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value.GetType() != typeof(int) || parameter.GetType() != typeof(double))
                throw new ArgumentException();

            return (int)value / (double)parameter;
        }
    }
}
