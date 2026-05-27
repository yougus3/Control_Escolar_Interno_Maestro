using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly CapParserService _parserService;
    private readonly FileScannerService _scannerService;

    [ObservableProperty] private string _rutaUsb = @"E:\";
    [ObservableProperty] private string? _archivoSeleccionado;
    [ObservableProperty] private string? _evaluacionSeleccionada;
    [ObservableProperty] private string _currentView = "List";

    public ObservableCollection<string> EvaluacionesDisponibles { get; } = new();
    public ObservableCollection<string> ArchivosDisponibles { get; } = new();
    public ObservableCollection<Alumno> Alumnos { get; } = new();

    public MainViewModel()
    {
        _parserService = new CapParserService();
        _scannerService = new FileScannerService();
        EscanearUsb();
    }

    [RelayCommand]
    public void EscanearUsb()
    {
        ArchivosDisponibles.Clear();
        Alumnos.Clear();
        EvaluacionesDisponibles.Clear();
        ArchivoSeleccionado = null;
        EvaluacionSeleccionada = null;
        CurrentView = "List";

        var archivos = _scannerService.ObtenerArchivosCap(RutaUsb);
        foreach (var archivo in archivos)
        {
            ArchivosDisponibles.Add(Path.GetFileName(archivo));
        }

        if (ArchivosDisponibles.Any())
        {
            ArchivoSeleccionado = ArchivosDisponibles.First();
        }
    }

    partial void OnArchivoSeleccionadoChanged(string? value)
    {
        Alumnos.Clear();
        EvaluacionesDisponibles.Clear();
        EvaluacionSeleccionada = null;
        CurrentView = "List";

        if (string.IsNullOrWhiteSpace(value))
            return;

        string rutaCompleta = Path.Combine(RutaUsb, value);
        if (!File.Exists(rutaCompleta))
            return;

        var alumnosProcesados = _parserService.ProcesarArchivo(rutaCompleta);

        if (alumnosProcesados.Any())
        {
            var keys = alumnosProcesados.First().Calificación.ObtenerClaves();

            foreach (var key in keys)
            {
                if (key.Equals("RESFINAL", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("PROMSEM", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                EvaluacionesDisponibles.Add(key);
            }

            foreach (var alumno in alumnosProcesados)
            {
                Alumnos.Add(alumno);
            }

            EvaluacionSeleccionada = EvaluacionesDisponibles.FirstOrDefault();
        }
    }

    partial void OnEvaluacionSeleccionadaChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        string valorMayusculas = value.ToUpperInvariant();

        if (valorMayusculas == "SEM" || valorMayusculas == "EXTRA")
        {
            CurrentView = "List";
        }
        else
        {
            CurrentView = "Parciales";
        }

        foreach (var alumno in Alumnos)
        {
            alumno.ActualizarSeleccion(value);
        }
    }
}