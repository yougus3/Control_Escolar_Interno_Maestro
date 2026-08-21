using System.Collections.Generic;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

public class MateriaParcial
{
    public List<ActividadParcial> Actividades { get; set; } = new();

    // Matrícula -> actividad -> valor obtenido
    public Dictionary<string, Dictionary<string, double>> Calificaciones { get; set; } = new();

    // Porcentaje acumulado definido por el docente para este CAP+parcial.
    // Se guarda en parciales.json junto a actividades y calificaciones.
    public double PorcentajeAcumulado { get; set; }

    // Backups para preextraordinario: se usan para restaurar actividades y capturas
    // cuando se revierte el pre.
    public List<ActividadParcial>? ActividadesBackup { get; set; }

    // Datos originales por alumno (capturas en P2/P3) antes de aplicar PRE.
    public Dictionary<string, PreOriginalData>? PreOriginals { get; set; }
}