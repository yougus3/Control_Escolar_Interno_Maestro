namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

public class ActividadParcial
{
    public bool Activa { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public double Porcentaje { get; set; }

    public double PuntajeMaximo { get; set; }
}