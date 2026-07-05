using System;
using System.Globalization;
using System.Windows.Data;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Converters;

public class PromediosConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        double[] nums = new double[3];
        int count = 0;
        for (int i = 0; i < 3; i++)
        {
            if (values.Length > i && values[i] is string s && double.TryParse(s, out var d))
            {
                nums[count++] = d;
            }
            else
            {
                return "";
            }
        }

        double promedio = (nums[0] + nums[1] + nums[2]) / 3.0;
        // truncar a dos decimales (ej. 8.0666 -> 8.06)
        double truncado = Math.Truncate(promedio * 100.0) / 100.0;
        return truncado.ToString("0.00", CultureInfo.CurrentCulture);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
