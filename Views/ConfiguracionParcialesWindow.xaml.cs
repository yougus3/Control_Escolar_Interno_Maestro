using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    // Configuración existente
    private bool _parcial1Habilitado;
    private bool _parcial2Habilitado;
    private bool _parcial3Habilitado;
    private bool _semestralHabilitado;

    // Nuevos campos para modo Global / Por materia
    private bool _modoGlobalConfiguracion;
    private bool _modoPorMateriaConfiguracion = true;
    private string? _evaluacionGlobalSeleccionada;
    private readonly ObservableCollection<string> _evaluacionesGlobalesDisponibles = new();

    private string? _materiaSeleccionadaConfiguracion;
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

    // ========== Propiedades existentes ==========
    public bool Parcial1Habilitado
    {
        get => _parcial1Habilitado;
        set
        {
            if (_parcial1Habilitado == value) return;
            _parcial1Habilitado = value;
            OnPropertyChanged();
            if (!_cargandoDatos && ModoPorMateriaConfiguracion)
                GuardarConfiguracionMateriaActual();
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
            if (!_cargandoDatos && ModoPorMateriaConfiguracion)
                GuardarConfiguracionMateriaActual();
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
            if (!_cargandoDatos && ModoPorMateriaConfiguracion)
                GuardarConfiguracionMateriaActual();
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
            if (!_cargandoDatos && ModoPorMateriaConfiguracion)
                GuardarConfiguracionMateriaActual();
        }
    }

    // ========== Nuevas propiedades para el modo Global / Por materia ==========
    public bool ModoGlobalConfiguracion
    {
        get => _modoGlobalConfiguracion;
        set
        {
            if (_modoGlobalConfiguracion == value) return;
            _modoGlobalConfiguracion = value;
            _modoPorMateriaConfiguracion = !value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ModoPorMateriaConfiguracion));
            OnPropertyChanged(nameof(GlobalVisible));
            OnPropertyChanged(nameof(PorMateriaVisible));
            OnPropertyChanged(nameof(InfoGuardado));
            if (!_cargandoDatos)
            {
                if (value)
                {
                    CargarEvaluacionesGlobales();
                    GuardarConfiguracionGlobal(); // Esto aplica a todas las materias
                }
                else
                {
                    CargarConfiguracionMateriaSeleccionada();
                }
                // Guardar el modo en el archivo global
                GuardarConfiguracionGlobal();
            }
        }
    }

    public bool ModoPorMateriaConfiguracion
    {
        get => _modoPorMateriaConfiguracion;
        set
        {
            if (_modoPorMateriaConfiguracion == value) return;
            _modoPorMateriaConfiguracion = value;
            _modoGlobalConfiguracion = !value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ModoGlobalConfiguracion));
            OnPropertyChanged(nameof(GlobalVisible));
            OnPropertyChanged(nameof(PorMateriaVisible));
            OnPropertyChanged(nameof(InfoGuardado));
            if (!_cargandoDatos)
            {
                if (value)
                    CargarConfiguracionMateriaSeleccionada();
                else
                {
                    CargarEvaluacionesGlobales();
                    GuardarConfiguracionGlobal();
                }
                GuardarConfiguracionGlobal();
            }
        }
    }

    public bool GlobalVisible => ModoGlobalConfiguracion;
    public bool PorMateriaVisible => ModoPorMateriaConfiguracion;

    public string InfoGuardado => ModoGlobalConfiguracion
        ? "Modo GLOBAL: aplica a todas las materias"
        : "Modo POR MATERIA: guardado por separado";

    public ObservableCollection<string> EvaluacionesGlobalesDisponibles => _evaluacionesGlobalesDisponibles;

    public string? EvaluacionGlobalSeleccionada
    {
        get => _evaluacionGlobalSeleccionada;
        set
        {
            if (_evaluacionGlobalSeleccionada == value) return;
            _evaluacionGlobalSeleccionada = value;
            OnPropertyChanged();
            if (!_cargandoDatos)
                GuardarConfiguracionGlobal();
        }
    }

    // Propiedades existentes para la interfaz
    public string? MateriaSeleccionadaConfiguracion
    {
        get => _materiaSeleccionadaConfiguracion;
        set
        {
            if (_materiaSeleccionadaConfiguracion == value) return;
            _materiaSeleccionadaConfiguracion = value;
            OnPropertyChanged();
            if (!_cargandoDatos && ModoPorMateriaConfiguracion)
                CargarConfiguracionMateriaSeleccionada();
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
                CargarMateriaDirecta();
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
                RefrescarAlumnosParaEvaluacion();
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

    // Nueva propiedad para EXTRA
    private bool _extraHabilitado;
    public bool ExtraHabilitado
    {
        get => _extraHabilitado;
        set
        {
            if (_extraHabilitado == value) return;
            _extraHabilitado = value;
            OnPropertyChanged();
            if (!_cargandoDatos && ModoPorMateriaConfiguracion)
                GuardarConfiguracionMateriaActual();
        }
    }

    // ========== Constructor ==========
    public ConfiguracionParcialesWindow(MainViewModel mainVm)
    {
        InitializeComponent();

        _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
        _configuracionService = new ConfiguracionParcialesService();
        _parserService = new CapParserService();
        _writerService = new CapWriterService();
        _scannerService = new FileScannerService();

        DataContext = this;

        CargarMateriasDisponibles();
        CargarEstadoGlobal();   // Carga el modo guardado y evaluación global
        CargarEvaluacionesGlobales();

        // Si el modo actual es "Por materia", cargar configuración de la primera materia
        if (ModoPorMateriaConfiguracion)
        {
            CargarConfiguracionMateriaSeleccionada();
        }

        // Inicializar la selección de materia
        if (MateriasDisponibles.Any())
        {
            _cargandoDatos = true;
            try
            {
                var primera = MateriasDisponibles.FirstOrDefault();
                MateriaSeleccionadaConfiguracion = primera;
                MateriaSeleccionadaDirecta = primera;
            }
            finally
            {
                _cargandoDatos = false;
            }

            if (ModoPorMateriaConfiguracion)
                CargarConfiguracionMateriaSeleccionada();

            if (!string.IsNullOrWhiteSpace(MateriaSeleccionadaDirecta))
                CargarMateriaDirecta();
        }
    }

    // ========== Métodos nuevos para el modo Global ==========
    private void CargarEvaluacionesGlobales()
    {
        if (_evaluacionesGlobalesDisponibles.Count > 0) return;

        _evaluacionesGlobalesDisponibles.Clear();
        _evaluacionesGlobalesDisponibles.Add("P1");
        _evaluacionesGlobalesDisponibles.Add("P2");
        _evaluacionesGlobalesDisponibles.Add("P3");
        _evaluacionesGlobalesDisponibles.Add("SEM");
        // Añadir EXTRA como opción global (mayúsc/minúsc handled elsewhere con IgnoreCase)
        _evaluacionesGlobalesDisponibles.Add("EXTRA");

        // Si no hay selección, asignar por defecto
        if (string.IsNullOrWhiteSpace(_evaluacionGlobalSeleccionada))
            EvaluacionGlobalSeleccionada = "P1";
    }

    private void CargarEstadoGlobal()
    {
        var rutaGlobal = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configuracion_global.json");
        if (File.Exists(rutaGlobal))
        {
            try
            {
                var json = File.ReadAllText(rutaGlobal);
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (data != null)
                {
                    if (data.TryGetValue("ModoGlobal", out var modoStr) && bool.TryParse(modoStr, out var modo))
                        _modoGlobalConfiguracion = modo;
                    if (data.TryGetValue("EvaluacionGlobal", out var eval))
                        _evaluacionGlobalSeleccionada = eval;
                }
            }
            catch { }
        }

        // Asegurar consistencia
        if (_modoGlobalConfiguracion)
            _modoPorMateriaConfiguracion = false;
        else
            _modoPorMateriaConfiguracion = true;

        // Si estamos en modo Global, aplicar la configuración global a todas las materias
        if (_modoGlobalConfiguracion && !string.IsNullOrWhiteSpace(_evaluacionGlobalSeleccionada))
        {
            AplicarConfiguracionGlobal();
        }

        OnPropertyChanged(nameof(ModoGlobalConfiguracion));
        OnPropertyChanged(nameof(ModoPorMateriaConfiguracion));
    }

    private void AplicarConfiguracionGlobal()
{
    if (!ModoGlobalConfiguracion) return;
    if (string.IsNullOrWhiteSpace(EvaluacionGlobalSeleccionada)) return;

        var configGlobal = new ConfiguracionParciales
    {
        Parcial1Habilitado = string.Equals(EvaluacionGlobalSeleccionada, "P1", StringComparison.OrdinalIgnoreCase),
        Parcial2Habilitado = string.Equals(EvaluacionGlobalSeleccionada, "P2", StringComparison.OrdinalIgnoreCase),
        Parcial3Habilitado = string.Equals(EvaluacionGlobalSeleccionada, "P3", StringComparison.OrdinalIgnoreCase),
        SemestralHabilitado = string.Equals(EvaluacionGlobalSeleccionada, "SEM", StringComparison.OrdinalIgnoreCase),
        ExtraHabilitado = string.Equals(EvaluacionGlobalSeleccionada, "EXTRA", StringComparison.OrdinalIgnoreCase),
        CapturaDirectaHabilitada = true
    };
    var service = new ConfiguracionParcialesService();
    
    // Usar el diccionario _mapaArchivos para obtener la ruta real de cada materia
    foreach (var kvp in _mapaArchivos)
    {
        string claveMateria = ObtenerClaveMateriaDesdeRuta(kvp.Value);
        if (!string.IsNullOrWhiteSpace(claveMateria))
            service.GuardarConfiguracion(claveMateria, configGlobal);
    }
    _mainVm.RecargarConfiguracionYArchivoActual();
}

private void GuardarConfiguracionGlobal()
{
    try
    {
        // Guardar el modo en un archivo aparte (solo para recordar el modo al abrir)
        var data = new Dictionary<string, string>
        {
            ["ModoGlobal"] = ModoGlobalConfiguracion.ToString(),
            ["EvaluacionGlobal"] = EvaluacionGlobalSeleccionada ?? "P1"
        };
        var json = JsonSerializer.Serialize(data);
        var rutaGlobal = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configuracion_global.json");
        File.WriteAllText(rutaGlobal, json);

        // Si estamos en modo Global, guardar la configuración para TODAS las materias
        if (ModoGlobalConfiguracion && !string.IsNullOrWhiteSpace(EvaluacionGlobalSeleccionada))
        {
            var configGlobal = new ConfiguracionParciales
            {
                Parcial1Habilitado = string.Equals(EvaluacionGlobalSeleccionada, "P1", StringComparison.OrdinalIgnoreCase),
                Parcial2Habilitado = string.Equals(EvaluacionGlobalSeleccionada, "P2", StringComparison.OrdinalIgnoreCase),
                Parcial3Habilitado = string.Equals(EvaluacionGlobalSeleccionada, "P3", StringComparison.OrdinalIgnoreCase),
                SemestralHabilitado = string.Equals(EvaluacionGlobalSeleccionada, "SEM", StringComparison.OrdinalIgnoreCase),
                ExtraHabilitado = string.Equals(EvaluacionGlobalSeleccionada, "EXTRA", StringComparison.OrdinalIgnoreCase),
                CapturaDirectaHabilitada = true
            };
            var service = new ConfiguracionParcialesService();
            foreach (var kvp in _mapaArchivos)
            {
                string claveMateria = ObtenerClaveMateriaDesdeRuta(kvp.Value);
                if (!string.IsNullOrWhiteSpace(claveMateria))
                {
                    service.GuardarConfiguracion(claveMateria, configGlobal);
                }
            }
        }

        _mainVm.RecargarConfiguracionYArchivoActual();
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error al guardar configuración global: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

    private void GuardarConfiguracionMateriaActual()
    {
        if (!ModoPorMateriaConfiguracion) return;
        string claveMateria = ObtenerClaveMateriaSeleccionada();
        if (string.IsNullOrWhiteSpace(claveMateria)) return;

        _configuracion.Parcial1Habilitado = Parcial1Habilitado;
        _configuracion.Parcial2Habilitado = Parcial2Habilitado;
        _configuracion.Parcial3Habilitado = Parcial3Habilitado;
        _configuracion.SemestralHabilitado = SemestralHabilitado;
        _configuracion.ExtraHabilitado = ExtraHabilitado;

        _configuracionService.GuardarConfiguracion(claveMateria, _configuracion);
        _mainVm.RecargarConfiguracionYArchivoActual();
    }

    // ========== Métodos existentes modificados ==========
    private void CargarConfiguracionMateriaSeleccionada()
    {
        if (!ModoPorMateriaConfiguracion) return;

        _cargandoDatos = true;
        try
        {
            if (string.IsNullOrWhiteSpace(MateriaSeleccionadaConfiguracion))
            {
                _configuracion = new ConfiguracionParciales
                {
                    CapturaDirectaHabilitada = false,
                    Parcial1Habilitado = true,
                    Parcial2Habilitado = false,
                    Parcial3Habilitado = false,
                    SemestralHabilitado = false,
                    ExtraHabilitado = false
                };
            }
            else
            {
                string claveMateria = ObtenerClaveMateriaSeleccionada();
                _configuracion = string.IsNullOrWhiteSpace(claveMateria)
                    ? new ConfiguracionParciales
                    {
                        CapturaDirectaHabilitada = false,
                        Parcial1Habilitado = true,
                        Parcial2Habilitado = false,
                        Parcial3Habilitado = false,
                        SemestralHabilitado = false,
                        ExtraHabilitado = false
                    }
                    : _configuracionService.ObtenerConfiguracion(claveMateria);
            }

            Parcial1Habilitado = _configuracion.Parcial1Habilitado;
            Parcial2Habilitado = _configuracion.Parcial2Habilitado;
            Parcial3Habilitado = _configuracion.Parcial3Habilitado;
            SemestralHabilitado = _configuracion.SemestralHabilitado;
            ExtraHabilitado = _configuracion.ExtraHabilitado;
        }
        finally
        {
            _cargandoDatos = false;
        }
    }

    private string ObtenerClaveMateriaSeleccionada()
    {
        if (string.IsNullOrWhiteSpace(MateriaSeleccionadaConfiguracion))
            return string.Empty;

        if (_mapaArchivos.TryGetValue(MateriaSeleccionadaConfiguracion, out string? rutaCompleta) &&
            !string.IsNullOrWhiteSpace(rutaCompleta))
        {
            string nombre = Path.GetFileNameWithoutExtension(rutaCompleta);
            return string.IsNullOrWhiteSpace(nombre)
                ? string.Empty
                : nombre.Trim().Replace(' ', '_');
        }

        return string.Empty;
    }

    private string ObtenerClaveMateriaDesdeNombreVisual(string nombreVisual)
    {
        if (string.IsNullOrWhiteSpace(nombreVisual)) return string.Empty;
        string texto = nombreVisual.Trim();
        int indexEspacio = texto.IndexOf(' ');
        if (indexEspacio <= 0)
            return texto.Replace(' ', '_');
        string clave = texto[..indexEspacio].Trim();
        string nombre = texto[(indexEspacio + 1)..].Trim();
        return string.IsNullOrWhiteSpace(nombre) ? clave : $"{clave}_{nombre}";
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

        // En modo Global, solo está habilitada la evaluación seleccionada
        if (ModoGlobalConfiguracion)
            return string.Equals(evaluacion.Trim(), EvaluacionGlobalSeleccionada, StringComparison.OrdinalIgnoreCase);

        // Modo Por materia
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
        if (ModoPorMateriaConfiguracion)
            GuardarConfiguracionMateriaActual();
        else
            GuardarConfiguracionGlobal();
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
        _writerService.GuardarEvaluacion(rutaCompleta, AlumnosDirectos.ToList(), EvaluacionSeleccionadaDirecta, idEval);
        CalificacionActualDirecta = valorNormalizado;
        CalificacionNuevaDirecta = valorNormalizado;
        EstadoDirecto = "Calificación guardada en el CAP.";
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

    private void Calificacion_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        TextBox tb = (TextBox)sender;

        string textoResultante = tb.Text.Insert(tb.CaretIndex, e.Text);

        int indicePunto = textoResultante.IndexOf('.');
        if (indicePunto != -1 && indicePunto != 1)
        {
            e.Handled = true;
            return;
        }

        bool esFormatoValido = Regex.IsMatch(textoResultante, @"^([0-9](\.[0-9]?)?|10?)$");

        e.Handled = !esFormatoValido;
    }
    private string ObtenerClaveMateriaDesdeRuta(string rutaCompleta)
    {
        if (string.IsNullOrWhiteSpace(rutaCompleta)) return string.Empty;
        string nombre = Path.GetFileNameWithoutExtension(rutaCompleta);
        return string.IsNullOrWhiteSpace(nombre) ? string.Empty : nombre.Trim().Replace(' ', '_');
    }
}