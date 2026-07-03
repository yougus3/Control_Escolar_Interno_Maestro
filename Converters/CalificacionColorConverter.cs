using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Converters
{
    public class CalificacionColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));

            string valStr = value.ToString() ?? "";
            
            if (double.TryParse(valStr, out double calif))
            {
                if (calif < 6.0)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626")); // Rojo (reprobado)
                else
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A")); // Verde (aprobado)
            }

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}