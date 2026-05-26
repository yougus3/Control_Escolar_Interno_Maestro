using System;
using System.Collections.Generic;
using System.IO;
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

        if (!File.Exists(rutaJson)) return mapa;

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
                        if (!mapa.ContainsKey(matricula)) mapa.Add(matricula, grupo);
                    }
                }
            }
        }
        catch { }
        return mapa;
    }

    public List<Alumno> ProcesarArchivo(string filePath)
    {
        var listadoAlumnos = new List<Alumno>();
        if (!File.Exists(filePath)) return listadoAlumnos;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encodingCap = Encoding.GetEncoding("iso-8859-1");
        var lineas = File.ReadAllLines(filePath, encodingCap);
        
        var mapaGrupos = CargarMapaGrupos();
        var mapaEvaluaciones = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var linea in lineas)
        {
            string l = linea.Trim();
            if (l.StartsWith("ID_EVAL", StringComparison.OrdinalIgnoreCase) && l.Contains("=") && l.Contains("_STR", StringComparison.OrdinalIgnoreCase))
            {
                var partes = l.Split('=');
                if (partes.Length == 2)
                {
                    string claveCompleta = partes[0].Trim(); 
                    string nombreColumnaReal = partes[1].Trim(); 
                    string idEval = claveCompleta.Replace("ID_", "").Replace("_STR", "");
                    mapaEvaluaciones[idEval] = nombreColumnaReal;
                }
            }
        }

        Alumno? alumnoActual = null;

        foreach (var linea in lineas)
        {
            string l = linea.Trim();
            if (string.IsNullOrWhiteSpace(l)) continue;

            if (l.StartsWith("[Alumno_") && l.EndsWith("]"))
            {
                alumnoActual = new Alumno();
                listadoAlumnos.Add(alumnoActual);
                continue;
            }

            if (alumnoActual != null && l.Contains("="))
            {
                var partes = l.Split('=');
                if (partes.Length < 2) continue;
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
                else if (key.StartsWith("CALIFICACION_", StringComparison.OrdinalIgnoreCase) && key.EndsWith("_STR", StringComparison.OrdinalIgnoreCase))
                {
                    string idEval = key.Replace("CALIFICACION_", "").Replace("_STR", "");
                    if (mapaEvaluaciones.TryGetValue(idEval, out string? columnaDestino))
                    {
                        alumnoActual.Calificación[columnaDestino] = string.IsNullOrWhiteSpace(val) ? "-" : val;
                    }
                }
            }
        }
        return listadoAlumnos;
    }
}