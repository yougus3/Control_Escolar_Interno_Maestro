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
using Microsoft.Win32;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.ViewModels;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views
{
    public partial class ConfiguracionParcialesWindow : Window, INotifyPropertyChanged
    {
        private readonly MainViewModel _mainVm;
        private readonly ConfiguracionParcialesService _configuracionService;
        private readonly CapParserService _parserService;
        private readonly CapWriterService _writerService;
        private readonly FileScannerService _scannerService;

        // El servicio PDF se crea al momento de exportar,
        // porque necesita el CAP seleccionado.
        private ReporteCalificacionesPdfService? _reportePdfService;

        private readonly Dictionary<string, string> _mapaArchivos =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, string> _evaluacionIdPorNombreDirecta =
            new(StringComparer.OrdinalIgnoreCase);

        private bool _cargandoDatos;

        // ============================================================
        // CONFIGURACIÓN GLOBAL
        // ============================================================

        private string? _evaluacionGlobalSeleccionada;

        private readonly ObservableCollection<string> _evaluacionesGlobalesDisponibles =
            new();

        private bool _isGlobalComboEnabled = true;

        // ============================================================
        // CALIFICACIÓN DIRECTA
        // ============================================================

        private string? _materiaSeleccionadaDirecta;
        private string? _evaluacionSeleccionadaDirecta;
        private Alumno? _alumnoSeleccionadoDirecto;

        private string _calificacionNuevaDirecta = string.Empty;
        private string _estadoDirecto = string.Empty;
        private string _nombreAlumnoDirecto = string.Empty;
        private string _matriculaAlumnoDirecto = string.Empty;
        private string _grupoAlumnoDirecto = string.Empty;
        private string _calificacionActualDirecta = string.Empty;

        // ============================================================
        // COLECCIONES
        // ============================================================

        public ObservableCollection<string> MateriasDisponibles { get; } =
            new();

        public ObservableCollection<string> EvaluacionesDisponiblesDirecta { get; } =
            new();

        public ObservableCollection<Alumno> AlumnosDirectos { get; } =
            new();

        // ============================================================
        // EVENTO
        // ============================================================

        public event PropertyChangedEventHandler? PropertyChanged;

        // ============================================================
        // PROPIEDADES CONFIGURACIÓN GLOBAL
        // ============================================================

        public bool IsGlobalComboEnabled
        {
            get => _isGlobalComboEnabled;

            set
            {
                if (_isGlobalComboEnabled == value)
                    return;

                _isGlobalComboEnabled = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(MensajeExtraVisible));
            }
        }

        public bool MensajeExtraVisible =>
            !IsGlobalComboEnabled;

        public ObservableCollection<string> EvaluacionesGlobalesDisponibles =>
            _evaluacionesGlobalesDisponibles;

        public string? EvaluacionGlobalSeleccionada
        {
            get => _evaluacionGlobalSeleccionada;

            set
            {
                if (_evaluacionGlobalSeleccionada == value)
                    return;

                _evaluacionGlobalSeleccionada = value;

                OnPropertyChanged();

                if (!_cargandoDatos)
                    GuardarConfiguracionGlobal();
            }
        }

        // ============================================================
        // PROPIEDADES CALIFICACIÓN DIRECTA
        // ============================================================

        public string? MateriaSeleccionadaDirecta
        {
            get => _materiaSeleccionadaDirecta;

            set
            {
                if (_materiaSeleccionadaDirecta == value)
                    return;

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
                if (_evaluacionSeleccionadaDirecta == value)
                    return;

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
                if (_alumnoSeleccionadoDirecto == value)
                    return;

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
                if (_calificacionNuevaDirecta == value)
                    return;

                _calificacionNuevaDirecta = value;

                OnPropertyChanged();
            }
        }

        public string EstadoDirecto
        {
            get => _estadoDirecto;

            set
            {
                if (_estadoDirecto == value)
                    return;

                _estadoDirecto = value;

                OnPropertyChanged();
            }
        }

        public string NombreAlumnoDirecto
        {
            get => _nombreAlumnoDirecto;

            set
            {
                if (_nombreAlumnoDirecto == value)
                    return;

                _nombreAlumnoDirecto = value;

                OnPropertyChanged();
            }
        }

        public string MatriculaAlumnoDirecto
        {
            get => _matriculaAlumnoDirecto;

            set
            {
                if (_matriculaAlumnoDirecto == value)
                    return;

                _matriculaAlumnoDirecto = value;

                OnPropertyChanged();
            }
        }

        public string GrupoAlumnoDirecto
        {
            get => _grupoAlumnoDirecto;

            set
            {
                if (_grupoAlumnoDirecto == value)
                    return;

                _grupoAlumnoDirecto = value;

                OnPropertyChanged();
            }
        }

        public string CalificacionActualDirecta
        {
            get => _calificacionActualDirecta;

            set
            {
                if (_calificacionActualDirecta == value)
                    return;

                _calificacionActualDirecta = value;

                OnPropertyChanged();
            }
        }

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public ConfiguracionParcialesWindow(MainViewModel mainVm)
        {
            InitializeComponent();

            _mainVm = mainVm ??
                      throw new ArgumentNullException(nameof(mainVm));

            _configuracionService =
                new ConfiguracionParcialesService();

            _parserService =
                new CapParserService();

            _writerService =
                new CapWriterService();

            _scannerService =
                new FileScannerService();

            // IMPORTANTE:
            // El servicio PDF NO se crea aquí porque todavía
            // no sabemos qué CAP va a exportar el usuario.

            DataContext = this;

            CargarMateriasDisponibles();
            VerificarSiCapEsExtra();
            CargarEvaluacionesGlobales();
            CargarEstadoGlobal();

            if (MateriasDisponibles.Any())
            {
                _cargandoDatos = true;

                try
                {
                    var primera =
                        MateriasDisponibles.FirstOrDefault();

                    MateriaSeleccionadaDirecta = primera;
                }
                finally
                {
                    _cargandoDatos = false;
                }

                if (!string.IsNullOrWhiteSpace(
                    MateriaSeleccionadaDirecta))
                {
                    CargarMateriaDirecta();
                }
            }
        }

        // ============================================================
        // VERIFICAR EXTRA
        // ============================================================

        private void VerificarSiCapEsExtra()
        {
            IsGlobalComboEnabled = true;

            if (!string.IsNullOrWhiteSpace(
                    _mainVm.ArchivoCompletoActual) &&
                File.Exists(_mainVm.ArchivoCompletoActual))
            {
                var resultado =
                    _parserService.ProcesarArchivoCompleto(
                        _mainVm.ArchivoCompletoActual);

                bool soloExtra =
                    resultado.EvaluacionesDisponibles != null &&
                    resultado.EvaluacionesDisponibles.Count == 1 &&
                    string.Equals(
                        resultado.EvaluacionesDisponibles.First(),
                        "EXTRA",
                        StringComparison.OrdinalIgnoreCase);

                if (soloExtra)
                {
                    IsGlobalComboEnabled = false;
                }
            }
        }

        // ============================================================
        // EVALUACIONES GLOBALES
        // ============================================================

        private void CargarEvaluacionesGlobales()
        {
            if (_evaluacionesGlobalesDisponibles.Count > 0)
                return;

            _evaluacionesGlobalesDisponibles.Clear();

            _evaluacionesGlobalesDisponibles.Add("P1");
            _evaluacionesGlobalesDisponibles.Add("P2");
            _evaluacionesGlobalesDisponibles.Add("P3");
            _evaluacionesGlobalesDisponibles.Add("SEM");
            _evaluacionesGlobalesDisponibles.Add("PREEXTRAORDINARIO");
        }

        // ============================================================
        // CARGAR ESTADO GLOBAL
        // ============================================================

        private void CargarEstadoGlobal()
        {
            var rutaGlobal =
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "configuracion_global.json");

            bool existeConfiguracion = false;

            if (File.Exists(rutaGlobal))
            {
                try
                {
                    var json =
                        File.ReadAllText(rutaGlobal);

                    var data =
                        JsonSerializer.Deserialize<
                            Dictionary<string, string>>(json);

                    if (data != null &&
                        data.TryGetValue(
                            "EvaluacionGlobal",
                            out var eval) &&
                        !string.IsNullOrWhiteSpace(eval))
                    {
                        _evaluacionGlobalSeleccionada =
                            eval;

                        existeConfiguracion = true;
                    }
                }
                catch
                {
                    // Configuración nueva.
                }
            }

            if (!existeConfiguracion)
            {
                _evaluacionGlobalSeleccionada = "P1";

                try
                {
                    var data =
                        new Dictionary<string, string>
                        {
                            ["EvaluacionGlobal"] = "P1"
                        };

                    var json =
                        JsonSerializer.Serialize(data);

                    File.WriteAllText(
                        rutaGlobal,
                        json);
                }
                catch
                {
                    // No impedir la apertura.
                }
            }

            if (string.Equals(
                _evaluacionGlobalSeleccionada,
                "EXTRA",
                StringComparison.OrdinalIgnoreCase))
            {
                _evaluacionGlobalSeleccionada = "P1";
            }

            AplicarConfiguracionGlobal();

            OnPropertyChanged(
                nameof(EvaluacionGlobalSeleccionada));
        }

        // ============================================================
        // APLICAR CONFIGURACIÓN GLOBAL
        // ============================================================

        private void AplicarConfiguracionGlobal()
        {
            if (string.IsNullOrWhiteSpace(
                    EvaluacionGlobalSeleccionada))
            {
                return;
            }

            var configGlobal =
                new ConfiguracionParciales
                {
                    Parcial1Habilitado =
                        string.Equals(
                            EvaluacionGlobalSeleccionada,
                            "P1",
                            StringComparison.OrdinalIgnoreCase),

                    Parcial2Habilitado =
                        string.Equals(
                            EvaluacionGlobalSeleccionada,
                            "P2",
                            StringComparison.OrdinalIgnoreCase),

                    Parcial3Habilitado =
                        string.Equals(
                            EvaluacionGlobalSeleccionada,
                            "P3",
                            StringComparison.OrdinalIgnoreCase),

                    SemestralHabilitado =
                        string.Equals(
                            EvaluacionGlobalSeleccionada,
                            "SEM",
                            StringComparison.OrdinalIgnoreCase),

                    PreExtraordinarioHabilitado =
                        string.Equals(
                            EvaluacionGlobalSeleccionada,
                            "PREEXTRAORDINARIO",
                            StringComparison.OrdinalIgnoreCase),

                    ExtraHabilitado = false,

                    CapturaDirectaHabilitada = true
                };

            var service =
                new ConfiguracionParcialesService();

            foreach (var kvp in _mapaArchivos)
            {
                string claveMateria =
                    ObtenerClaveMateriaDesdeRuta(
                        kvp.Value);

                if (!string.IsNullOrWhiteSpace(
                    claveMateria))
                {
                    service.GuardarConfiguracion(
                        claveMateria,
                        configGlobal);
                }
            }

            _mainVm.RecargarConfiguracionYArchivoActual();
        }

        // ============================================================
        // GUARDAR CONFIGURACIÓN GLOBAL
        // ============================================================

        private void GuardarConfiguracionGlobal()
        {
            if (!IsGlobalComboEnabled)
                return;

            try
            {
                var data =
                    new Dictionary<string, string>
                    {
                        ["EvaluacionGlobal"] =
                            EvaluacionGlobalSeleccionada ?? "P1"
                    };

                var json =
                    JsonSerializer.Serialize(data);

                var rutaGlobal =
                    Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "configuracion_global.json");

                File.WriteAllText(
                    rutaGlobal,
                    json);

                AplicarConfiguracionGlobal();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al guardar configuración global: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ============================================================
        // CARGAR MATERIAS
        // ============================================================

        private void CargarMateriasDisponibles()
        {
            MateriasDisponibles.Clear();
            _mapaArchivos.Clear();

            try
            {
                var archivos =
                    _scannerService.ObtenerArchivosCap(
                        _mainVm.RutaUsb);

                foreach (var archivo in archivos)
                {
                    string nombreVisual =
                        _parserService.ObtenerNombreVisualArchivo(
                            archivo);

                    _mapaArchivos[nombreVisual] =
                        archivo;

                    MateriasDisponibles.Add(
                        nombreVisual);
                }
            }
            catch
            {
                EstadoDirecto =
                    "No se pudieron cargar las materias desde el USB.";
            }
        }

        // ============================================================
        // VERIFICAR EVALUACIÓN
        // ============================================================

        private bool EvaluacionEstaHabilitada(
            string evaluacion)
        {
            if (string.IsNullOrWhiteSpace(evaluacion))
                return false;

            return string.Equals(
                evaluacion.Trim(),
                EvaluacionGlobalSeleccionada,
                StringComparison.OrdinalIgnoreCase);
        }

        // ============================================================
        // CARGAR MATERIA DIRECTA
        // ============================================================

        private void CargarMateriaDirecta()
        {
            AlumnosDirectos.Clear();
            EvaluacionesDisponiblesDirecta.Clear();
            _evaluacionIdPorNombreDirecta.Clear();

            if (string.IsNullOrWhiteSpace(
                    MateriaSeleccionadaDirecta))
            {
                EstadoDirecto =
                    "Selecciona una materia.";

                EvaluacionSeleccionadaDirecta =
                    null;

                AlumnoSeleccionadoDirecto =
                    null;

                return;
            }

            if (!_mapaArchivos.TryGetValue(
                    MateriaSeleccionadaDirecta,
                    out string? rutaCompleta))
            {
                EstadoDirecto =
                    "No se encontró el archivo de la materia seleccionada.";

                EvaluacionSeleccionadaDirecta =
                    null;

                AlumnoSeleccionadoDirecto =
                    null;

                return;
            }

            if (!File.Exists(rutaCompleta))
            {
                EstadoDirecto =
                    "El archivo CAP ya no existe.";

                EvaluacionSeleccionadaDirecta =
                    null;

                AlumnoSeleccionadoDirecto =
                    null;

                return;
            }

            try
            {
                _cargandoDatos = true;

                var resultado =
                    _parserService.ProcesarArchivoCompleto(
                        rutaCompleta);

                foreach (var kvp in resultado.EvaluacionIdPorNombre)
                {
                    _evaluacionIdPorNombreDirecta[kvp.Key] =
                        kvp.Value;
                }

                bool soloExtra =
                    resultado.EvaluacionesDisponibles != null &&
                    resultado.EvaluacionesDisponibles.Count == 1 &&
                    string.Equals(
                        resultado.EvaluacionesDisponibles.First(),
                        "EXTRA",
                        StringComparison.OrdinalIgnoreCase);

                var cfgService =
                    new ConfiguracionParcialesService();

                string claveMateria =
                    ObtenerClaveMateriaDesdeRuta(
                        rutaCompleta);

                var cfgPorCap =
                    cfgService.ObtenerConfiguracion(
                        claveMateria);

                bool tieneCfgPorCap =
                    cfgPorCap != null &&
                    (
                        cfgPorCap.Parcial1Habilitado ||
                        cfgPorCap.Parcial2Habilitado ||
                        cfgPorCap.Parcial3Habilitado ||
                        cfgPorCap.SemestralHabilitado ||
                        cfgPorCap.ExtraHabilitado
                    );

                foreach (var eval in
                         resultado.EvaluacionesDisponibles)
                {
                    if (soloExtra)
                    {
                        EvaluacionesDisponiblesDirecta.Add(
                            eval);

                        continue;
                    }

                    bool habilitadoPorCap = false;

                    if (tieneCfgPorCap)
                    {
                        if (string.Equals(
                            eval,
                            "P1",
                            StringComparison.OrdinalIgnoreCase))
                        {
                            habilitadoPorCap =
                                cfgPorCap!.Parcial1Habilitado;
                        }
                        else if (string.Equals(
                            eval,
                            "P2",
                            StringComparison.OrdinalIgnoreCase))
                        {
                            habilitadoPorCap =
                                cfgPorCap!.Parcial2Habilitado;
                        }
                        else if (string.Equals(
                            eval,
                            "P3",
                            StringComparison.OrdinalIgnoreCase))
                        {
                            habilitadoPorCap =
                                cfgPorCap!.Parcial3Habilitado;
                        }
                        else if (string.Equals(
                            eval,
                            "SEM",
                            StringComparison.OrdinalIgnoreCase))
                        {
                            habilitadoPorCap =
                                cfgPorCap!.SemestralHabilitado;
                        }
                        else if (string.Equals(
                            eval,
                            "PREEXTRAORDINARIO",
                            StringComparison.OrdinalIgnoreCase))
                        {
                            habilitadoPorCap =
                                cfgPorCap!.PreExtraordinarioHabilitado;
                        }
                        else if (string.Equals(
                            eval,
                            "EXTRA",
                            StringComparison.OrdinalIgnoreCase))
                        {
                            habilitadoPorCap =
                                cfgPorCap!.ExtraHabilitado;
                        }
                    }

                    if (tieneCfgPorCap)
                    {
                        if (habilitadoPorCap)
                        {
                            EvaluacionesDisponiblesDirecta.Add(
                                eval);
                        }
                    }
                    else
                    {
                        if (EvaluacionEstaHabilitada(eval))
                        {
                            EvaluacionesDisponiblesDirecta.Add(
                                eval);
                        }
                    }
                }

                foreach (var alumno in
                         resultado.Alumnos)
                {
                    AlumnosDirectos.Add(
                        alumno);
                }

                if (EvaluacionesDisponiblesDirecta.Any())
                {
                    EvaluacionSeleccionadaDirecta =
                        EvaluacionesDisponiblesDirecta.First();

                    if (soloExtra)
                    {
                        EstadoDirecto =
                            "Este CAP contiene sólo EVALUACIÓN EXTRA. No puede cambiar parciales aquí.";
                    }
                }
                else
                {
                    EvaluacionSeleccionadaDirecta =
                        null;
                }

                AlumnoSeleccionadoDirecto =
                    AlumnosDirectos.FirstOrDefault();

                EstadoDirecto =
                    $"Materia cargada: {MateriaSeleccionadaDirecta}";
            }
            finally
            {
                _cargandoDatos = false;

                RefrescarAlumnosParaEvaluacion();
                RefrescarDatosAlumnoSeleccionado();
            }
        }

        // ============================================================
        // REFRESCAR ALUMNOS
        // ============================================================

        private void RefrescarAlumnosParaEvaluacion()
        {
            if (string.IsNullOrWhiteSpace(
                    EvaluacionSeleccionadaDirecta))
            {
                foreach (var alumno in AlumnosDirectos)
                {
                    alumno.ActualizarSeleccion(
                        string.Empty);
                }

                CalificacionActualDirecta =
                    string.Empty;

                CalificacionNuevaDirecta =
                    string.Empty;

                return;
            }

            foreach (var alumno in AlumnosDirectos)
            {
                alumno.ActualizarSeleccion(
                    EvaluacionSeleccionadaDirecta);
            }

            RefrescarDatosAlumnoSeleccionado();
        }

        // ============================================================
        // REFRESCAR DATOS ALUMNO
        // ============================================================

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

            NombreAlumnoDirecto =
                AlumnoSeleccionadoDirecto.Nombre;

            MatriculaAlumnoDirecto =
                AlumnoSeleccionadoDirecto.Matricula;

            GrupoAlumnoDirecto =
                AlumnoSeleccionadoDirecto.Grupo;

            CalificacionActualDirecta =
                AlumnoSeleccionadoDirecto.ValorSeleccionado;

            CalificacionNuevaDirecta =
                AlumnoSeleccionadoDirecto.ValorSeleccionado;
        }

        // ============================================================
        // GUARDAR CALIFICACIÓN DIRECTA
        // ============================================================

        private void GuardarDirecta_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(
                    MateriaSeleccionadaDirecta) ||
                string.IsNullOrWhiteSpace(
                    EvaluacionSeleccionadaDirecta) ||
                AlumnoSeleccionadoDirecto == null)
            {
                MessageBox.Show(
                    "Asegúrate de seleccionar materia, evaluación y alumno.",
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (!_mapaArchivos.TryGetValue(
                    MateriaSeleccionadaDirecta,
                    out string? rutaCompleta) ||
                !_evaluacionIdPorNombreDirecta.TryGetValue(
                    EvaluacionSeleccionadaDirecta,
                    out string? idEval))
            {
                MessageBox.Show(
                    "Error al localizar datos del CAP para guardar.",
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            string valorNormalizado;

            // ========================================================
            // PREEXTRAORDINARIO
            // ========================================================

            if (string.Equals(
                    EvaluacionSeleccionadaDirecta,
                    "PREEXTRAORDINARIO",
                    StringComparison.OrdinalIgnoreCase))
            {
                string texto =
                    (CalificacionNuevaDirecta ?? string.Empty)
                    .Trim()
                    .ToUpperInvariant();

                // NP
                if (texto == "NP")
                {
                    valorNormalizado = "NP";
                }
                else
                {
                    // La variable queda declarada e inicializada
                    // correctamente para evitar CS0165.
                    if (!int.TryParse(
                            texto,
                            out int iv))
                    {
                        MessageBox.Show(
                            "Para PREEXTRAORDINARIO solo se permiten enteros de 0 a 6 o NP.",
                            "Aviso",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return;
                    }

                    if (iv < 0 || iv > 6)
                    {
                        MessageBox.Show(
                            "Para PREEXTRAORDINARIO solo se permiten enteros de 0 a 6 o NP.",
                            "Aviso",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return;
                    }

                    valorNormalizado =
                        iv.ToString(
                            CultureInfo.InvariantCulture);
                }
            }
            else
            {
                if (!TryNormalizarCalificacion(
                        CalificacionNuevaDirecta,
                        out string valorNormalizadoLocal))
                {
                    MessageBox.Show(
                        "La calificación debe ser un número entre 0 y 10.",
                        "Aviso",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                valorNormalizado =
                    valorNormalizadoLocal;
            }

            // ========================================================
            // ACTUALIZAR ALUMNO
            // ========================================================

            AlumnoSeleccionadoDirecto.ValorSeleccionado =
                valorNormalizado;

            AlumnoSeleccionadoDirecto.Calificación[
                EvaluacionSeleccionadaDirecta] =
                valorNormalizado;

            // ========================================================
            // SINCRONIZAR MAIN VM
            // ========================================================

            SincronizarConMainVm(
                EvaluacionSeleccionadaDirecta,
                AlumnoSeleccionadoDirecto.Matricula,
                valorNormalizado);

            // ========================================================
            // GUARDAR EN CAP
            // ========================================================

            _writerService.GuardarEvaluacion(
                rutaCompleta,
                AlumnosDirectos.ToList(),
                EvaluacionSeleccionadaDirecta,
                idEval);

            CalificacionActualDirecta =
                valorNormalizado;

            CalificacionNuevaDirecta =
                valorNormalizado;

            EstadoDirecto =
                "Calificación guardada en el CAP.";
        }

        // ============================================================
        // SINCRONIZAR CON MAIN VM
        // ============================================================

        private void SincronizarConMainVm(
            string evaluacion,
            string matricula,
            string valor)
        {
            if (!_mapaArchivos.TryGetValue(
                    MateriaSeleccionadaDirecta ?? string.Empty,
                    out var ruta))
            {
                return;
            }

            if (!string.Equals(
                    _mainVm.ArchivoCompletoActual,
                    ruta,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var alumnoMain =
                _mainVm.Alumnos.FirstOrDefault(
                    a => string.Equals(
                        a.Matricula,
                        matricula,
                        StringComparison.OrdinalIgnoreCase));

            if (alumnoMain == null)
                return;

            alumnoMain.Calificación[evaluacion] =
                valor;

            if (string.Equals(
                    _mainVm.EvaluacionSeleccionada,
                    evaluacion,
                    StringComparison.OrdinalIgnoreCase))
            {
                alumnoMain.ActualizarSeleccion(
                    evaluacion);
            }
        }

        // ============================================================
        // NORMALIZAR CALIFICACIÓN
        // ============================================================

        private static bool TryNormalizarCalificacion(
            string texto,
            out string valorNormalizado)
        {
            valorNormalizado = string.Empty;

            if (string.IsNullOrWhiteSpace(texto))
                return false;

            string limpio =
                texto.Trim()
                    .Replace(',', '.');

            if (!double.TryParse(
                    limpio,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double valor) ||
                valor < 0 ||
                valor > 10)
            {
                return false;
            }

            valorNormalizado =
                valor.ToString(
                    "0.##",
                    CultureInfo.InvariantCulture);

            return true;
        }

        // ============================================================
        // EXPORTAR PDF
        // ============================================================

        private void ExportarPdf_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                // ----------------------------------------------------
                // OBTENER CAP DE LA MATERIA SELECCIONADA
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(
                        MateriaSeleccionadaDirecta))
                {
                    MessageBox.Show(
                        "Selecciona una materia antes de exportar el PDF.",
                        "Aviso",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                if (!_mapaArchivos.TryGetValue(
                        MateriaSeleccionadaDirecta,
                        out string? rutaCap) ||
                    string.IsNullOrWhiteSpace(
                        rutaCap))
                {
                    MessageBox.Show(
                        "No se encontró el archivo CAP de la materia seleccionada.",
                        "Aviso",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                if (!File.Exists(rutaCap))
                {
                    MessageBox.Show(
                        "El archivo CAP seleccionado ya no existe.",
                        "Aviso",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                // ----------------------------------------------------
                // CREAR SERVICIO CON CAP REAL
                // ----------------------------------------------------

                _reportePdfService =
                    new ReporteCalificacionesPdfService(
                        rutaCap);

                // ----------------------------------------------------
                // GENERAR PDF
                // ----------------------------------------------------

                byte[] pdfBytes =
                    _reportePdfService.GenerarDiseno();

                // ----------------------------------------------------
                // GUARDAR ARCHIVO
                // ----------------------------------------------------

                var dialog =
                    new SaveFileDialog
                    {
                        Title =
                            "Guardar reporte PDF",

                        Filter =
                            "Archivo PDF (*.pdf)|*.pdf",

                        FileName =
                            $"{Path.GetFileNameWithoutExtension(rutaCap)}_Reporte.pdf",

                        DefaultExt =
                            ".pdf",

                        AddExtension =
                            true
                    };

                if (dialog.ShowDialog() != true)
                    return;

                File.WriteAllBytes(
                    dialog.FileName,
                    pdfBytes);

                MessageBox.Show(
                    "El PDF se exportó correctamente.",
                    "Exportación completada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudo exportar el PDF:\n\n{ex.Message}",
                    "Error de exportación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ============================================================
        // CERRAR
        // ============================================================

        private void Cerrar_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        // ============================================================
        // PROPERTY CHANGED
        // ============================================================

        private void OnPropertyChanged(
            [CallerMemberName]
            string? nombrePropiedad = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(
                    nombrePropiedad));
        }

        // ============================================================
        // PREVIEW TEXT INPUT
        // ============================================================

        private void Calificacion_PreviewTextInput(
            object sender,
            TextCompositionEventArgs e)
        {
            TextBox tb =
                (TextBox)sender;

            string textoResultante =
                tb.Text.Insert(
                    tb.CaretIndex,
                    e.Text);

            int indicePunto =
                textoResultante.IndexOf('.');

            if (indicePunto != -1 &&
                indicePunto != 1)
            {
                e.Handled = true;
                return;
            }

            e.Handled =
                !Regex.IsMatch(
                    textoResultante,
                    @"^([0-9](\.[0-9]?)?|10?)$");
        }

        // ============================================================
        // OBTENER CLAVE DE MATERIA
        // ============================================================

        private string ObtenerClaveMateriaDesdeRuta(
            string rutaCompleta)
        {
            if (string.IsNullOrWhiteSpace(
                    rutaCompleta))
            {
                return string.Empty;
            }

            string nombre =
                Path.GetFileNameWithoutExtension(
                    rutaCompleta);

            return string.IsNullOrWhiteSpace(nombre)
                ? string.Empty
                : nombre.Trim()
                    .Replace(' ', '_');
        }
    }
}