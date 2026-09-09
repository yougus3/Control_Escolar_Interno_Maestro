using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.ViewModels;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views;

public partial class ConfiguracionParcialesWindow :
    Window,
    INotifyPropertyChanged
{
    private readonly MainViewModel _mainVm;
    private readonly ConfiguracionParcialesService _configuracionService;
    private readonly CapParserService _parserService;
    private readonly CapWriterService _writerService;
    private readonly FileScannerService _scannerService;

    private string? _evaluacionGlobalSeleccionada;

    private readonly ObservableCollection<string>
        _evaluacionesGlobalesDisponibles =
            new();

    // La ruta/configuración global ahora se gestiona exclusivamente en parciales.db
    // mediante ConfiguracionParcialesService. No usar archivos en AppData.

    private bool _isGlobalComboEnabled = true;

    private bool _cargandoDatos;

    private readonly Dictionary<string, string> _mapaArchivos =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, string>
        _evaluacionIdPorNombreDirecta =
            new(StringComparer.OrdinalIgnoreCase);

    // ============================================================
    // PROFESORES
    // ============================================================

    private readonly ObservableCollection<
        SqliteService.ProfesorConfigurado>
        _profesores =
            new();

    private string _claveProfesorCap =
        string.Empty;

    private SqliteService.ProfesorConfigurado?
        _profesorSeleccionado;

    private bool _usarProfesorDelCap = true;

    private bool _enviarAOtroCorreo;

    private string _correoAlternativo =
        string.Empty;

    private bool _enviandoCorreo;

    private string _estadoCorreo =
        string.Empty;

    private string _archivoPdfGenerado =
        string.Empty;

    // ============================================================
    // CALIFICACIÓN DIRECTA
    // ============================================================

    private string? _materiaSeleccionadaDirecta;

    private string? _evaluacionSeleccionadaDirecta;

    private Alumno? _alumnoSeleccionadoDirecto;

    private string _calificacionNuevaDirecta =
        string.Empty;

    private string _estadoDirecto =
        string.Empty;

    private string _nombreAlumnoDirecto =
        string.Empty;

    private string _matriculaAlumnoDirecto =
        string.Empty;

    private string _grupoAlumnoDirecto =
        string.Empty;

    private string _calificacionActualDirecta =
        string.Empty;

    // ============================================================
    // COLECCIONES
    // ============================================================

    public ObservableCollection<CapFileItem>
        CapFiles { get; } =
        new();

    public ObservableCollection<string>
        MateriasDisponibles { get; } =
        new();

    public ObservableCollection<string>
        EvaluacionesDisponiblesDirecta { get; } =
        new();

    public ObservableCollection<Alumno>
        AlumnosDirectos { get; } =
        new();

    public ObservableCollection<string>
        EvaluacionesGlobalesDisponibles =>
        _evaluacionesGlobalesDisponibles;

    public ObservableCollection<
        SqliteService.ProfesorConfigurado>
        Profesores =>
        _profesores;

    // ============================================================
    // EVENTO
    // ============================================================

    public event PropertyChangedEventHandler?
        PropertyChanged;

    // ============================================================
    // ITEM CAP
    // ============================================================

    public class CapFileItem :
        INotifyPropertyChanged
    {
        private bool _isSelected = true;

        public string DisplayName { get; set; } =
            string.Empty;

        public string FilePath { get; set; } =
            string.Empty;

        public bool IsSelected
        {
            get => _isSelected;

            set
            {
                if (_isSelected == value)
                    return;

                _isSelected =
                    value;

                PropertyChanged?.Invoke(
                    this,
                    new PropertyChangedEventArgs(
                        nameof(IsSelected)));
            }
        }

        public string NombreProfesor { get; set; } =
            string.Empty;

        public string ClaveProfesor { get; set; } =
            string.Empty;

        public bool IsExtra { get; set; }

        public event PropertyChangedEventHandler?
            PropertyChanged;
    }

    // ============================================================
    // PROPIEDADES PROFESOR
    // ============================================================

    public string ClaveProfesorCap
    {
        get => _claveProfesorCap;

        set
        {
            string nuevo =
                value?.Trim() ??
                string.Empty;

            if (string.Equals(
                    _claveProfesorCap,
                    nuevo,
                    StringComparison.Ordinal))
            {
                return;
            }

            _claveProfesorCap =
                nuevo;

            OnPropertyChanged();

            OnPropertyChanged(
                nameof(ProfesorDetectadoTexto));

            OnPropertyChanged(
                nameof(CorreoProfesorSeleccionado));

            OnPropertyChanged(
                nameof(CorreoDestino));

            OnPropertyChanged(
                nameof(HayDestinatarioValido));

            OnPropertyChanged(
                nameof(PuedeEnviarCorreo));
        }
    }

    public SqliteService.ProfesorConfigurado?
        ProfesorSeleccionado
    {
        get => _profesorSeleccionado;

        set
        {
            if (ReferenceEquals(
                    _profesorSeleccionado,
                    value))
            {
                return;
            }

            _profesorSeleccionado =
                value;

            OnPropertyChanged();

            OnPropertyChanged(
                nameof(CorreoProfesorSeleccionado));

            OnPropertyChanged(
                nameof(NombreProfesorSeleccionado));

            OnPropertyChanged(
                nameof(CorreoDestino));

            OnPropertyChanged(
                nameof(HayDestinatarioValido));

            OnPropertyChanged(
                nameof(PuedeEnviarCorreo));

            OnPropertyChanged(
                nameof(ProfesorDetectadoTexto));
        }
    }

    public bool UsarProfesorDelCap
    {
        get => _usarProfesorDelCap;

        set
        {
            if (_usarProfesorDelCap == value)
                return;

            _usarProfesorDelCap =
                value;

            OnPropertyChanged();

            OnPropertyChanged(
                nameof(ProfesorComboHabilitado));

            OnPropertyChanged(
                nameof(CorreoDestino));

            OnPropertyChanged(
                nameof(HayDestinatarioValido));

            OnPropertyChanged(
                nameof(PuedeEnviarCorreo));

            if (value)
            {
                SeleccionarProfesorDetectado();
            }
        }
    }

    public bool ProfesorComboHabilitado =>
        !UsarProfesorDelCap;

    public bool EnviarAOtroCorreo
    {
        get => _enviarAOtroCorreo;

        set
        {
            if (_enviarAOtroCorreo == value)
                return;

            _enviarAOtroCorreo =
                value;

            OnPropertyChanged();

            OnPropertyChanged(
                nameof(CorreoDestino));

            OnPropertyChanged(
                nameof(HayDestinatarioValido));

            OnPropertyChanged(
                nameof(PuedeEnviarCorreo));
        }
    }

    public string CorreoAlternativo
    {
        get => _correoAlternativo;

        set
        {
            if (_correoAlternativo == value)
                return;

            _correoAlternativo =
                value;

            OnPropertyChanged();

            OnPropertyChanged(
                nameof(CorreoDestino));

            OnPropertyChanged(
                nameof(HayDestinatarioValido));

            OnPropertyChanged(
                nameof(PuedeEnviarCorreo));
        }
    }

    public bool EnviandoCorreo
    {
        get => _enviandoCorreo;

        private set
        {
            if (_enviandoCorreo == value)
                return;

            _enviandoCorreo =
                value;

            OnPropertyChanged();

            OnPropertyChanged(
                nameof(PuedeEnviarCorreo));
        }
    }

    public bool PuedeEnviarCorreo =>
        !EnviandoCorreo &&
        HayDestinatarioValido;

    public string EstadoCorreo
    {
        get => _estadoCorreo;

        private set
        {
            if (_estadoCorreo == value)
                return;

            _estadoCorreo =
                value;

            OnPropertyChanged();
        }
    }

    public string ArchivoPdfActual
    {
        get => _archivoPdfGenerado;

        private set
        {
            if (_archivoPdfGenerado == value)
                return;

            _archivoPdfGenerado =
                value;

            OnPropertyChanged();
        }
    }

    public string ProfesorDetectadoTexto
    {
        get
        {
            if (string.IsNullOrWhiteSpace(
                    ClaveProfesorCap))
            {
                return
                    "No se encontró CLAVEPROFESOR en el CAP.";
            }

            if (ProfesorSeleccionado == null)
            {
                return
                    $"CLAVEPROFESOR: {ClaveProfesorCap} | No se encontró ese docente en configuracion.bin.";
            }

            return
                $"Profesor detectado: {ProfesorSeleccionado.NOMBREPROFESOR}";
        }
    }

    public string NombreProfesorSeleccionado =>
        ProfesorSeleccionado?
            .NOMBREPROFESOR
            ?.Trim()
            ?? string.Empty;

    // ============================================================
    // CORREO DEL PROFESOR
    //
    // El dato viene de:
    //
    // configuracion.bin
    //      ↓
    // LiteDbService
    //      ↓
    // ProfesorConfigurado.EMAIL
    //
    // No se lee CONFIG_DOCENTE directamente aquí.
    // ============================================================

    public string CorreoProfesorSeleccionado =>
        ProfesorSeleccionado?
            .EMAIL
            ?.Trim()
            ?? string.Empty;

    public string CorreoDestino
    {
        get
        {
            if (EnviarAOtroCorreo)
            {
                return
                    CorreoAlternativo.Trim();
            }

            return
                CorreoProfesorSeleccionado.Trim();
        }
    }

    public bool HayDestinatarioValido =>
        !string.IsNullOrWhiteSpace(
            CorreoDestino);

    // ============================================================
    // CONFIG GLOBAL
    // ============================================================

    public bool IsGlobalComboEnabled
    {
        get => _isGlobalComboEnabled;

        set
        {
            if (_isGlobalComboEnabled == value)
                return;

            _isGlobalComboEnabled =
                value;

            OnPropertyChanged();

            OnPropertyChanged(
                nameof(MensajeExtraVisible));
        }
    }

    public bool MensajeExtraVisible =>
        !IsGlobalComboEnabled;

    public string? EvaluacionGlobalSeleccionada
    {
        get => _evaluacionGlobalSeleccionada;

        set
        {
            if (_evaluacionGlobalSeleccionada == value)
                return;

            _evaluacionGlobalSeleccionada =
                value;

            OnPropertyChanged();

            if (!_cargandoDatos)
            {
                GuardarConfiguracionGlobal();
            }
        }
    }

    // ============================================================
    // PROPIEDADES DIRECTA
    // ============================================================

    public string? MateriaSeleccionadaDirecta
    {
        get => _materiaSeleccionadaDirecta;

        set
        {
            if (_materiaSeleccionadaDirecta == value)
                return;

            _materiaSeleccionadaDirecta =
                value;

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
            if (_evaluacionSeleccionadaDirecta == value)
                return;

            _evaluacionSeleccionadaDirecta =
                value;

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
            if (_alumnoSeleccionadoDirecto == value)
                return;

            _alumnoSeleccionadoDirecto =
                value;

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

            _calificacionNuevaDirecta =
                value;

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

            _estadoDirecto =
                value;

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

            _nombreAlumnoDirecto =
                value;

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

            _matriculaAlumnoDirecto =
                value;

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

            _grupoAlumnoDirecto =
                value;

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

            _calificacionActualDirecta =
                value;

            OnPropertyChanged();
        }
    }

    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public ConfiguracionParcialesWindow(
        MainViewModel mainVm)
    {
        InitializeComponent();

        _mainVm =
            mainVm
            ?? throw new ArgumentNullException(
                nameof(mainVm));

        _configuracionService =
            new ConfiguracionParcialesService();
        // placeholder: configuración inicial cargada

        _parserService =
            new CapParserService();

        _writerService =
            new CapWriterService();

        _scannerService =
            new FileScannerService();

        DataContext =
            this;

        CargarMateriasDisponibles();

        SuscribirCambiosDeSeleccionCaps();
        // placeholder: subscripción de cambios

        CargarProfesores();

        ActualizarProfesorDesdeCapsSeleccionados();

        VerificarSiCapEsExtra();

        CargarEvaluacionesGlobales();
        // placeholder: evaluaciones globales preparadas

        CargarEstadoGlobal();

        if (MateriasDisponibles.Any())
        {
            _cargandoDatos = true;

            try
            {
                MateriaSeleccionadaDirecta =
                    MateriasDisponibles.FirstOrDefault();
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
    // NORMALIZAR CLAVE PROFESOR
    // ============================================================

    private static string NormalizarClaveProfesor(
        string? clave)
    {
        if (string.IsNullOrWhiteSpace(
                clave))
        {
            return string.Empty;
        }

        var caracteres =
            clave
                .Trim()
                .Where(char.IsLetterOrDigit)
                .ToArray();

        return
            new string(
                caracteres)
            .ToUpperInvariant();
    }

    // ============================================================
    // CARGAR PROFESORES
    // ============================================================

    private void CargarProfesores()
    {
        _profesores.Clear();

        try
        {
            using var lite =
                new SqliteService();

            foreach (var profesor
                     in lite.GetProfesores())
            {
                if (profesor == null)
                    continue;

                profesor.CLAVEPROFESOR =
                    profesor.CLAVEPROFESOR
                        ?.Trim()
                    ?? string.Empty;

                profesor.EMAIL =
                    profesor.EMAIL
                        ?.Trim()
                    ?? string.Empty;

                _profesores.Add(
                    profesor);
            }
        }
        catch (Exception ex)
        {
            EstadoCorreo =
                $"Error al cargar profesores: {ex.Message}";
        }

        OnPropertyChanged(
            nameof(Profesores));
    }

    // ============================================================
    // LEER DATO CAP
    // ============================================================

    private static string LeerDatoCap(
        string filePath,
        string nombreCampo)
    {
        if (string.IsNullOrWhiteSpace(
                filePath) ||
            !File.Exists(
                filePath))
        {
            return string.Empty;
        }

        try
        {
            Encoding.RegisterProvider(
                CodePagesEncodingProvider.Instance);

            var encoding =
                Encoding.GetEncoding(
                    "iso-8859-1");

            foreach (var linea
                     in File.ReadLines(
                         filePath,
                         encoding))
            {
                string lineaLimpia =
                    linea
                        .Trim()
                        .TrimStart('\uFEFF');

                if (!lineaLimpia.Contains('='))
                    continue;

                var partes =
                    lineaLimpia.Split(
                        '=',
                        2);

                if (partes.Length != 2)
                    continue;

                string clave =
                    partes[0].Trim();

                if (!string.Equals(
                        clave,
                        nombreCampo,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return
                    partes[1]
                        .Trim()
                        .Trim('"');
            }
        }
        catch
        {
        }

        return string.Empty;
    }

    // ============================================================
    // CLAVE PROFESOR DESDE CAP
    // ============================================================

    private string ObtenerClaveProfesorDesdeCap(
        string filePath)
    {
        return
            LeerDatoCap(
                filePath,
                "CLAVEPROFESOR");
    }

    // ============================================================
    // ACTUALIZAR PROFESOR DESDE CAPS
    // ============================================================

    private void ActualizarProfesorDesdeCapsSeleccionados()
    {
        var seleccionados =
            CapFiles
                .Where(
                    c => c.IsSelected)
                .ToList();

        var primero =
            seleccionados.FirstOrDefault();

        if (primero == null)
        {
            ClaveProfesorCap =
                string.Empty;

            ProfesorSeleccionado =
                null;

            return;
        }

        string claveCap =
            primero.ClaveProfesor;

        if (string.IsNullOrWhiteSpace(
                claveCap))
        {
            claveCap =
                ObtenerClaveProfesorDesdeCap(
                    primero.FilePath);

            primero.ClaveProfesor =
                claveCap;
        }

        ClaveProfesorCap =
            claveCap;

        SeleccionarProfesorDetectado();

        var claves =
            seleccionados
                .Select(
                    c =>
                    {
                        string clave =
                            c.ClaveProfesor;

                        if (string.IsNullOrWhiteSpace(
                                clave))
                        {
                            clave =
                                ObtenerClaveProfesorDesdeCap(
                                    c.FilePath);
                        }

                        return
                            NormalizarClaveProfesor(
                                clave);
                    })
                .Where(
                    c =>
                        !string.IsNullOrWhiteSpace(c))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        if (claves.Count > 1)
        {
            EstadoCorreo =
                "Los CAP seleccionados pertenecen a diferentes profesores. Se tomó el profesor del primer CAP.";
        }
        else if (ProfesorSeleccionado != null)
        {
            EstadoCorreo =
                $"Profesor detectado: {ProfesorSeleccionado.NOMBREPROFESOR}";
        }
        else
        {
            EstadoCorreo =
                $"CLAVEPROFESOR detectado: {ClaveProfesorCap}, pero no se encontró coincidencia en configuracion.bin.";
        }

        OnPropertyChanged(
            nameof(CorreoProfesorSeleccionado));

        OnPropertyChanged(
            nameof(CorreoDestino));

        OnPropertyChanged(
            nameof(HayDestinatarioValido));

        OnPropertyChanged(
            nameof(PuedeEnviarCorreo));
    }

    // ============================================================
    // BUSCAR PROFESOR EN CONFIGURACION.BIN
    // ============================================================

    private void SeleccionarProfesorDetectado()
    {
        if (string.IsNullOrWhiteSpace(
                ClaveProfesorCap))
        {
            ProfesorSeleccionado =
                null;

            return;
        }

        string claveBuscada =
            NormalizarClaveProfesor(
                ClaveProfesorCap);

        ProfesorSeleccionado =
            _profesores.FirstOrDefault(
                p =>
                    NormalizarClaveProfesor(
                        p.CLAVEPROFESOR)
                    ==
                    claveBuscada);
    }

    // ============================================================
    // CAMBIOS SELECCIÓN CAP
    // ============================================================

    private void SuscribirCambiosDeSeleccionCaps()
    {
        foreach (var cap in CapFiles)
        {
            cap.PropertyChanged -=
                Cap_PropertyChanged;

            cap.PropertyChanged +=
                Cap_PropertyChanged;
        }
    }

    private void Cap_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName ==
            nameof(CapFileItem.IsSelected))
        {
            ActualizarProfesorDesdeCapsSeleccionados();
        }
    }

    // ============================================================
    // EXPORTAR PDF LOCAL
    // ============================================================

    private void ExportarPdf_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            var seleccionados =
                CapFiles
                    .Where(
                        c => c.IsSelected)
                    .ToList();

            if (seleccionados.Count == 0)
            {
                MessageBox.Show(
                    "Selecciona al menos un CAP para exportar.",
                    "Exportar PDF",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            string primerCap =
                seleccionados
                    .First()
                    .FilePath;

            if (!File.Exists(
                    primerCap))
            {
                MessageBox.Show(
                    "El primer CAP seleccionado ya no existe.",
                    "Exportar PDF",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            string ciclo =
                ObtenerCicloEscolarDesdeCap(
                    primerCap);

            if (string.IsNullOrWhiteSpace(
                    ciclo))
            {
                ciclo =
                    "SIN_CICLO";
            }

            string nombreProfesor =
                seleccionados
                    .Select(
                        c =>
                            c.NombreProfesor?.Trim())
                    .FirstOrDefault(
                        n =>
                            !string.IsNullOrWhiteSpace(n))
                    ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    nombreProfesor))
            {
                nombreProfesor =
                    NombreProfesorSeleccionado;
            }

            if (string.IsNullOrWhiteSpace(
                    nombreProfesor))
            {
                nombreProfesor =
                    "PROFESOR";
            }

            string documentos =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments);

            string carpetaDestino =
                Path.Combine(
                    documentos,
                    LimpiarNombreArchivo(
                        ciclo));

            Directory.CreateDirectory(
                carpetaDestino);

            string nombreArchivo =
                $"{LimpiarNombreArchivo(nombreProfesor)}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            string rutaSalida =
                Path.Combine(
                    carpetaDestino,
                    nombreArchivo);

            byte[] pdf =
                GenerarPdfCombinado(
                    seleccionados);

            if (pdf.Length == 0)
            {
                MessageBox.Show(
                    "No se pudo generar el PDF.",
                    "Exportar PDF",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            File.WriteAllBytes(
                rutaSalida,
                pdf);

            ArchivoPdfActual =
                rutaSalida;

            var visor =
                new PdfViewerWindow(
                    rutaSalida)
                {
                    Owner = this
                };

            visor.Show();
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
    // CICLO ESCOLAR
    // ============================================================

    private static string ObtenerCicloEscolarDesdeCap(
        string filePath)
    {
        string ciclo =
            LeerDatoCap(
                filePath,
                "CICLO_DESCRIPCION");

        if (string.IsNullOrWhiteSpace(
                ciclo))
        {
            ciclo =
                LeerDatoCap(
                    filePath,
                    "CICLO_CODIGOCORTO");
        }

        return
            ciclo.Trim();
    }

    // ============================================================
    // PDF COMBINADO
    // ============================================================

    private static byte[] GenerarPdfCombinado(
        List<CapFileItem> seleccionados)
    {
        using var documento =
            new PdfDocument();

        foreach (var cap
                 in seleccionados)
        {
            if (string.IsNullOrWhiteSpace(
                    cap.FilePath))
            {
                continue;
            }

            if (!File.Exists(
                    cap.FilePath))
            {
                continue;
            }

            var servicio =
                new ReporteCalificacionesPdfService(
                    cap.FilePath);

            byte[] bytes =
                servicio.GenerarDiseno();

            if (bytes.Length == 0)
                continue;

            using var entradaStream =
                new MemoryStream(
                    bytes);

            using var entrada =
                PdfReader.Open(
                    entradaStream,
                    PdfDocumentOpenMode.Import);

            for (int i = 0;
                 i < entrada.PageCount;
                 i++)
            {
                documento.AddPage(
                    entrada.Pages[i]);
            }
        }

        if (documento.PageCount == 0)
        {
            return Array.Empty<byte>();
        }

        using var salida =
            new MemoryStream();

        documento.Save(
            salida);

        return salida.ToArray();
    }

    // ============================================================
    // LIMPIAR NOMBRE ARCHIVO
    // ============================================================

    private static string LimpiarNombreArchivo(
        string nombre)
    {
        if (string.IsNullOrWhiteSpace(
                nombre))
        {
            return "SIN_NOMBRE";
        }

        string limpio =
            nombre.Trim();

        foreach (char caracter
                 in Path.GetInvalidFileNameChars())
        {
            limpio =
                limpio.Replace(
                    caracter,
                    '_');
        }

        return limpio;
    }

    // ============================================================
    // ENVIAR CALIFICACIONES
    // ============================================================

    private async void EnviarCalificaciones_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (EnviandoCorreo)
            return;

        var seleccionados =
            CapFiles
                .Where(
                    c => c.IsSelected)
                .ToList();

        if (seleccionados.Count == 0)
        {
            MessageBox.Show(
                "Selecciona al menos un CAP.",
                "Enviar calificaciones",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        ActualizarProfesorDesdeCapsSeleccionados();

        string destinatario =
            CorreoDestino.Trim();

        if (string.IsNullOrWhiteSpace(
                destinatario))
        {
            MessageBox.Show(
                "No existe un correo destinatario válido.",
                "Enviar calificaciones",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (UsarProfesorDelCap &&
            ProfesorSeleccionado == null &&
            !EnviarAOtroCorreo)
        {
            MessageBox.Show(
                $"El CAP contiene CLAVEPROFESOR '{ClaveProfesorCap}', pero no se encontró ese profesor en configuracion.bin.",
                "Profesor no encontrado",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        EnviandoCorreo =
            true;

        string? archivoTemporal =
            null;

        try
        {
            EstadoCorreo =
                "Generando reporte PDF...";

            byte[] pdf =
                GenerarPdfCombinado(
                    seleccionados);

            if (pdf.Length == 0)
            {
                throw new InvalidOperationException(
                    "No se pudo generar ninguna página del PDF.");
            }

            // ====================================================
            // EL NOMBRE DEL ADJUNTO ES EL MISMO QUE EL LOCAL
            // ====================================================

            string primerCap =
                seleccionados
                    .First()
                    .FilePath;

            string ciclo =
                ObtenerCicloEscolarDesdeCap(
                    primerCap);

            if (string.IsNullOrWhiteSpace(
                    ciclo))
            {
                ciclo =
                    "SIN_CICLO";
            }

            string nombreProfesor =
                seleccionados
                    .Select(
                        c =>
                            c.NombreProfesor?.Trim())
                    .FirstOrDefault(
                        n =>
                            !string.IsNullOrWhiteSpace(n))
                    ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    nombreProfesor))
            {
                nombreProfesor =
                    NombreProfesorSeleccionado;
            }

            if (string.IsNullOrWhiteSpace(
                    nombreProfesor))
            {
                nombreProfesor =
                    "PROFESOR";
            }

            string nombreArchivo =
                $"{LimpiarNombreArchivo(nombreProfesor)}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            // ====================================================
            // CARPETA TEMPORAL
            // ====================================================

            string tempFolder =
                Path.Combine(
                    Path.GetTempPath(),
                    "CEIM");

            Directory.CreateDirectory(
                tempFolder);

            archivoTemporal =
                Path.Combine(
                    tempFolder,
                    nombreArchivo);

            File.WriteAllBytes(
                archivoTemporal,
                pdf);

            ArchivoPdfActual =
                archivoTemporal;

            EstadoCorreo =
                $"Enviando a {destinatario}...";

            string asunto =
                $"Reporte de calificaciones - {nombreProfesor}";

            string cuerpo =
                $"Buen día, {nombreProfesor}.\r\n\r\n" +
                "Se adjunta el reporte de calificaciones correspondiente.\r\n\r\n" +
                "Este correo fue enviado automáticamente desde CEIM.";

            using var lite =
                new SqliteService();

            await lite.EnviarCorreoAsync(
                destinatario,
                asunto,
                cuerpo,
                archivoTemporal);

            EstadoCorreo =
                $"Reporte enviado correctamente a {destinatario}.";

            MessageBox.Show(
                $"El reporte fue enviado correctamente a:\n\n{destinatario}",
                "Correo enviado",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            EstadoCorreo =
                "No se pudo enviar el reporte.";

            MessageBox.Show(
                $"No se pudo enviar el reporte:\n\n{ex.Message}",
                "Error al enviar",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            EnviandoCorreo =
                false;

            if (!string.IsNullOrWhiteSpace(
                    archivoTemporal))
            {
                try
                {
                    if (File.Exists(
                            archivoTemporal))
                    {
                        File.Delete(
                            archivoTemporal);
                    }
                }
                catch
                {
                }
            }

            ArchivoPdfActual =
                string.Empty;
        }
    }

    // ============================================================
    // SELECCIONAR TODO
    // ============================================================

    private void SeleccionarTodoCaps_Click(
        object sender,
        RoutedEventArgs e)
    {
        foreach (var cap
                 in CapFiles)
        {
            cap.IsSelected =
                true;
        }

        ActualizarProfesorDesdeCapsSeleccionados();
    }

    // ============================================================
    // DESELECCIONAR TODO
    // ============================================================

    private void DeseleccionarTodoCaps_Click(
        object sender,
        RoutedEventArgs e)
    {
        foreach (var cap
                 in CapFiles)
        {
            cap.IsSelected =
                false;
        }

        ActualizarProfesorDesdeCapsSeleccionados();
    }

    // ============================================================
    // VERIFICAR EXTRA
    // ============================================================

    private void VerificarSiCapEsExtra()
    {
        IsGlobalComboEnabled =
            true;

        if (!string.IsNullOrWhiteSpace(
                _mainVm.ArchivoCompletoActual) &&
            File.Exists(
                _mainVm.ArchivoCompletoActual))
        {
            var resultado =
                _parserService
                    .ProcesarArchivoCompleto(
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
                IsGlobalComboEnabled =
                    false;
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
        try
        {
            var cfg = _configuracionService.ObtenerConfiguracion();

            if (cfg != null)
            {
                if (!string.IsNullOrWhiteSpace(cfg.EvaluacionGlobal) &&
                    !string.Equals(cfg.EvaluacionGlobal, "EXTRA", StringComparison.OrdinalIgnoreCase))
                {
                    _evaluacionGlobalSeleccionada = cfg.EvaluacionGlobal;
                }
                else
                {
                    // Fallback: determinar a partir de los flags
                    if (cfg.Parcial1Habilitado) _evaluacionGlobalSeleccionada = "P1";
                    else if (cfg.Parcial2Habilitado) _evaluacionGlobalSeleccionada = "P2";
                    else if (cfg.Parcial3Habilitado) _evaluacionGlobalSeleccionada = "P3";
                    else if (cfg.SemestralHabilitado) _evaluacionGlobalSeleccionada = "SEM";
                    else _evaluacionGlobalSeleccionada = cfg.EvaluacionGlobal ?? "P1";
                }
            }
            else
            {
                _evaluacionGlobalSeleccionada = "P1";
            }
        }
        catch
        {
            _evaluacionGlobalSeleccionada = "P1";
        }

        AplicarConfiguracionGlobal();

        OnPropertyChanged(nameof(EvaluacionGlobalSeleccionada));
    }

    // ============================================================
    // APLICAR CONFIG GLOBAL
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

                ExtraHabilitado =
                    false,

                CapturaDirectaHabilitada =
                    true
            };

        var service =
            new ConfiguracionParcialesService();

        foreach (var kvp
                 in _mapaArchivos)
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

        _mainVm
            .RecargarConfiguracionYArchivoActual();
    }

    // ============================================================
    // GUARDAR CONFIG GLOBAL
    // ============================================================

    private void GuardarConfiguracionGlobal()
    {
        if (!IsGlobalComboEnabled)
            return;

        try
        {
            var cfg = _configuracionService.ObtenerConfiguracion() ?? new ConfiguracionParciales();

            // Ajustar flags según la evaluación seleccionada
            cfg.Parcial1Habilitado = string.Equals(EvaluacionGlobalSeleccionada, "P1", StringComparison.OrdinalIgnoreCase);
            cfg.Parcial2Habilitado = string.Equals(EvaluacionGlobalSeleccionada, "P2", StringComparison.OrdinalIgnoreCase);
            cfg.Parcial3Habilitado = string.Equals(EvaluacionGlobalSeleccionada, "P3", StringComparison.OrdinalIgnoreCase);
            cfg.SemestralHabilitado = string.Equals(EvaluacionGlobalSeleccionada, "SEM", StringComparison.OrdinalIgnoreCase);
            cfg.PreExtraordinarioHabilitado = string.Equals(EvaluacionGlobalSeleccionada, "PREEXTRAORDINARIO", StringComparison.OrdinalIgnoreCase);
            cfg.ExtraHabilitado = false; // La evaluación global nunca será EXTRA
            cfg.EvaluacionGlobal = EvaluacionGlobalSeleccionada ?? "P1";

            _configuracionService.GuardarConfiguracion("", cfg);

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

        CapFiles.Clear();

        try
        {
            var archivos =
                _scannerService
                    .ObtenerArchivosCap(
                        _mainVm.RutaUsb);

            foreach (var archivo in archivos)
            {
                string nombreVisual =
                    _parserService
                        .ObtenerNombreVisualArchivo(
                            archivo);

                _mapaArchivos[
                    nombreVisual] =
                    archivo;

                MateriasDisponibles.Add(
                    nombreVisual);

                try
                {
                    var info =
                        _parserService
                            .ObtenerInfoParaCombo(
                                archivo);

                    string display =
                        string.IsNullOrWhiteSpace(
                            info.GrupoCap)
                            ? info.NombreBase
                            : $"{info.NombreBase} {info.GrupoCap}";

                    string claveProfesor =
                        ObtenerClaveProfesorDesdeCap(
                            archivo);

                    CapFiles.Add(
                        new CapFileItem
                        {
                            DisplayName =
                                display,

                            FilePath =
                                archivo,

                            IsSelected =
                                true,

                            NombreProfesor =
                                info.NombreProfesor
                                ?? string.Empty,

                            ClaveProfesor =
                                claveProfesor,

                            IsExtra =
                                info.IsExtra
                        });
                }
                catch
                {
                }
            }
        }
        catch
        {
            EstadoDirecto =
                "No se pudieron cargar las materias desde el USB.";
        }
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

            var cap =
                CapFiles.FirstOrDefault(
                    c =>
                        c.DisplayName ==
                        MateriaSeleccionadaDirecta);

            if (cap != null)
            {
                rutaCompleta =
                    cap.FilePath;
            }

            EvaluacionSeleccionadaDirecta =
                null;

            AlumnoSeleccionadoDirecto =
                null;

            return;
        }

        if (!File.Exists(
                rutaCompleta))
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
            _cargandoDatos =
                true;

            var resultado =
                _parserService
                    .ProcesarArchivoCompleto(
                        rutaCompleta);

            foreach (
                var kvp
                in resultado.EvaluacionIdPorNombre)
            {
                _evaluacionIdPorNombreDirecta[
                    kvp.Key] =
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

            foreach (
                var eval
                in resultado.EvaluacionesDisponibles)
            {
                if (soloExtra)
                {
                    EvaluacionesDisponiblesDirecta.Add(
                        eval);

                    continue;
                }

                bool habilitadoPorCap =
                    false;

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
                    if (EvaluacionEstaHabilitada(
                            eval))
                    {
                        EvaluacionesDisponiblesDirecta.Add(
                            eval);
                    }
                }
            }

            foreach (
                var alumno
                in resultado.Alumnos)
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
            _cargandoDatos =
                false;

            RefrescarAlumnosParaEvaluacion();

            RefrescarDatosAlumnoSeleccionado();
        }
    }

    // ============================================================
    // EVALUACIÓN HABILITADA
    // ============================================================

    private bool EvaluacionEstaHabilitada(
        string evaluacion)
    {
        if (string.IsNullOrWhiteSpace(
                evaluacion))
        {
            return false;
        }

        return string.Equals(
            evaluacion.Trim(),
            EvaluacionGlobalSeleccionada,
            StringComparison.OrdinalIgnoreCase);
    }

    // ============================================================
    // REFRESCAR ALUMNOS
    // ============================================================

    private void RefrescarAlumnosParaEvaluacion()
    {
        if (string.IsNullOrWhiteSpace(
                EvaluacionSeleccionadaDirecta))
        {
            foreach (var alumno
                     in AlumnosDirectos)
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

        foreach (var alumno
                 in AlumnosDirectos)
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
            NombreAlumnoDirecto =
                string.Empty;

            MatriculaAlumnoDirecto =
                string.Empty;

            GrupoAlumnoDirecto =
                string.Empty;

            CalificacionActualDirecta =
                string.Empty;

            CalificacionNuevaDirecta =
                string.Empty;

            return;
        }

        NombreAlumnoDirecto =
            AlumnoSeleccionadoDirecto.Nombre;

        MatriculaAlumnoDirecto =
            AlumnoSeleccionadoDirecto.Matricula;

        GrupoAlumnoDirecto =
            AlumnoSeleccionadoDirecto.Grupo;

        CalificacionActualDirecta =
            AlumnoSeleccionadoDirecto
                .ValorSeleccionado;

        CalificacionNuevaDirecta =
            AlumnoSeleccionadoDirecto
                .ValorSeleccionado;
    }

    // ============================================================
    // GUARDAR DIRECTA
    // ============================================================

    private void GuardarDirecta_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (
            string.IsNullOrWhiteSpace(
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

        if (string.Equals(
                EvaluacionSeleccionadaDirecta,
                "PREEXTRAORDINARIO",
                StringComparison.OrdinalIgnoreCase))
        {
            string texto =
                (CalificacionNuevaDirecta ??
                 string.Empty)
                .Trim()
                .ToUpperInvariant();

            if (texto == "NP")
            {
                valorNormalizado =
                    "NP";
            }
            else
            {
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

                if (iv < 0 ||
                    iv > 6)
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

        AlumnoSeleccionadoDirecto
            .ValorSeleccionado =
            valorNormalizado;

        AlumnoSeleccionadoDirecto
            .Calificación[
                EvaluacionSeleccionadaDirecta] =
            valorNormalizado;

        SincronizarConMainVm(
            EvaluacionSeleccionadaDirecta,
            AlumnoSeleccionadoDirecto.Matricula,
            valorNormalizado);

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
    // SINCRONIZAR MAIN VM
    // ============================================================

    private void SincronizarConMainVm(
        string evaluacion,
        string matricula,
        string valor)
    {
        if (!_mapaArchivos.TryGetValue(
                MateriaSeleccionadaDirecta
                    ?? string.Empty,
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
                a =>
                    string.Equals(
                        a.Matricula,
                        matricula,
                        StringComparison.OrdinalIgnoreCase));

        if (alumnoMain == null)
            return;

        alumnoMain.Calificación[
            evaluacion] =
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
        valorNormalizado =
            string.Empty;

        if (string.IsNullOrWhiteSpace(
                texto))
        {
            return false;
        }

        string limpio =
            texto.Trim()
                .Replace(
                    ',',
                    '.');

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
    // CLAVE MATERIA
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

        return string.IsNullOrWhiteSpace(
                nombre)
            ? string.Empty
            : nombre.Trim()
                .Replace(
                    ' ',
                    '_');
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
}
