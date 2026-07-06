using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

public class CapWriterService
{
    public bool GuardarEvaluacion(string filePath, IReadOnlyList<Alumno> alumnos, string nombreEvaluacion, string idEval)
    {
        if (!File.Exists(filePath))
            return false;

        if (string.IsNullOrWhiteSpace(nombreEvaluacion))
            return false;

        if (string.IsNullOrWhiteSpace(idEval))
            return false;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encodingCap = Encoding.GetEncoding("iso-8859-1");

        var lineas = File.ReadAllLines(filePath, encodingCap).ToList();

        Alumno? alumnoActual = null;
        int indiceAlumno = -1;

        string patronNormal = $"CALIFICACION_{idEval}=";
        string patronStr = $"CALIFICACION_{idEval}_STR=";
        string patronLiteral = $"CALIFICACION_{idEval}_LITERAL=";

        for (int i = 0; i < lineas.Count; i++)
        {
            string lineaTrim = lineas[i].Trim();

            if (lineaTrim.StartsWith("[Alumno_", StringComparison.OrdinalIgnoreCase) &&
                lineaTrim.EndsWith("]"))
            {
                indiceAlumno++;

                alumnoActual = indiceAlumno >= 0 && indiceAlumno < alumnos.Count
                    ? alumnos[indiceAlumno]
                    : null;

                continue;
            }

            if (alumnoActual == null)
                continue;

            string valorBase = alumnoActual.Calificación[nombreEvaluacion]?.Trim() ?? "";
            string valorBaseUpper = valorBase.ToUpperInvariant();

            string valorNormal = valorBase;
            string valorStr = valorBase;
            string valorLiteral = "";

            // Lógica exacta: si es NP se pone la S, de lo contrario se deja en blanco ("") y se borra la "S"
            if (valorBaseUpper == "NP")
            {
                valorNormal = "-555";
                valorStr = "NP";
                valorLiteral = "S";
            }

            if (lineaTrim.StartsWith(patronNormal, StringComparison.OrdinalIgnoreCase))
            {
                lineas[i] = $"{patronNormal}{valorNormal}";
            }
            else if (lineaTrim.StartsWith(patronStr, StringComparison.OrdinalIgnoreCase))
            {
                lineas[i] = $"{patronStr}{valorStr}";
            }
            else if (lineaTrim.StartsWith(patronLiteral, StringComparison.OrdinalIgnoreCase))
            {
                lineas[i] = $"{patronLiteral}{valorLiteral}";
            }
        }

        File.WriteAllLines(filePath, lineas, encodingCap);

        return true;
    }
}