using System.Collections.Generic;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

public class CapParseResult
{
    public List<Alumno> Alumnos { get; set; } = new();

    public List<string> EvaluacionesDisponibles { get; set; } = new();

    public Dictionary<string, string> EvaluacionIdPorNombre { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}