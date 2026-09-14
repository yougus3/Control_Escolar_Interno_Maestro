using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

public sealed class ExportacionCalificacionesJson
{
    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } =
        "CEIM_CALIFICACIONES";

    [JsonPropertyName("version")]
    public int Version { get; set; } =
        1;

    [JsonPropertyName("fechaExportacion")]
    public string FechaExportacion { get; set; } =
        string.Empty;

    [JsonPropertyName("profesor")]
    public ExportacionProfesorJson Profesor { get; set; } =
        new();

    [JsonPropertyName("materias")]
    public List<ExportacionMateriaJson> Materias { get; set; } =
        new();
}

public sealed class ExportacionProfesorJson
{
    [JsonPropertyName("clave")]
    public string Clave { get; set; } =
        string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } =
        string.Empty;
}

public sealed class ExportacionMateriaJson
{
    [JsonPropertyName("archivoCap")]
    public string ArchivoCap { get; set; } =
        string.Empty;

    [JsonPropertyName("claveMateria")]
    public string ClaveMateria { get; set; } =
        string.Empty;

    [JsonPropertyName("evaluaciones")]
    public ExportacionEvaluacionesJson Evaluaciones { get; set; } =
        new();
}

public sealed class ExportacionEvaluacionesJson
{
    [JsonPropertyName("P1")]
    public List<ExportacionAlumnoJson> P1 { get; set; } =
        new();

    [JsonPropertyName("P2")]
    public List<ExportacionAlumnoJson> P2 { get; set; } =
        new();

    [JsonPropertyName("P3")]
    public List<ExportacionAlumnoJson> P3 { get; set; } =
        new();

    [JsonPropertyName("SEM")]
    public List<ExportacionAlumnoJson> SEM { get; set; } =
        new();

    [JsonPropertyName("PREEXTRAORDINARIO")]
    public List<ExportacionAlumnoJson> PREEXTRAORDINARIO { get; set; } =
        new();
}

public sealed class ExportacionAlumnoJson
{
    [JsonPropertyName("matricula")]
    public string Matricula { get; set; } =
        string.Empty;

    [JsonPropertyName("nombreAlumno")]
    public string NombreAlumno { get; set; } =
        string.Empty;

    [JsonPropertyName("calificacion")]
    public double Calificacion { get; set; }
}