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
        // truncar al primer decimal (ej. 9.4333 -> 9.4)
        double truncado = Math.Truncate(promedio * 10.0) / 10.0;
        return truncado.ToString("0.0", CultureInfo.CurrentCulture);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
