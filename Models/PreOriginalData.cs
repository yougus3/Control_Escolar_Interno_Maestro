using System.Collections.Generic;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

public class PreOriginalData
{
    // Capturas originales del alumno para este parcial
    public Dictionary<string, double> CapturasOriginal { get; set; } = new();
}
