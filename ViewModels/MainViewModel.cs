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
    private readonly ConfiguracionParcialesService _configuracionService;

    private readonly Dictionary<string, string> _mapaArchivos =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, string> _evaluacionIdPorNombre =
        new(StringComparer.OrdinalIgnoreCase);

    private ConfiguracionParciales _configuracionActual = new();

    private string? _archivoCompletoActual;

    public string? ArchivoCompletoActual => _archivoCompletoActual;

    public ParcialesViewModel ParcialesVm { get; }

    [ObservableProperty] private string _rutaUsb = string.Empty;
    [ObservableProperty] private bool _rutaUsbEditable = true;
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
        _configuracionService = new ConfiguracionParcialesService();
        _configuracionActual = _configuracionService.ObtenerConfiguracion();

        ParcialesVm = new ParcialesViewModel(this);

        try
        {
            var drives = DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Removable && d.IsReady)
                .OrderBy(d => d.Name)
                .ToList();

            if (drives.Any() && string.IsNullOrWhiteSpace(_rutaUsb))
            {
                _rutaUsb = drives.First().Name;
                _rutaUsbEditable = false;
            }
        }
        catch
        {
        }

        EscanearUsb();
    }

    public void RecargarConfiguracionYArchivoActual()
    {
        if (!string.IsNullOrWhiteSpace(ArchivoSeleccionado))
        {
            CargarArchivoSeleccionado(ArchivoSeleccionado);
        }
    }

    private static string ObtenerClaveMateriaDesdeRuta(string? rutaCompleta)
    {
        if (string.IsNullOrWhiteSpace(rutaCompleta))
            return string.Empty;

        string nombre = Path.GetFileNameWithoutExtension(rutaCompleta);
        return string.IsNullOrWhiteSpace(nombre)
            ? string.Empty
            : nombre.Trim().Replace(' ', '_');
    }

    private bool EvaluacionEstaHabilitada(string evaluacion)
    {
        if (string.IsNullOrWhiteSpace(evaluacion))
            return false;

        return evaluacion.Trim().ToUpperInvariant() switch
        {
            "P1" => _configuracionActual.Parcial1Habilitado,
            "P2" => _configuracionActual.Parcial2Habilitado,
            "P3" => _configuracionActual.Parcial3Habilitado,
            "SEM" => _configuracionActual.SemestralHabilitado,
            _ => true
        };
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
        CargarArchivoSeleccionado(value);
    }

    private void CargarArchivoSeleccionado(string? value)
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
        string claveMateria = ObtenerClaveMateriaDesdeRuta(rutaCompleta);
        _configuracionActual = _configuracionService.ObtenerConfiguracion(claveMateria);

        var resultado = _parserService.ProcesarArchivoCompleto(rutaCompleta);

        foreach (var kvp in resultado.EvaluacionIdPorNombre)
        {
            _evaluacionIdPorNombre[kvp.Key] = kvp.Value;
        }

        foreach (var eval in resultado.EvaluacionesDisponibles)
        {
            if (EvaluacionEstaHabilitada(eval))
            {
                EvaluacionesDisponibles.Add(eval);
            }
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
            if (valorMayusculas == "SEM")
            {
                SincronizarCalificacionSemestral();
            }
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

        SincronizarCalificacionSemestral();

        MessageBox.Show(
            ok ? "Guardado correcto." : "No se pudo guardar el archivo CAP.",
            ok ? "OK" : "Error",
            MessageBoxButton.OK,
            ok ? MessageBoxImage.Information : MessageBoxImage.Error);
    }

    private void SincronizarCalificacionSemestral()
    {
        if (string.IsNullOrWhiteSpace(ArchivoSeleccionado)) return;

        string claveMateria = string.Empty;
        if (!string.IsNullOrWhiteSpace(ArchivoCompletoActual))
        {
            try
            {
                var nombre = Path.GetFileNameWithoutExtension(ArchivoCompletoActual);
                if (!string.IsNullOrWhiteSpace(nombre)) claveMateria = nombre.Trim().Replace(' ', '_');
            }
            catch
            {
            }
        }

        if (string.IsNullOrWhiteSpace(claveMateria) && !string.IsNullOrWhiteSpace(ArchivoSeleccionado))
        {
            string texto = ArchivoSeleccionado.Trim();
            int indexEspacio = texto.IndexOf(' ');
            if (indexEspacio <= 0) claveMateria = texto.Replace(' ', '_');
            else
            {
                string clave = texto[..indexEspacio].Trim();
                string nombre = texto[(indexEspacio + 1)..].Trim();
                claveMateria = string.IsNullOrWhiteSpace(nombre) ? clave : $"{clave}_{nombre}";
            }
        }

        if (string.IsNullOrWhiteSpace(claveMateria)) return;

        var jsonService = new ParcialJsonService();
        var m1 = jsonService.ObtenerMateria($"{claveMateria}_P1");
        var m2 = jsonService.ObtenerMateria($"{claveMateria}_P2");
        var m3 = jsonService.ObtenerMateria($"{claveMateria}_P3");

        if (m1 == null || m2 == null || m3 == null) return;

        bool p1Activa = m1.Calificaciones.TryGetValue("$CONFIG$", out var c1) &&
                        c1.TryGetValue("AsistenciaActiva", out var aa1) && aa1 > 0;
        bool p2Activa = m2.Calificaciones.TryGetValue("$CONFIG$", out var c2) &&
                        c2.TryGetValue("AsistenciaActiva", out var aa2) && aa2 > 0;
        bool p3Activa = m3.Calificaciones.TryGetValue("$CONFIG$", out var c3) &&
                        c3.TryGetValue("AsistenciaActiva", out var aa3) && aa3 > 0;

        int totalClases = 0;
        if (p1Activa && p2Activa && p3Activa)
        {
            int clasesP1 = c1.TryGetValue("ClasesTotales", out var ct1) ? (int)ct1 : 0;
            int clasesP2 = c2.TryGetValue("ClasesTotales", out var ct2) ? (int)ct2 : 0;
            int clasesP3 = c3.TryGetValue("ClasesTotales", out var ct3) ? (int)ct3 : 0;
            totalClases = clasesP1 + clasesP2 + clasesP3;
        }

        foreach (var alumno in Alumnos)
        {
            string? califP1Str = alumno.Calificación["P1"];
            string? califP2Str = alumno.Calificación["P2"];
            string? califP3Str = alumno.Calificación["P3"];

            bool p1Valida = double.TryParse(califP1Str, out double p1Num);
            bool p2Valida = double.TryParse(califP2Str, out double p2Num);
            bool p3Valida = double.TryParse(califP3Str, out double p3Num);

            if (!p1Valida || !p2Valida || !p3Valida)
            {
                alumno.Calificación["SEM"] = "";
                continue;
            }

            double promedio = (p1Num + p2Num + p3Num) / 3.0;
            int promedioRedondeado = RedondearPromedio(promedio);
            bool cumpleAsistencia = true;
            if (p1Activa && p2Activa && p3Activa && totalClases > 0)
            {
                int faltasP1 = m1.Calificaciones.TryGetValue(alumno.Matricula, out var cap1) &&
                               cap1.TryGetValue("__Inasistencias__", out var f1) ? (int)f1 : 0;
                int faltasP2 = m2.Calificaciones.TryGetValue(alumno.Matricula, out var cap2) &&
                               cap2.TryGetValue("__Inasistencias__", out var f2) ? (int)f2 : 0;
                int faltasP3 = m3.Calificaciones.TryGetValue(alumno.Matricula, out var cap3) &&
                               cap3.TryGetValue("__Inasistencias__", out var f3) ? (int)f3 : 0;
                int totalFaltas = faltasP1 + faltasP2 + faltasP3;
                int asistencias = totalClases - totalFaltas;
                double porcentajeAsistencia = ((double)asistencias / totalClases) * 100.0;
                cumpleAsistencia = porcentajeAsistencia >= 80.0;
            }

            if (cumpleAsistencia)
            {
                alumno.Calificación["SEM"] =
                    promedioRedondeado.ToString();
            }
            else
            {
                alumno.Calificación["SEM"] = "0";
            }
        }
    }

    private int RedondearPromedio(double promedio)
    {
        if (promedio < 6.0)
            return (int)Math.Floor(promedio);

        return (int)Math.Round(promedio, MidpointRounding.AwayFromZero);
    }
}