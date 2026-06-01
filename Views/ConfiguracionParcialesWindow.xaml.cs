using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.ViewModels;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views;

public partial class ConfiguracionParcialesWindow : Window, INotifyPropertyChanged
{
    private readonly MainViewModel _mainVm;
    private readonly ConfiguracionParcialesService _configuracionService;
    private readonly CapParserService _parserService;
    private readonly CapWriterService _writerService;
    private readonly FileScannerService _scannerService;

    private readonly Dictionary<string, string> _mapaArchivos = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _evaluacionIdPorNombreDirecta = new(StringComparer.OrdinalIgnoreCase);

    private ConfiguracionParciales _configuracion = new();
    private bool _cargandoDatos;

    private bool _parcial1Habilitado;
    private bool _parcial2Habilitado;
    private bool _parcial3Habilitado;
    private bool _semestralHabilitado;

    private string? _materiaSeleccionadaDirecta;
    private string? _evaluacionSeleccionadaDirecta;
    private Alumno? _alumnoSeleccionadoDirecto;
    private string _calificacionNuevaDirecta = string.Empty;
    private string _estadoDirecto = string.Empty;
    private string _nombreAlumnoDirecto = string.Empty;
    private string _matriculaAlumnoDirecto = string.Empty;
    private string _grupoAlumnoDirecto = string.Empty;
    private string _calificacionActualDirecta = string.Empty;

