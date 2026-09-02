using System;
using System.Globalization;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services
{
    public static class NumberUtils
    {
        public static double TruncateOneDecimal(double value)
        {
            return Math.Truncate(value * 10.0) / 10.0;
        }

        public static decimal TruncateOneDecimal(decimal value)
        {
            return Math.Truncate(value * 10m) / 10m;
        }

        public static string ToOneDecimalString(double value)
        {
            return TruncateOneDecimal(value).ToString("0.0", CultureInfo.InvariantCulture);
        }

        public static string ToOneDecimalString(decimal value)
        {
            return TruncateOneDecimal(value).ToString("0.0", CultureInfo.InvariantCulture);
        }

        public static string ToSmartString(double value)
        {
            double truncated = TruncateOneDecimal(value);

            // Contar dígitos de la parte entera (valor absoluto)
            double absIntPartD = Math.Truncate(Math.Abs(truncated));
            long absIntPart = (long)absIntPartD;
            int digits = 1;
            if (absIntPart > 0)
            {
                digits = (int)Math.Floor(Math.Log10(absIntPart)) + 1;
            }

            // Regla solicitada:
            // - Si la parte entera tiene 2 o más dígitos -> mostrar solo entero (sin decimales)
            // - Si la parte entera tiene 1 dígito -> mostrar truncado a 1 decimal
            if (digits >= 2)
            {
                long intPart = (long)Math.Truncate(truncated);
                return intPart.ToString(CultureInfo.InvariantCulture);
            }

            // Parte entera de 1 dígito -> mostrar truncado a 1 decimal (o entero si no tiene fracción)
            if (Math.Abs(truncated - Math.Truncate(truncated)) < 0.0000001)
                return ((long)Math.Truncate(truncated)).ToString(CultureInfo.InvariantCulture);

            return truncated.ToString("0.0", CultureInfo.InvariantCulture);
        }

        public static string ToSmartString(decimal value)
        {
            decimal truncated = TruncateOneDecimal(value);

            // Contar dígitos de la parte entera (valor absoluto)
            decimal absIntPartD = Math.Truncate(Math.Abs(truncated));
            long absIntPart = (long)absIntPartD;
            int digits = 1;
            if (absIntPart > 0)
            {
                digits = (int)Math.Floor(Math.Log10(absIntPart)) + 1;
            }

            if (digits >= 2)
            {
                long intPart = (long)Math.Truncate(truncated);
                return intPart.ToString(CultureInfo.InvariantCulture);
            }

            if (truncated == Math.Truncate(truncated))
                return ((long)Math.Truncate(truncated)).ToString(CultureInfo.InvariantCulture);

            return truncated.ToString("0.0", CultureInfo.InvariantCulture);
        }
    }
}
