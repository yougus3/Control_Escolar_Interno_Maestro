using System;
using System.Globalization;
using System.Windows.Data;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Converters;

public class ExtraFinalConverter : IMultiValueConverter
{
    // values: p1, p2, p3, sem, extra
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        string extraStr = values.Length > 4 && values[4] is string es ? es : string.Empty;
        if (!string.IsNullOrWhiteSpace(extraStr))
        {
            // Si hay EXTRA en CAP, devolver tal cual (trimmed)
            return extraStr.Trim();
        }

        // Si no hay EXTRA, calcular la calificación ordinaria como en CalifOrdinariaConverter
        double[] nums = new double[3];
        for (int i = 0; i < 3; i++)
        {
            if (values.Length > i && values[i] is string s && double.TryParse(s, out var d))
            {
                nums[i] = d;
            }
            else
            {
                return "";
            }
        }

        string semStr = values.Length > 3 && values[3] is string ss ? ss : string.Empty;
        if (!double.TryParse(semStr, out var semNum))
        {
            return "";
        }

        double promedioParciales = (nums[0] + nums[1] + nums[2]) / 3.0;
        double promTrunc = Math.Truncate(promedioParciales * 10.0) / 10.0;

        double raw = (promTrunc + semNum) / 2.0;

        int final;
        if (raw < 6.0)
        {
            final = 5;
        }
        else
        {
            final = (int)Math.Round(raw, MidpointRounding.AwayFromZero);
        }

        return final.ToString("0.0", culture);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
