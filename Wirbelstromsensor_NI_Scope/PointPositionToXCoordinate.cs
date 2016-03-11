using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace Wirbelstromsensor_NI_Scope.Converter
{
    class PointPositionToXCoordinate : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double v  = ((Point)value).X;
            return v;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Point p = new Point(((double)value), (double)0.0);
            return p;
        }
    }
}
