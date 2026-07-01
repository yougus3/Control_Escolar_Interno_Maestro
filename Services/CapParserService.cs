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
    public CapParserService()
    {
    }

    private Dictionary<string, string> CargarMapaGrupos()
    {
        try
        {
            using var lite = new LiteDbService();
            return lite.GetGrupos();
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public Dictionary<string, string> ObtenerTodosGrupos()
    {
        return CargarMapaGrupos();
    }

    public void GuardarTodosGrupos(Dictionary<string, string> grupos)
    {
        try
        {
            using var lite = new LiteDbService();
            lite.SaveGrupos(grupos ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }
        catch { }
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

        string resultado = string.IsNullOrWhiteSpace(clave) ? asignatura : string.IsNullOrWhiteSpace(asignatura) ? clave : $"{clave} {asignatura}";

        try
        {
            bool containsExtra = false;
            foreach (var line in File.ReadAllLines(filePath, encodingCap))
            {
                if (line.IndexOf("EXTRA", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    containsExtra = true;
                    break;
                }
            }

            if (!containsExtra)
            {
                var mapa = CargarMapaGrupos();
                bool inAlumno = false;
                foreach (var linea in File.ReadAllLines(filePath, encodingCap))
                {
                    var l = linea.Trim();
                    if (l.StartsWith("[Alumno_", StringComparison.OrdinalIgnoreCase) && l.EndsWith("]"))
                    {
                        inAlumno = true;
                        continue;
                    }

                    if (!inAlumno) continue;

                    if (l.StartsWith("Matricula", StringComparison.OrdinalIgnoreCase) && l.Contains('='))
                    {
                        var partes = l.Split('=', 2);
                        if (partes.Length == 2)
                        {
                            var matricula = partes[1].Trim();
                            if (mapa.TryGetValue(matricula, out var grupo) && !string.IsNullOrWhiteSpace(grupo))
                            {
                                resultado = $"{resultado} ({grupo})";
                            }
                            break;
                        }
                    }
                }
            }
        }
        catch { }

        return resultado;
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

        bool enSeccionEvaluaciones = false;

        foreach (var linea in lineas)
        {
            string l = linea.Trim();

            if (string.IsNullOrWhiteSpace(l))
                continue;

            if (l.StartsWith("[", StringComparison.OrdinalIgnoreCase) && l.EndsWith("]"))
            {
                enSeccionEvaluaciones = l.Equals("[Evaluaciones]", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (!enSeccionEvaluaciones)
                continue;

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

        // Construimos la lista de evaluaciones disponibles a partir de las ID leídas
        // Si el archivo CAP declara evaluaciones las respetamos (P1,P2,P3,SEM,EXTRA, etc.).
        // Solo cuando no hay ninguna evaluación declarada aplicamos la lista por defecto.
        if (mapaEvaluaciones.Count > 0)
        {
            // Queremos respetar el orden lógico P1,P2,P3,SEM si existen, y luego cualquier otra (p.ej. EXTRA)
            var ordenPreferente = new[] { "P1", "P2", "P3", "SEM" };
            var yaAgregadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var o in ordenPreferente)
            {
                if (mapaEvaluaciones.Values.Any(v => string.Equals(v, o, StringComparison.OrdinalIgnoreCase)))
                {
                    resultado.EvaluacionesDisponibles.Add(o);
                    yaAgregadas.Add(o);
                }
            }

            foreach (var nombre in mapaEvaluaciones.Values.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(nombre)) continue;
                if (yaAgregadas.Contains(nombre)) continue;
                // Excluir nombres reservados por seguridad
                if (string.Equals(nombre, "RESFINAL", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(nombre, "PROMSEM", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(nombre, "RESULSEM", StringComparison.OrdinalIgnoreCase))
                    continue;

                resultado.EvaluacionesDisponibles.Add(nombre);
                yaAgregadas.Add(nombre);
            }
        }
        else
        {
            resultado.EvaluacionesDisponibles.Add("P1");
            resultado.EvaluacionesDisponibles.Add("P2");
            resultado.EvaluacionesDisponibles.Add("P3");
            resultado.EvaluacionesDisponibles.Add("SEM");
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