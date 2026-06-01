namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

public class ConfiguracionParciales
{
    public bool CapturaDirectaHabilitada { get; set; }

    public bool Parcial1Habilitado { get; set; } = true;
    public bool Parcial2Habilitado { get; set; }
    public bool Parcial3Habilitado { get; set; }
    public bool SemestralHabilitado { get; set; }
}