    public ObservableCollection<string> MateriasDisponibles { get; } = new();
    public ObservableCollection<string> EvaluacionesDisponiblesDirecta { get; } = new();
    public ObservableCollection<Alumno> AlumnosDirectos { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool Parcial1Habilitado
    {
        get => _parcial1Habilitado;
        set
        {
            if (_parcial1Habilitado == value) return;
            _parcial1Habilitado = value;
            OnPropertyChanged();
        }
    }

    public bool Parcial2Habilitado
    {
        get => _parcial2Habilitado;
        set
        {
            if (_parcial2Habilitado == value) return;
            _parcial2Habilitado = value;
            OnPropertyChanged();
        }
    }

    public bool Parcial3Habilitado
    {
        get => _parcial3Habilitado;
        set
        {
            if (_parcial3Habilitado == value) return;
            _parcial3Habilitado = value;
            OnPropertyChanged();
        }
    }

    public bool SemestralHabilitado
    {
        get => _semestralHabilitado;
        set
        {
            if (_semestralHabilitado == value) return;
            _semestralHabilitado = value;
            OnPropertyChanged();
        }
    }

    public string? MateriaSeleccionadaDirecta
    {
        get => _materiaSeleccionadaDirecta;
        set
        {
            if (_materiaSeleccionadaDirecta == value) return;
            _materiaSeleccionadaDirecta = value;
            OnPropertyChanged();
            if (!_cargandoDatos)
            {
                CargarMateriaDirecta();
            }
        }
    }

    public string? EvaluacionSeleccionadaDirecta
    {
        get => _evaluacionSeleccionadaDirecta;
        set
        {
            if (_evaluacionSeleccionadaDirecta == value) return;
            _evaluacionSeleccionadaDirecta = value;
            OnPropertyChanged();
            if (!_cargandoDatos)
            {
                RefrescarAlumnosParaEvaluacion();
            }
        }
    }

    public Alumno? AlumnoSeleccionadoDirecto
    {
        get => _alumnoSeleccionadoDirecto;
        set
        {
            if (_alumnoSeleccionadoDirecto == value) return;
            _alumnoSeleccionadoDirecto = value;
            OnPropertyChanged();
            RefrescarDatosAlumnoSeleccionado();
        }
    }

    public string CalificacionNuevaDirecta
    {
        get => _calificacionNuevaDirecta;
        set
        {
            if (_calificacionNuevaDirecta == value) return;
            _calificacionNuevaDirecta = value;
            OnPropertyChanged();
        }
    }

    public string EstadoDirecto
    {
        get => _estadoDirecto;
        set
        {
            if (_estadoDirecto == value) return;
            _estadoDirecto = value;
            OnPropertyChanged();
        }
    }

    public string NombreAlumnoDirecto
    {
        get => _nombreAlumnoDirecto;
        set
        {
            if (_nombreAlumnoDirecto == value) return;
            _nombreAlumnoDirecto = value;
            OnPropertyChanged();
        }
    }

    public string MatriculaAlumnoDirecto
    {
        get => _matriculaAlumnoDirecto;
        set
        {
            if (_matriculaAlumnoDirecto == value) return;
            _matriculaAlumnoDirecto = value;
            OnPropertyChanged();
        }
    }

    public string GrupoAlumnoDirecto
    {
        get => _grupoAlumnoDirecto;
        set
        {
            if (_grupoAlumnoDirecto == value) return;
            _grupoAlumnoDirecto = value;
            OnPropertyChanged();
        }
    }

    public string CalificacionActualDirecta
    {
        get => _calificacionActualDirecta;
        set
        {
            if (_calificacionActualDirecta == value) return;
            _calificacionActualDirecta = value;
            OnPropertyChanged();
        }
    }

    public ConfiguracionParcialesWindow(MainViewModel mainVm)
    {
        InitializeComponent();

        _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
        _configuracionService = new ConfiguracionParcialesService();
        _parserService = new CapParserService();
        _writerService = new CapWriterService();
        _scannerService = new FileScannerService();

        DataContext = this;

        CargarConfiguracionInicial();
        CargarMateriasDisponibles();

        if (MateriasDisponibles.Any())
        {
            MateriaSeleccionadaDirecta = MateriasDisponibles.FirstOrDefault();
        }
    }

    private void CargarConfiguracionInicial()
    {
        _configuracion = _configuracionService.ObtenerConfiguracion();

        _cargandoDatos = true;
        try
        {
            Parcial1Habilitado = _configuracion.Parcial1Habilitado;
            Parcial2Habilitado = _configuracion.Parcial2Habilitado;
            Parcial3Habilitado = _configuracion.Parcial3Habilitado;
            SemestralHabilitado = _configuracion.SemestralHabilitado;
        }
        finally
        {
            _cargandoDatos = false;
        }
    }

    private void CargarMateriasDisponibles()
    {
        MateriasDisponibles.Clear();
        _mapaArchivos.Clear();

        try
        {
            var archivos = _scannerService.ObtenerArchivosCap(_mainVm.RutaUsb);

            foreach (var archivo in archivos)
            {
                string nombreVisual = _parserService.ObtenerNombreVisualArchivo(archivo);
                _mapaArchivos[nombreVisual] = archivo;
                MateriasDisponibles.Add(nombreVisual);
            }
        }
        catch
        {
            EstadoDirecto = "No se pudieron cargar las materias desde el USB.";
        }
    }

    private bool EvaluacionEstaHabilitada(string evaluacion)
    {
        if (string.IsNullOrWhiteSpace(evaluacion))
            return false;

        return evaluacion.Trim().ToUpperInvariant() switch
        {
            "P1" => _configuracion.Parcial1Habilitado,
            "P2" => _configuracion.Parcial2Habilitado,
            "P3" => _configuracion.Parcial3Habilitado,
            "SEM" => _configuracion.SemestralHabilitado,
            _ => true
        };
    }

    private void CargarMateriaDirecta()
    {
        AlumnosDirectos.Clear();
        EvaluacionesDisponiblesDirecta.Clear();
        _evaluacionIdPorNombreDirecta.Clear();

        if (string.IsNullOrWhiteSpace(MateriaSeleccionadaDirecta))
        {
            EstadoDirecto = "Selecciona una materia.";
            EvaluacionSeleccionadaDirecta = null;
            AlumnoSeleccionadoDirecto = null;
            return;
        }

        if (!_mapaArchivos.TryGetValue(MateriaSeleccionadaDirecta, out string? rutaCompleta))
        {
            EstadoDirecto = "No se encontró el archivo de la materia seleccionada.";
            EvaluacionSeleccionadaDirecta = null;
            AlumnoSeleccionadoDirecto = null;
            return;
        }

        if (!File.Exists(rutaCompleta))
        {
            EstadoDirecto = "El archivo CAP ya no existe.";
            EvaluacionSeleccionadaDirecta = null;
            AlumnoSeleccionadoDirecto = null;
            return;
        }

        try
        {
            _cargandoDatos = true;

            var resultado = _parserService.ProcesarArchivoCompleto(rutaCompleta);

            foreach (var kvp in resultado.EvaluacionIdPorNombre)
            {
                _evaluacionIdPorNombreDirecta[kvp.Key] = kvp.Value;
            }

            foreach (var eval in resultado.EvaluacionesDisponibles)
            {
                if (EvaluacionEstaHabilitada(eval))
                {
                    EvaluacionesDisponiblesDirecta.Add(eval);
                }
            }

            foreach (var alumno in resultado.Alumnos)
            {
                AlumnosDirectos.Add(alumno);
            }

            if (EvaluacionesDisponiblesDirecta.Any())
            {
                EvaluacionSeleccionadaDirecta = EvaluacionesDisponiblesDirecta.First();
            }
            else
            {
                EvaluacionSeleccionadaDirecta = null;
            }

            AlumnoSeleccionadoDirecto = AlumnosDirectos.FirstOrDefault();
            EstadoDirecto = $"Materia cargada: {MateriaSeleccionadaDirecta}";
        }
        finally
        {
            _cargandoDatos = false;
            RefrescarAlumnosParaEvaluacion();
            RefrescarDatosAlumnoSeleccionado();
        }
    }

    private void RefrescarAlumnosParaEvaluacion()
    {
        if (string.IsNullOrWhiteSpace(EvaluacionSeleccionadaDirecta))
        {
            foreach (var alumno in AlumnosDirectos)
            {
                alumno.ActualizarSeleccion(string.Empty);
            }

            CalificacionActualDirecta = string.Empty;
            CalificacionNuevaDirecta = string.Empty;
            return;
        }

        foreach (var alumno in AlumnosDirectos)
        {
            alumno.ActualizarSeleccion(EvaluacionSeleccionadaDirecta);
        }

        RefrescarDatosAlumnoSeleccionado();
    }

    private void RefrescarDatosAlumnoSeleccionado()
    {
        if (AlumnoSeleccionadoDirecto == null)
        {
            NombreAlumnoDirecto = string.Empty;
            MatriculaAlumnoDirecto = string.Empty;
            GrupoAlumnoDirecto = string.Empty;
            CalificacionActualDirecta = string.Empty;
            CalificacionNuevaDirecta = string.Empty;
            return;
        }

        NombreAlumnoDirecto = AlumnoSeleccionadoDirecto.Nombre;
        MatriculaAlumnoDirecto = AlumnoSeleccionadoDirecto.Matricula;
        GrupoAlumnoDirecto = AlumnoSeleccionadoDirecto.Grupo;
        CalificacionActualDirecta = AlumnoSeleccionadoDirecto.ValorSeleccionado;
        CalificacionNuevaDirecta = AlumnoSeleccionadoDirecto.ValorSeleccionado;
    }

    private void GuardarConfiguracion_Click(object sender, RoutedEventArgs e)
    {
        _configuracion.Parcial1Habilitado = Parcial1Habilitado;
        _configuracion.Parcial2Habilitado = Parcial2Habilitado;
        _configuracion.Parcial3Habilitado = Parcial3Habilitado;
        _configuracion.SemestralHabilitado = SemestralHabilitado;

        _configuracionService.GuardarConfiguracion(_configuracion);

        _mainVm.RecargarConfiguracionYArchivoActual();
        CargarMateriasDisponibles();

        if (!string.IsNullOrWhiteSpace(MateriaSeleccionadaDirecta))
        {
            CargarMateriaDirecta();
        }

        MessageBox.Show(
            "Configuración guardada.",
            "OK",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void GuardarDirecta_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(MateriaSeleccionadaDirecta))
        {
            MessageBox.Show(
                "Selecciona una materia.",
                "Aviso",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(EvaluacionSeleccionadaDirecta))
        {
            MessageBox.Show(
                "Selecciona una evaluación.",
                "Aviso",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (AlumnoSeleccionadoDirecto == null)
        {
            MessageBox.Show(
                "Selecciona un alumno.",
                "Aviso",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (!_mapaArchivos.TryGetValue(MateriaSeleccionadaDirecta, out string? rutaCompleta))
        {
            MessageBox.Show(
                "No se pudo localizar el archivo de la materia.",
                "Aviso",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (!_evaluacionIdPorNombreDirecta.TryGetValue(EvaluacionSeleccionadaDirecta, out string? idEval))
        {
            MessageBox.Show(
                "No se pudo localizar el ID de la evaluación en el CAP.",
                "Aviso",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (!TryNormalizarCalificacion(CalificacionNuevaDirecta, out string valorNormalizado))
        {
            MessageBox.Show(
                "La calificación debe ser un número entre 0 y 10.",
                "Aviso",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        AlumnoSeleccionadoDirecto.ValorSeleccionado = valorNormalizado;
        AlumnoSeleccionadoDirecto.Calificación[EvaluacionSeleccionadaDirecta] = valorNormalizado;

        SincronizarConMainVm(EvaluacionSeleccionadaDirecta, AlumnoSeleccionadoDirecto.Matricula, valorNormalizado);

        var ok = _writerService.GuardarEvaluacion(
            rutaCompleta,
            AlumnosDirectos.ToList(),
            EvaluacionSeleccionadaDirecta,
            idEval);

        if (ok)
        {
            CalificacionActualDirecta = valorNormalizado;
            CalificacionNuevaDirecta = valorNormalizado;
            EstadoDirecto = "Calificación guardada en el CAP.";

            MessageBox.Show(
                "Calificación guardada correctamente.",
                "OK",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show(
                "No se pudo guardar el archivo CAP.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void SincronizarConMainVm(string evaluacion, string matricula, string valor)
    {
        if (!string.Equals(_mainVm.ArchivoCompletoActual, _mapaArchivos.TryGetValue(MateriaSeleccionadaDirecta ?? string.Empty, out var ruta) ? ruta : string.Empty, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var alumnoMain = _mainVm.Alumnos.FirstOrDefault(a =>
            string.Equals(a.Matricula, matricula, StringComparison.OrdinalIgnoreCase));

        if (alumnoMain == null)
            return;

        alumnoMain.Calificación[evaluacion] = valor;

        if (string.Equals(_mainVm.EvaluacionSeleccionada, evaluacion, StringComparison.OrdinalIgnoreCase))
        {
            alumnoMain.ActualizarSeleccion(evaluacion);
        }
    }

    private static bool TryNormalizarCalificacion(string texto, out string valorNormalizado)
    {
        valorNormalizado = string.Empty;

        if (string.IsNullOrWhiteSpace(texto))
            return false;

        string limpio = texto.Trim().Replace(',', '.');

        if (!double.TryParse(limpio, NumberStyles.Any, CultureInfo.InvariantCulture, out double valor))
            return false;

        if (valor < 0 || valor > 10)
            return false;

        valorNormalizado = valor.ToString("0.##", CultureInfo.InvariantCulture);
        return true;
    }

    private void Cerrar_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnPropertyChanged([CallerMemberName] string? nombrePropiedad = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombrePropiedad));
    }
}