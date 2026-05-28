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
}