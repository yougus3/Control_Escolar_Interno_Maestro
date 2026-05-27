using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

public class CapParserService
{
    private Dictionary<string, string> CargarMapaGrupos()
    {
        var mapa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string rutaJson = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "grupo.json");

        if (!File.Exists(rutaJson))
            return mapa;

        try
        {
            string jsonContent = File.ReadAllText(rutaJson, Encoding.UTF8);
            var datos = JsonSerializer.Deserialize<List<List<string>>>(jsonContent);

            if (datos != null)
            {
                foreach (var relacion in datos)
                {
                    if (relacion.Count >= 2)
                    {
                        string matricula = relacion[0].Trim();
                        string grupo = relacion[1].Trim();

                        if (!mapa.ContainsKey(matricula))
                            mapa.Add(matricula, grupo);
                    }
                }
            }
        }
        catch
        {
        }

        return mapa;
    }

    public string ObtenerNombreVisualArchivo(string filePath)
    {
        if (!File.Exists(filePath))
            return Path.GetFileNameWithoutExtension(filePath);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encodingCap = Encoding.GetEncoding("iso-8859-1");
        var lineas = File.ReadAllLines(filePath, encodingCap);

        string clave = string.Empty;
        string asignatura = string.Empty;

        foreach (var linea in lineas)
        {
            string l = linea.Trim();

            if (l.StartsWith("CLAVEASIGNATURA=", StringComparison.OrdinalIgnoreCase))
            {
                clave = l.Split('=', 2)[1].Trim();
            }
            else if (l.StartsWith("ASIGNATURA_STR=", StringComparison.OrdinalIgnoreCase))
            {
                asignatura = l.Split('=', 2)[1].Trim();
            }

            if (!string.IsNullOrWhiteSpace(clave) &&
                !string.IsNullOrWhiteSpace(asignatura))
            {
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(clave) && string.IsNullOrWhiteSpace(asignatura))
            return Path.GetFileNameWithoutExtension(filePath);

        if (string.IsNullOrWhiteSpace(clave))
            return asignatura;

        if (string.IsNullOrWhiteSpace(asignatura))
            return clave;

        return $"{clave} {asignatura}";
    }

    public CapParseResult ProcesarArchivoCompleto(string filePath)
    {
        var resultado = new CapParseResult();

        if (!File.Exists(filePath))
            return resultado;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encodingCap = Encoding.GetEncoding("iso-8859-1");
        var lineas = File.ReadAllLines(filePath, encodingCap);

        var mapaGrupos = CargarMapaGrupos();
        var mapaEvaluaciones = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var linea in lineas)
        {
            string l = linea.Trim();

            if (l.StartsWith("ID_EVAL", StringComparison.OrdinalIgnoreCase) &&
                l.Contains('=') &&
                l.Contains("_STR", StringComparison.OrdinalIgnoreCase))
            {
                var partes = l.Split('=', 2);
                if (partes.Length == 2)
                {
                    string claveCompleta = partes[0].Trim();
                    string nombreColumnaReal = partes[1].Trim();
                    string idEval = claveCompleta.Replace("ID_", "").Replace("_STR", "");

                    if (idEval.Equals("RESFINAL", StringComparison.OrdinalIgnoreCase) ||
                        nombreColumnaReal.Equals("RESFINAL", StringComparison.OrdinalIgnoreCase) ||
                        idEval.Equals("PROMSEM", StringComparison.OrdinalIgnoreCase) ||
                        nombreColumnaReal.Equals("PROMSEM", StringComparison.OrdinalIgnoreCase) ||
                        idEval.Equals("RESULSEM", StringComparison.OrdinalIgnoreCase) ||
                        nombreColumnaReal.Equals("RESULSEM", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    mapaEvaluaciones[idEval] = nombreColumnaReal;
                    resultado.EvaluacionIdPorNombre[nombreColumnaReal] = idEval;
                }
            }
        }

        foreach (var eval in mapaEvaluaciones.Values.Distinct())
        {
            resultado.EvaluacionesDisponibles.Add(eval);
        }

        Alumno? alumnoActual = null;

        foreach (var linea in lineas)
        {
            string l = linea.Trim();

            if (string.IsNullOrWhiteSpace(l))
                continue;

            if (l.StartsWith("[Alumno_", StringComparison.OrdinalIgnoreCase) && l.EndsWith("]"))
            {
                alumnoActual = new Alumno();
                resultado.Alumnos.Add(alumnoActual);
                continue;
            }

            if (alumnoActual == null || !l.Contains('='))
                continue;

            var partes = l.Split('=', 2);
            if (partes.Length < 2)
                continue;

            string key = partes[0].Trim();
            string val = partes[1].Trim();

            if (key.Equals("Matricula", StringComparison.OrdinalIgnoreCase))
            {
                alumnoActual.Matricula = val;
                alumnoActual.Grupo = mapaGrupos.TryGetValue(val, out string? g) ? g : "S/G";
            }
            else if (key.Equals("Nombre", StringComparison.OrdinalIgnoreCase))
            {
                alumnoActual.Nombre = val;
            }
            else if (key.StartsWith("CALIFICACION_", StringComparison.OrdinalIgnoreCase) &&
                     key.EndsWith("_STR", StringComparison.OrdinalIgnoreCase))
            {
                string idEval = key.Replace("CALIFICACION_", "").Replace("_STR", "");

                if (mapaEvaluaciones.TryGetValue(idEval, out string? columnaDestino))
                {
                    alumnoActual.Calificación[columnaDestino] =
                        string.IsNullOrWhiteSpace(val) ? "" : val;
                }
            }
        }

        return resultado;
    }

    public List<Alumno> ProcesarArchivo(string filePath)
    {
        return ProcesarArchivoCompleto(filePath).Alumnos;
    }
}