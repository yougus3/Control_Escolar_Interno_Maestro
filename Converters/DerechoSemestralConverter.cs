using System;
using System.Globalization;
using System.Windows.Data;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Converters;

public class DerechoSemestralConverter : IMultiValueConverter
{
    public object Convert(
        object[] values,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        if (values == null || values.Length < 3)
            return false;

        double suma = 0;
        int cantidad = 0;

        foreach (var value in values)
        {
            if (value == null)
                return false;

            string texto = value.ToString()?.Trim() ?? string.Empty;

            if (!double.TryParse(
                    texto.Replace(',', '.'),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double calificacion))
            {
                return false;
            }

            suma += calificacion;
            cantidad++;
        }

        if (cantidad != 3)
            return false;

        double promedio = suma / 3.0;

        return promedio >= 6.0;
    }

    public object[] ConvertBack(
        object value,
        Type[] targetTypes,
        object parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}