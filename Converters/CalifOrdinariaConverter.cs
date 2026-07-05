using System;
using System.Globalization;
using System.Windows.Data;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Converters;

public class CalifOrdinariaConverter : IMultiValueConverter
{
    // values: p1, p2, p3, sem
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
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
        // truncar a dos decimales para el promedio de parciales (ej. 8.0666 -> 8.06)
        double promTrunc = Math.Truncate(promedioParciales * 100.0) / 100.0;

        // Semestral ya viene como entero o número según reglas; convertir a double
        double semValue = semNum;

        // La calificación ordinaria es el promedio entre promTrunc y semValue, luego aplicar reglas de redondeo:
        double raw = (promTrunc + semValue) / 2.0;

        // Aplicar regla: si raw < 6.0 -> floored integer; si >=6 -> redondeo con .5 hacia arriba
        int final;
        if (raw < 6.0)
        {
            final = (int)Math.Floor(raw);
        }
        else
        {
            final = (int)Math.Round(raw, MidpointRounding.AwayFromZero);
        }

        return final.ToString(CultureInfo.CurrentCulture);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
