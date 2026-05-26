using CommunityToolkit.Mvvm.ComponentModel;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

public partial class Alumno : ObservableObject
{
    [ObservableProperty]
    private string _matricula = string.Empty;

    [ObservableProperty]
    private string _nombre = string.Empty;

    [ObservableProperty]
    private string _grupo = string.Empty;

    [ObservableProperty]
    private string _valorSeleccionado = "-";

    public Calificación Calificación { get; set; } = new Calificación();

    public void ActualizarSeleccion(string key)
    {
        ValorSeleccionado = Calificación[key];
    }
}