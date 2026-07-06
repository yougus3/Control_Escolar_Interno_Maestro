using CommunityToolkit.Mvvm.ComponentModel;

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

    // NUEVA PROPIEDAD AGREGADA PARA CONTROLAR EL DERECHO AL EXTRAORDINARIO
    [ObservableProperty]
    private bool _tieneDerechoExtra = true;

    private string _evaluacionActual = string.Empty;

    public Calificación Calificación { get; set; } = new Calificación();

    public void ActualizarSeleccion(string? key)
    {
        _evaluacionActual = key ?? string.Empty;
        ValorSeleccionado = Calificación[key ?? string.Empty];
    }

    partial void OnValorSeleccionadoChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(_evaluacionActual))
            return;

        var newVal = value ?? string.Empty;
        // Evitar asignaciones redundantes que pueden provocar notificaciones recursivas
        if (Calificación[_evaluacionActual] == newVal)
            return;

        Calificación[_evaluacionActual] = newVal;
    }
}