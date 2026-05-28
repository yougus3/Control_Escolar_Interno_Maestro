using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly CapParserService _parserService;
    private readonly CapWriterService _writerService;
    private readonly FileScannerService _scannerService;

    private readonly Dictionary<string, string> _mapaArchivos =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, string> _evaluacionIdPorNombre =
        new(StringComparer.OrdinalIgnoreCase);

    private string? _archivoCompletoActual;

    // Expose the full file path of the currently selected CAP file so other
    // view models (e.g. ParcialesViewModel) can use the file name as a
    // stable key when saving JSON data.
    public string? ArchivoCompletoActual => _archivoCompletoActual;

    public ParcialesViewModel ParcialesVm { get; }

    [ObservableProperty] private string _rutaUsb = @"E:\";
    [ObservableProperty] private string? _archivoSeleccionado;
    [ObservableProperty] private string? _evaluacionSeleccionada;
    [ObservableProperty] private string _currentView = "List";

    public ObservableCollection<string> EvaluacionesDisponibles { get; } = new();
    public ObservableCollection<string> ArchivosDisponibles { get; } = new();
    public ObservableCollection<Alumno> Alumnos { get; } = new();

    public bool EsExtraSeleccionado =>
        string.Equals(EvaluacionSeleccionada, "EXTRA", StringComparison.OrdinalIgnoreCase);

    public MainViewModel()
    {
        _parserService = new CapParserService();
        _writerService = new CapWriterService();
        _scannerService = new FileScannerService();

        ParcialesVm = new ParcialesViewModel(this);

        EscanearUsb();
    }

    [RelayCommand]
    public void EscanearUsb()
    {
        ArchivosDisponibles.Clear();
        Alumnos.Clear();
        EvaluacionesDisponibles.Clear();
        _mapaArchivos.Clear();
        _evaluacionIdPorNombre.Clear();
        _archivoCompletoActual = null;

        ArchivoSeleccionado = null;
        EvaluacionSeleccionada = null;
        CurrentView = "List";

        var archivos = _scannerService.ObtenerArchivosCap(RutaUsb);

        foreach (var archivo in archivos)
        {
            string nombreVisual = _parserService.ObtenerNombreVisualArchivo(archivo);
            _mapaArchivos[nombreVisual] = archivo;
            ArchivosDisponibles.Add(nombreVisual);
        }

        if (ArchivosDisponibles.Any())
        {
            ArchivoSeleccionado = ArchivosDisponibles.First();
        }

        OnPropertyChanged(nameof(EsExtraSeleccionado));
    }

    partial void OnArchivoSeleccionadoChanged(string? value)
    {
        Alumnos.Clear();
        EvaluacionesDisponibles.Clear();
        _evaluacionIdPorNombre.Clear();
        EvaluacionSeleccionada = null;
        CurrentView = "List";
        _archivoCompletoActual = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            OnPropertyChanged(nameof(EsExtraSeleccionado));
            return;
        }

        if (!_mapaArchivos.TryGetValue(value, out string? rutaCompleta))
        {
            OnPropertyChanged(nameof(EsExtraSeleccionado));
            return;
        }

        if (!File.Exists(rutaCompleta))
        {
            OnPropertyChanged(nameof(EsExtraSeleccionado));
            return;
        }

        _archivoCompletoActual = rutaCompleta;

        var resultado = _parserService.ProcesarArchivoCompleto(rutaCompleta);

        foreach (var kvp in resultado.EvaluacionIdPorNombre)
        {
            _evaluacionIdPorNombre[kvp.Key] = kvp.Value;
        }

        foreach (var eval in resultado.EvaluacionesDisponibles)
        {
            EvaluacionesDisponibles.Add(eval);
        }

        foreach (var alumno in resultado.Alumnos)
        {
            Alumnos.Add(alumno);
        }

        EvaluacionSeleccionada = EvaluacionesDisponibles.FirstOrDefault();

        OnPropertyChanged(nameof(EsExtraSeleccionado));
    }

    partial void OnEvaluacionSeleccionadaChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            CurrentView = "List";
            OnPropertyChanged(nameof(EsExtraSeleccionado));
            return;
        }

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

        OnPropertyChanged(nameof(EsExtraSeleccionado));
    }

    [RelayCommand]
    public void Guardar()
    {
        if (string.IsNullOrWhiteSpace(_archivoCompletoActual))
        {
            MessageBox.Show(
                "No hay archivo cargado para guardar.",
                "Aviso",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(EvaluacionSeleccionada))
        {
            MessageBox.Show(
                "No hay evaluación seleccionada.",
                "Aviso",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (!_evaluacionIdPorNombre.TryGetValue(EvaluacionSeleccionada, out var idEval))
        {
            MessageBox.Show(
                "No se pudo localizar el ID de la evaluación en el CAP.",
                "Aviso",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (string.Equals(CurrentView, "Parciales", StringComparison.OrdinalIgnoreCase))
        {
            ParcialesVm.PrepararGuardado();
        }

        var ok = _writerService.GuardarEvaluacion(
            _archivoCompletoActual,
            Alumnos.ToList(),
            EvaluacionSeleccionada,
            idEval);

        MessageBox.Show(
            ok ? "Guardado correcto." : "No se pudo guardar el archivo CAP.",
            ok ? "OK" : "Error",
            MessageBoxButton.OK,
            ok ? MessageBoxImage.Information : MessageBoxImage.Error);
    }
}