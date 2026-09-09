using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.ViewModels;

public class EvaluacionItem
{
    public string Id { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
}

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

    private bool _isUpdatingProgrammatically = false;

    // ============================================================
    // PROFESORES / CORREO
    // ============================================================

    private readonly List<SqliteService.ProfesorConfigurado>
        _profesoresCache = new();

    [ObservableProperty]
    private string _claveProfesorActual = string.Empty;

    [ObservableProperty]
    private SqliteService.ProfesorConfigurado?
        _profesorSeleccionado;

    [ObservableProperty]
    private bool _usarProfesorDelCap = true;

    [ObservableProperty]
    private bool _enviarAOtroCorreo = false;

    [ObservableProperty]
    private string _correoAlternativo = string.Empty;

    [ObservableProperty]
    private bool _enviandoCorreo = false;

    [ObservableProperty]
    private string _estadoCorreo = string.Empty;

    public ObservableCollection<SqliteService.ProfesorConfigurado>
        Profesores { get; } = new();

    public bool ProfesorComboHabilitado =>
        !UsarProfesorDelCap;

    public string CorreoProfesorSeleccionado =>
        ProfesorSeleccionado?.EMAIL?.Trim()
        ?? string.Empty;

    public string NombreProfesorSeleccionado =>
        ProfesorSeleccionado?.NOMBREPROFESOR?.Trim()
        ?? string.Empty;

    public string CorreoDestino =>
        EnviarAOtroCorreo
            ? CorreoAlternativo?.Trim()
              ?? string.Empty
            : CorreoProfesorSeleccionado;

    public bool HayDestinatarioValido =>
        !string.IsNullOrWhiteSpace(
            CorreoDestino);

    // ============================================================
    // PROPIEDADES EXISTENTES
    // ============================================================

    public bool IsUpdatingProgrammatically =>
        _isUpdatingProgrammatically;

    public string? ArchivoCompletoActual =>
        _archivoCompletoActual;

    public ParcialesViewModel ParcialesVm { get; }

    [ObservableProperty]
    private string _rutaUsb = string.Empty;

    [ObservableProperty]
    private bool _rutaUsbEditable = false;

    [ObservableProperty]
    private string _rutaUsbLabel = string.Empty;

    [ObservableProperty]
    private string _currentView = "List";

    [ObservableProperty]
    private bool _tieneCambios;

    [ObservableProperty]
    private string _nombreMateriaExtra = string.Empty;

    [ObservableProperty]
    private string _nombreProfesorExtra = string.Empty;

    [ObservableProperty]
    private string _textoEvaluadosExtra = string.Empty;

    [ObservableProperty]
    private bool _faltanPorEvaluarExtra = false;

    public List<AlumnoFaltante> ListaNoEvaluadosExtra { get; private set; } =
        new();

    private string? _archivoSeleccionado;

    public string? ArchivoSeleccionado
    {
        get => _archivoSeleccionado;

        set
        {
            if (_archivoSeleccionado == value)
                return;

            if (!_isUpdatingProgrammatically &&
                !ManejarCambiosPendientes())
            {
                OnPropertyChanged(
                    nameof(ArchivoSeleccionado));

                return;
            }

            SetProperty(
                ref _archivoSeleccionado,
                value);

            CargarArchivoSeleccionado(value);
        }
    }

    private string? _evaluacionSeleccionada;

    public string? EvaluacionSeleccionada
    {
        get => _evaluacionSeleccionada;

        set
        {
            if (_evaluacionSeleccionada == value)
                return;

            if (!_isUpdatingProgrammatically &&
                !ManejarCambiosPendientes())
            {
                OnPropertyChanged(
                    nameof(EvaluacionSeleccionada));

                return;
            }

            SetProperty(
                ref _evaluacionSeleccionada,
                value);

            CambiarEvaluacion(value);
        }
    }

    public ObservableCollection<EvaluacionItem>
        EvaluacionesDisponibles { get; } = new();

    public ObservableCollection<string>
        ArchivosDisponibles { get; } = new();

    public ObservableCollection<Alumno>
        Alumnos { get; } = new();

    // ============================================================
    // ALUMNOS CON DERECHO A EXTRA
    // ============================================================

    public ObservableCollection<Alumno>
        AlumnosConDerechoExtra { get; } = new();

    private readonly List<Alumno> _subscribedAlumnos =
        new();

    // Proxy para mostrar el texto de evaluados en la barra superior
    public string TextoEvaluados =>
        ParcialesVm?.TextoEvaluados ?? string.Empty;

    // Proxy para visibilidad del icono de advertencia
    public bool ParcialesFaltan =>
        ParcialesVm?.FaltanPorEvaluar ?? false;

    public bool EsExtraSeleccionado =>
        string.Equals(
            EvaluacionSeleccionada,
            "EXTRA",
            StringComparison.OrdinalIgnoreCase);

    public bool EsPreExtraordinarioSeleccionado =>
        string.Equals(
            EvaluacionSeleccionada,
            "PREEXTRAORDINARIO",
            StringComparison.OrdinalIgnoreCase);

    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public MainViewModel()
    {
        _parserService =
            new CapParserService();

        _writerService =
            new CapWriterService();

        _scannerService =
            new FileScannerService();

        _configuracionService =
            new ConfiguracionParcialesService();

        _configuracionActual =
            _configuracionService
                .ObtenerConfiguracion();

        // ========================================================
        // CARGAR PROFESORES DESDE BIN
        // ========================================================

        CargarProfesores();

        ParcialesVm =
            new ParcialesViewModel(this);

        // Suscribirse a cambios en ParcialesVm
        if (ParcialesVm != null)
        {
            ParcialesVm.PropertyChanged +=
                ParcialesVm_PropertyChangedHandler;
        }

        try
        {
            var carpetaData =
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory
                    ?? string.Empty,
                    "data");

            if (!Directory.Exists(
                    carpetaData))
            {
                Directory.CreateDirectory(
                    carpetaData);
            }

            RutaUsb =
                carpetaData;

            RutaUsbEditable =
                false;

            RutaUsbLabel =
                "Raiz";
        }
        catch
        {
            RutaUsb =
                string.Empty;

            RutaUsbLabel =
                string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(
                RutaUsb))
        {
            ProcesarDirectorioSeleccionado();
        }
    }

    private void ParcialesVm_PropertyChangedHandler(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName ==
            nameof(ParcialesViewModel.TextoEvaluados))
        {
            OnPropertyChanged(
                nameof(TextoEvaluados));
        }
        else if (
            e.PropertyName ==
            nameof(ParcialesViewModel.FaltanPorEvaluar))
        {
            OnPropertyChanged(
                nameof(ParcialesFaltan));
        }
    }

    // ============================================================
    // CARGAR PROFESORES
    // ============================================================

    private void CargarProfesores()
    {
        Profesores.Clear();
        _profesoresCache.Clear();

        try
        {
            using var lite =
                new SqliteService();

            var profesores =
                lite.GetProfesores();

            foreach (var profesor in profesores)
            {
                _profesoresCache.Add(
                    profesor);

                Profesores.Add(
                    profesor);
            }
        }
        catch
        {
        }

        OnPropertyChanged(
            nameof(ProfesorComboHabilitado));

        OnPropertyChanged(
            nameof(CorreoProfesorSeleccionado));

        OnPropertyChanged(
            nameof(NombreProfesorSeleccionado));

        OnPropertyChanged(
            nameof(CorreoDestino));

        OnPropertyChanged(
            nameof(HayDestinatarioValido));
    }

    // ============================================================
    // CAMBIO CHECKBOX:
    // USAR PROFESOR DEL CAP
    // ============================================================

    partial void OnUsarProfesorDelCapChanged(
        bool value)
    {
        OnPropertyChanged(
            nameof(ProfesorComboHabilitado));

        OnPropertyChanged(
            nameof(CorreoProfesorSeleccionado));

        OnPropertyChanged(
            nameof(CorreoDestino));

        OnPropertyChanged(
            nameof(HayDestinatarioValido));

        if (value)
        {
            SeleccionarProfesorPorClave(
                ClaveProfesorActual);
        }
    }

    // ============================================================
    // CAMBIO PROFESOR COMBOBOX
    // ============================================================

    partial void OnProfesorSeleccionadoChanged(
        SqliteService.ProfesorConfigurado? value)
    {
        OnPropertyChanged(
            nameof(CorreoProfesorSeleccionado));

        OnPropertyChanged(
            nameof(NombreProfesorSeleccionado));

        OnPropertyChanged(
            nameof(CorreoDestino));

        OnPropertyChanged(
            nameof(HayDestinatarioValido));
    }

    // ============================================================
    // CAMBIO CHECK:
    // ENVIAR A OTRO CORREO
    // ============================================================

    partial void OnEnviarAOtroCorreoChanged(
        bool value)
    {
        OnPropertyChanged(
            nameof(CorreoDestino));

        OnPropertyChanged(
            nameof(HayDestinatarioValido));
    }

    partial void OnCorreoAlternativoChanged(
        string value)
    {
        OnPropertyChanged(
            nameof(CorreoDestino));

        OnPropertyChanged(
            nameof(HayDestinatarioValido));
    }

    // ============================================================
    // SELECCIONAR PROFESOR POR CLAVE
    // ============================================================

    private void SeleccionarProfesorPorClave(
        string claveProfesor)
    {
        ProfesorSeleccionado =
            null;

        if (string.IsNullOrWhiteSpace(
                claveProfesor))
        {
            return;
        }

        var profesor =
            _profesoresCache.FirstOrDefault(
                p =>
                    string.Equals(
                        p.CLAVEPROFESOR?.Trim(),
                        claveProfesor.Trim(),
                        StringComparison.OrdinalIgnoreCase));

        if (profesor != null)
        {
            ProfesorSeleccionado =
                profesor;
        }

        OnPropertyChanged(
            nameof(CorreoProfesorSeleccionado));

        OnPropertyChanged(
            nameof(CorreoDestino));

        OnPropertyChanged(
            nameof(HayDestinatarioValido));
    }

    // ============================================================
    // DETECTAR CLAVEPROFESOR EN EL CAP
    // ============================================================

    private string ObtenerClaveProfesorDesdeCap(
        string filePath)
    {
        if (!File.Exists(filePath))
            return string.Empty;

        try
        {
            Encoding.RegisterProvider(
                CodePagesEncodingProvider.Instance);

            var encodingCap =
                Encoding.GetEncoding(
                    "iso-8859-1");

            var lineas =
                File.ReadAllLines(
                    filePath,
                    encodingCap);

            foreach (var linea in lineas)
            {
                string l =
                    linea.Trim();

                if (!l.Contains('='))
                    continue;

                var partes =
                    l.Split('=', 2);

                if (partes.Length != 2)
                    continue;

                string key =
                    partes[0].Trim();

                if (key.Equals(
                        "CLAVEPROFESOR",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return partes[1].Trim();
                }
            }
        }
        catch
        {
        }

        return string.Empty;
    }

    // ============================================================
    // CARGAR PROFESOR DEL CAP
    // ============================================================

    private void CargarProfesorDelCap(
        string filePath)
    {
        ClaveProfesorActual =
            ObtenerClaveProfesorDesdeCap(
                filePath);

        SeleccionarProfesorPorClave(
            ClaveProfesorActual);

        EstadoCorreo =
            string.IsNullOrWhiteSpace(
                ClaveProfesorActual)
                ? "El CAP no contiene CLAVEPROFESOR."
                : ProfesorSeleccionado != null
                    ? $"Profesor detectado: {ProfesorSeleccionado.NOMBREPROFESOR}"
                    : $"CLAVEPROFESOR detectada ({ClaveProfesorActual}), pero no existe en configuracion.bin.";

        OnPropertyChanged(
            nameof(CorreoDestino));

        OnPropertyChanged(
            nameof(HayDestinatarioValido));
    }

    // ============================================================
    // ENVIAR REPORTE
    // ============================================================

    [RelayCommand]
    private async Task EnviarReporteAsync(
        string? archivoPdf)
    {
        if (EnviandoCorreo)
            return;

        if (string.IsNullOrWhiteSpace(
                archivoPdf))
        {
            MessageBox.Show(
                "Primero debe generarse el reporte de calificaciones.",
                "Enviar reporte",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (!File.Exists(
                archivoPdf))
        {
            MessageBox.Show(
                "No se encontró el archivo PDF del reporte.",
                "Enviar reporte",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        string destinatario =
            CorreoDestino;

        if (string.IsNullOrWhiteSpace(
                destinatario))
        {
            MessageBox.Show(
                "No hay un correo de destino válido.",
                "Enviar reporte",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        EnviandoCorreo =
            true;

        EstadoCorreo =
            "Enviando reporte...";

        try
        {
            string materia =
                ArchivoSeleccionado
                ?? "Calificaciones";

            string asunto =
                $"Reporte de calificaciones - {materia}";

            string profesor =
                !string.IsNullOrWhiteSpace(
                    NombreProfesorSeleccionado)
                    ? NombreProfesorSeleccionado
                    : "Docente";

            string cuerpo =
                $"Buen día, {profesor}.\r\n\r\n" +
                "Se adjunta el reporte de calificaciones correspondiente.\r\n\r\n" +
                "Este mensaje fue enviado automáticamente desde CEIM.";

            using var lite =
                new SqliteService();

            await lite.EnviarCorreoAsync(
                destinatario,
                asunto,
                cuerpo,
                archivoPdf);

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
                "No se pudo enviar el correo.";

            MessageBox.Show(
                $"No se pudo enviar el reporte.\n\n{ex.Message}",
                "Error al enviar",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            EnviandoCorreo =
                false;
        }
    }

    // ============================================================
    // CONTADOR EXTRA
    // ============================================================

    public void ActualizarConteoEvaluadosExtra()
    {
        if (!string.Equals(
                CurrentView,
                "Extra",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        int total =
            AlumnosConDerechoExtra.Count;

        int evaluados =
            0;

        var lista =
            new List<AlumnoFaltante>();

        foreach (var alumno
                 in AlumnosConDerechoExtra)
        {
            if (!string.IsNullOrWhiteSpace(
                    alumno.Calificación["EXTRA"]))
            {
                evaluados++;
            }
            else
            {
                lista.Add(
                    new AlumnoFaltante
                    {
                        Materia =
                            NombreMateriaExtra,

                        Grupo =
                            alumno.Grupo,

                        Matricula =
                            alumno.Matricula,

                        Nombre =
                            alumno.Nombre,

                        Razon =
                            "Falta calificación de extraordinario"
                    });
            }
        }

        TextoEvaluadosExtra =
            $"{evaluados} de {total}";

        FaltanPorEvaluarExtra =
            evaluados < total;

        ListaNoEvaluadosExtra =
            lista;
    }

    // ============================================================
    // CLAVE DE MATERIA ACTUAL
    // ============================================================

    private string ObtenerClaveMateriaActual()
    {
        if (!string.IsNullOrWhiteSpace(
                _archivoCompletoActual))
        {
            string clave =
                Path.GetFileNameWithoutExtension(
                    _archivoCompletoActual);

            return string.IsNullOrWhiteSpace(clave)
                ? string.Empty
                : clave.Trim().Replace(
                    ' ',
                    '_');
        }

        return string.Empty;
    }

    // ============================================================
    // OBTENER CLAVEASIGNATURA DIRECTAMENTE DEL CAP
    // ============================================================

    private static string ObtenerClaveAsignaturaDesdeCap(
        string? rutaCompleta)
    {
        if (string.IsNullOrWhiteSpace(
                rutaCompleta) ||
            !File.Exists(rutaCompleta))
        {
            return string.Empty;
        }

        try
        {
            Encoding.RegisterProvider(
                CodePagesEncodingProvider.Instance);

            var encodingCap =
                Encoding.GetEncoding(
                    "iso-8859-1");

            foreach (var linea in
                     File.ReadLines(
                         rutaCompleta,
                         encodingCap))
            {
                string texto =
                    linea.Trim();

                if (!texto.Contains('='))
                    continue;

                var partes =
                    texto.Split('=', 2);

                if (partes.Length != 2)
                    continue;

                string clave =
                    partes[0].Trim();

                if (clave.Equals(
                        "CLAVEASIGNATURA",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return partes[1].Trim();
                }
            }
        }
        catch
        {
        }

        return string.Empty;
    }

    // ============================================================
    // CARGAR ALUMNOS AUTORIZADOS PARA EXTRA
    // ============================================================

    private void CargarAlumnosConDerechoExtra()
    {
        AlumnosConDerechoExtra.Clear();

        string claveAsignatura =
            ObtenerClaveAsignaturaDesdeCap(
                _archivoCompletoActual);

        if (string.IsNullOrWhiteSpace(
                claveAsignatura))
        {
            System.Diagnostics.Debug.WriteLine(
                "[EXTRA] El CAP no contiene CLAVEASIGNATURA.");

            return;
        }

        try
        {
            using var lite =
                new SqliteService();

            var autorizados =
                lite.ObtenerMatriculasConDerechoExtra(
                    claveAsignatura);

            var autorizadosNormalizados =
                new HashSet<string>(
                    autorizados
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(x))
                        .Select(x =>
                            x.Trim()),
                    StringComparer.OrdinalIgnoreCase);

            foreach (var alumno in Alumnos)
            {
                if (alumno == null)
                    continue;

                string matricula =
                    alumno.Matricula?.Trim()
                    ?? string.Empty;

                if (string.IsNullOrWhiteSpace(
                        matricula))
                {
                    continue;
                }

                if (autorizadosNormalizados.Contains(
                        matricula))
                {
                    AlumnosConDerechoExtra.Add(
                        alumno);
                }
            }

            System.Diagnostics.Debug.WriteLine(
                $"[EXTRA] CLAVEASIGNATURA CAP = {claveAsignatura}");

            System.Diagnostics.Debug.WriteLine(
                $"[EXTRA] Matrículas autorizadas = {autorizadosNormalizados.Count}");

            System.Diagnostics.Debug.WriteLine(
                $"[EXTRA] Alumnos del CAP = {Alumnos.Count}");

            System.Diagnostics.Debug.WriteLine(
                $"[EXTRA] Alumnos mostrados = {AlumnosConDerechoExtra.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[EXTRA] Error: {ex}");

            AlumnosConDerechoExtra.Clear();
        }
    }

    // ============================================================
    // COMPROBAR DERECHO EXTRA DE UNA MATRÍCULA
    // ============================================================

    public bool AlumnoTieneDerechoExtra(
        string matricula)
    {
        if (string.IsNullOrWhiteSpace(
                matricula))
        {
            return false;
        }

        string claveAsignatura =
            ObtenerClaveAsignaturaDesdeCap(
                _archivoCompletoActual);

        if (string.IsNullOrWhiteSpace(
                claveAsignatura))
        {
            return false;
        }

        try
        {
            using var lite =
                new SqliteService();

            return lite.TieneDerechoExtra(
                claveAsignatura,
                matricula.Trim());
        }
        catch
        {
            return false;
        }
    }

    // ============================================================
    // MANEJAR CAMBIOS PENDIENTES
    // ============================================================

    private bool ManejarCambiosPendientes()
    {
        if (!TieneCambios)
            return true;

        var res =
            MessageBox.Show(
                "Hay cambios sin guardar en esta vista. ¿Deseas guardar antes de cambiar?\n\n" +
                "Sí = Guardar y continuar\n" +
                "No = Descartar cambios\n" +
                "Cancelar = Quedarse aquí",
                "Cambios sin guardar",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

        if (res ==
            MessageBoxResult.Cancel)
        {
            return false;
        }

        if (res ==
            MessageBoxResult.Yes)
        {
            Guardar();
        }

        TieneCambios =
            false;

        return true;
    }

    // ============================================================
    // RECARGAR CONFIGURACIÓN Y ARCHIVO ACTUAL
    // ============================================================

    public void RecargarConfiguracionYArchivoActual()
    {
        CargarProfesores();

        if (!string.IsNullOrWhiteSpace(
                ArchivoSeleccionado))
        {
            CargarArchivoSeleccionado(
                ArchivoSeleccionado);

            return;
        }

        CargarAlumnosConDerechoExtra();
    }

    // ============================================================
    // CLAVE MATERIA DESDE RUTA
    // ============================================================

    private static string ObtenerClaveMateriaDesdeRuta(
        string? rutaCompleta)
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
            : nombre.Trim().Replace(
                ' ',
                '_');
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

        return evaluacion.Trim().ToUpperInvariant() switch
        {
            "P1" =>
                _configuracionActual.Parcial1Habilitado,

            "P2" =>
                _configuracionActual.Parcial2Habilitado,

            "P3" =>
                _configuracionActual.Parcial3Habilitado,

            "SEM" =>
                _configuracionActual.SemestralHabilitado,

            "PREEXTRAORDINARIO" =>
                _configuracionActual.PreExtraordinarioHabilitado,

            "EXTRA" =>
                _configuracionActual.ExtraHabilitado,

            _ => true
        };
    }

    private string ObtenerNombreEvaluacionVisual(
        string eval)
    {
        return eval.ToUpperInvariant() switch
        {
            "P1" => "PARCIAL 1",
            "P2" => "PARCIAL 2",
            "P3" => "PARCIAL 3",
            "SEM" => "SEMESTRAL",
            "PREEXTRAORDINARIO" => "PREEXTRA",
            "EXTRA" => "EXTRA",
            _ => eval
        };
    }

    // ============================================================
    // BUSCAR CARPETA
    // ============================================================

    [RelayCommand]
    public void BuscarCarpeta()
    {
        try
        {
            var carpetaData =
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory
                    ?? string.Empty,
                    "data");

            if (!Directory.Exists(
                    carpetaData))
            {
                Directory.CreateDirectory(
                    carpetaData);
            }

            RutaUsb =
                carpetaData;

            RutaUsbEditable =
                false;

            RutaUsbLabel =
                "Raiz";

            ProcesarDirectorioSeleccionado();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudo acceder a la carpeta de datos.\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // ============================================================
    // PROCESAR DIRECTORIO
    // ============================================================

    private void ProcesarDirectorioSeleccionado()
    {
        if (!string.IsNullOrWhiteSpace(
                RutaUsb))
        {
            GlobalSettings.CurrentCapDirectory =
                RutaUsb;
        }

        ArchivosDisponibles.Clear();
        Alumnos.Clear();
        AlumnosConDerechoExtra.Clear();
        EvaluacionesDisponibles.Clear();

        _mapaArchivos.Clear();
        _evaluacionIdPorNombre.Clear();

        _archivoCompletoActual = null;

        TieneCambios = false;

        _archivoSeleccionado = null;

        OnPropertyChanged(
            nameof(ArchivoSeleccionado));

        _evaluacionSeleccionada = null;

        OnPropertyChanged(
            nameof(EvaluacionSeleccionada));

        CurrentView =
            "List";

        var archivos =
            _scannerService
                .ObtenerArchivosCap(
                    RutaUsb);

        foreach (var archivo in archivos)
        {
            var info =
                _parserService
                    .ObtenerInfoParaCombo(
                        archivo);

            string nombreCombo;

            if (info.IsExtra)
            {
                string profesorMostrar =
                    string.IsNullOrWhiteSpace(
                        info.NombreProfesor)
                        ? "SIN REGISTRO"
                        : info.NombreProfesor;

                nombreCombo =
                    $"{info.NombreBase} - Grupo: {profesorMostrar}";
            }
            else
            {
                var resTemp =
                    _parserService
                        .ProcesarArchivoCompleto(
                            archivo);

                string grupoNormal =
                    resTemp.Alumnos
                        .FirstOrDefault()
                        ?.Grupo
                    ?? "S/G";

                nombreCombo =
                    $"{info.NombreBase} - Grupo: {grupoNormal}";
            }

            _mapaArchivos[nombreCombo] =
                archivo;

            ArchivosDisponibles.Add(
                nombreCombo);
        }

        if (ArchivosDisponibles.Any())
        {
            ArchivoSeleccionado =
                ArchivosDisponibles.First();
        }

        OnPropertyChanged(
            nameof(EsExtraSeleccionado));

        OnPropertyChanged(
            nameof(EsPreExtraordinarioSeleccionado));
    }

    // ============================================================
    // CARGAR ARCHIVO SELECCIONADO
    // ============================================================

    private void CargarArchivoSeleccionado(
        string? value)
    {
        _isUpdatingProgrammatically = true;

        try
        {
            Alumnos.Clear();

            AlumnosConDerechoExtra.Clear();

            EvaluacionesDisponibles.Clear();

            _evaluacionIdPorNombre.Clear();

            _evaluacionSeleccionada = null;

            OnPropertyChanged(
                nameof(EvaluacionSeleccionada));

            CurrentView =
                "List";

            _archivoCompletoActual =
                null;

            if (string.IsNullOrWhiteSpace(value) ||
                !_mapaArchivos.TryGetValue(
                    value,
                    out string? rutaCompleta))
            {
                return;
            }

            if (!File.Exists(
                    rutaCompleta))
            {
                return;
            }

            _archivoCompletoActual =
                rutaCompleta;

            // ====================================================
            // DETECTAR PROFESOR DESDE EL CAP
            // ====================================================

            CargarProfesorDelCap(
                rutaCompleta);

            string claveMateria =
                ObtenerClaveMateriaDesdeRuta(
                    rutaCompleta);

            _configuracionActual =
                _configuracionService
                    .ObtenerConfiguracion(
                        claveMateria);

            var infoCombo =
                _parserService
                    .ObtenerInfoParaCombo(
                        rutaCompleta);

            NombreMateriaExtra =
                infoCombo.NombreBase;

            NombreProfesorExtra =
                string.IsNullOrWhiteSpace(
                    infoCombo.NombreProfesor)
                    ? "SIN REGISTRO"
                    : infoCombo.NombreProfesor;

            var resultado =
                _parserService
                    .ProcesarArchivoCompleto(
                        rutaCompleta);

            foreach (var kvp in
                     resultado.EvaluacionIdPorNombre)
            {
                _evaluacionIdPorNombre[
                    kvp.Key] =
                    kvp.Value;
            }

            bool esExtra =
                resultado.EvaluacionesDisponibles.Any(
                    e =>
                        e.Equals(
                            "EXTRA",
                            StringComparison.OrdinalIgnoreCase));

            if (esExtra)
            {
                EvaluacionesDisponibles.Add(
                    new EvaluacionItem
                    {
                        Id =
                            "EXTRA",

                        Nombre =
                            "EXTRAORDINARIO/INTER"
                    });
            }
            else
            {
                foreach (
                    var eval in
                    resultado.EvaluacionesDisponibles)
                {
                    if (!EvaluacionEstaHabilitada(
                            eval))
                    {
                        continue;
                    }

                    EvaluacionesDisponibles.Add(
                        new EvaluacionItem
                        {
                            Id =
                                eval,

                            Nombre =
                                ObtenerNombreEvaluacionVisual(
                                    eval)
                        });
                }

                if (_configuracionActual
                        .PreExtraordinarioHabilitado)
                {
                    bool existePRE =
                        EvaluacionesDisponibles.Any(
                            e =>
                                string.Equals(
                                    e.Id,
                                    "PREEXTRAORDINARIO",
                                    StringComparison.OrdinalIgnoreCase));

                    if (!existePRE)
                    {
                        EvaluacionesDisponibles.Add(
                            new EvaluacionItem
                            {
                                Id =
                                    "PREEXTRAORDINARIO",

                                Nombre =
                                    "PREEXTRAORDINARIO"
                            });
                    }
                }

                if (EvaluacionesDisponibles.Count == 0)
                {
                    EvaluacionesDisponibles.Add(
                        new EvaluacionItem
                        {
                            Id =
                                "P1",

                            Nombre =
                                "PARCIAL 1"
                        });
                }
            }

            foreach (var alumno in
                     resultado.Alumnos)
            {
                Alumnos.Add(
                    alumno);
            }

            SuscribirAlumnos();

            // ====================================================
            // FILTRAR ALUMNOS AUTORIZADOS PARA EXTRA
            // ====================================================

            CargarAlumnosConDerechoExtra();

            if (esExtra)
            {
                EvaluacionSeleccionada =
                    "EXTRA";
            }
            else if (
                _configuracionActual
                    .PreExtraordinarioHabilitado &&
                EvaluacionesDisponibles.Any(
                    e =>
                        string.Equals(
                            e.Id,
                            "PREEXTRAORDINARIO",
                            StringComparison.OrdinalIgnoreCase)))
            {
                EvaluacionSeleccionada =
                    "PREEXTRAORDINARIO";
            }
            else
            {
                EvaluacionSeleccionada =
                    EvaluacionesDisponibles
                        .LastOrDefault()
                        ?.Id;
            }

            OnPropertyChanged(
                nameof(EsExtraSeleccionado));

            OnPropertyChanged(
                nameof(EsPreExtraordinarioSeleccionado));

            TieneCambios =
                false;
        }
        finally
        {
            Application.Current?
                .Dispatcher
                .BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(
                        () =>
                        {
                            _isUpdatingProgrammatically =
                                false;

                            if (string.Equals(
                                    CurrentView,
                                    "Extra",
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                ActualizarConteoEvaluadosExtra();
                            }
                        }));
        }
    }

    // ============================================================
    // CAMBIAR EVALUACIÓN
    // ============================================================

    private void CambiarEvaluacion(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            CurrentView =
                "List";

            OnPropertyChanged(
                nameof(EsExtraSeleccionado));

            OnPropertyChanged(
                nameof(EsPreExtraordinarioSeleccionado));

            return;
        }

        string valor =
            value.ToUpperInvariant();

        if (valor != "EXTRA" &&
            EvaluacionesDisponibles.Count == 1 &&
            EvaluacionesDisponibles.Any(
                e => e.Id == "EXTRA"))
        {
            valor =
                "EXTRA";
        }

        if (valor == "SEM")
        {
            CurrentView =
                "Semestral";

            SincronizarCalificacionSemestral();
        }
        else if (
            valor == "PREEXTRAORDINARIO")
        {
            CurrentView =
                "Preextraordinario";
        }
        else if (
            valor == "EXTRA")
        {
            CurrentView =
                "Extra";

            // ====================================================
            // RECARGAR FILTRO EXTRA
            // ====================================================

            CargarAlumnosConDerechoExtra();

            ActualizarConteoEvaluadosExtra();
        }
        else
        {
            CurrentView =
                "Parciales";
        }

        foreach (var alumno in Alumnos)
        {
            if (valor != "PREEXTRAORDINARIO")
            {
                alumno.ActualizarSeleccion(
                    value);
            }
        }

        OnPropertyChanged(
            nameof(EsExtraSeleccionado));

        OnPropertyChanged(
            nameof(EsPreExtraordinarioSeleccionado));

        TieneCambios =
            false;
    }

    // ============================================================
    // GUARDAR RESULTADOS PRE EN CAP
    // ============================================================

    public bool GuardarResultadosPreEnCap()
    {
        if (string.IsNullOrWhiteSpace(
                _archivoCompletoActual))
        {
            return false;
        }

        try
        {
            bool guardadoP1 = false;
            bool guardadoP2 = false;
            bool guardadoP3 = false;
            bool guardadoSem = false;

            if (_evaluacionIdPorNombre.TryGetValue(
                    "P1",
                    out var idP1) &&
                !string.IsNullOrWhiteSpace(idP1))
            {
                guardadoP1 =
                    _writerService
                        .GuardarEvaluacion(
                            _archivoCompletoActual,
                            Alumnos.ToList(),
                            "P1",
                            idP1);
            }

            if (_evaluacionIdPorNombre.TryGetValue(
                    "P2",
                    out var idP2) &&
                !string.IsNullOrWhiteSpace(idP2))
            {
                guardadoP2 =
                    _writerService
                        .GuardarEvaluacion(
                            _archivoCompletoActual,
                            Alumnos.ToList(),
                            "P2",
                            idP2);
            }

            if (_evaluacionIdPorNombre.TryGetValue(
                    "P3",
                    out var idP3) &&
                !string.IsNullOrWhiteSpace(idP3))
            {
                guardadoP3 =
                    _writerService
                        .GuardarEvaluacion(
                            _archivoCompletoActual,
                            Alumnos.ToList(),
                            "P3",
                            idP3);
            }

            if (_evaluacionIdPorNombre.TryGetValue(
                    "SEM",
                    out var idSem) &&
                !string.IsNullOrWhiteSpace(idSem))
            {
                guardadoSem =
                    _writerService
                        .GuardarEvaluacion(
                            _archivoCompletoActual,
                            Alumnos.ToList(),
                            "SEM",
                            idSem);
            }

            return
                guardadoP1 ||
                guardadoP2 ||
                guardadoP3 ||
                guardadoSem;
        }
        catch
        {
            return false;
        }
    }

    // ============================================================
    // GUARDAR
    // ============================================================

    [RelayCommand]
    public void Guardar()
    {
        if (string.IsNullOrWhiteSpace(
                _archivoCompletoActual))
        {
            MessageBox.Show(
                "No hay archivo cargado para guardar.",
                "Aviso",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (string.IsNullOrWhiteSpace(
                EvaluacionSeleccionada))
        {
            MessageBox.Show(
                "No hay evaluación seleccionada.",
                "Aviso",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (string.Equals(
                EvaluacionSeleccionada,
                "PREEXTRAORDINARIO",
                StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(
                "El PREEXTRAORDINARIO se guarda automáticamente al capturar cada calificación.",
                "PREEXTRAORDINARIO",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            TieneCambios =
                false;

            return;
        }

        if (!_evaluacionIdPorNombre.TryGetValue(
                EvaluacionSeleccionada,
                out var idEval))
        {
            MessageBox.Show(
                "No se pudo localizar el ID de la evaluación en el CAP.",
                "Aviso",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (string.Equals(
                CurrentView,
                "Parciales",
                StringComparison.OrdinalIgnoreCase))
        {
            ParcialesVm.PrepararGuardado();
        }

        var ok =
            _writerService
                .GuardarEvaluacion(
                    _archivoCompletoActual,
                    Alumnos.ToList(),
                    EvaluacionSeleccionada,
                    idEval);

        _isUpdatingProgrammatically =
            true;

        try
        {
            SincronizarCalificacionSemestral();
        }
        finally
        {
            _isUpdatingProgrammatically =
                false;
        }

        if (
            !string.Equals(
                EvaluacionSeleccionada,
                "SEM",
                StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(
                EvaluacionSeleccionada,
                "EXTRA",
                StringComparison.OrdinalIgnoreCase) &&
            _evaluacionIdPorNombre.TryGetValue(
                "SEM",
                out var idSem))
        {
            _writerService
                .GuardarEvaluacion(
                    _archivoCompletoActual,
                    Alumnos.ToList(),
                    "SEM",
                    idSem);
        }

        TieneCambios =
            false;

        if (ParcialesVm != null)
            ParcialesVm.TieneCambios = false;

        MessageBox.Show(
            ok
                ? "Guardado correcto."
                : "No se pudo guardar el archivo CAP.",
            ok
                ? "OK"
                : "Error",
            MessageBoxButton.OK,
            ok
                ? MessageBoxImage.Information
                : MessageBoxImage.Error);
    }

    // ============================================================
    // SINCRONIZAR CALIFICACIÓN SEMESTRAL
    // ============================================================

    private void SincronizarCalificacionSemestral()
    {
        if (string.IsNullOrWhiteSpace(
                ArchivoSeleccionado))
        {
            return;
        }

        string claveMateria =
            string.Empty;

        if (!string.IsNullOrWhiteSpace(
                ArchivoCompletoActual))
        {
            try
            {
                var nombre =
                    Path.GetFileNameWithoutExtension(
                        ArchivoCompletoActual);

                if (!string.IsNullOrWhiteSpace(
                        nombre))
                {
                    claveMateria =
                        nombre
                            .Trim()
                            .Replace(
                                ' ',
                                '_');
                }
            }
            catch
            {
            }
        }

        if (string.IsNullOrWhiteSpace(
                claveMateria) &&
            !string.IsNullOrWhiteSpace(
                ArchivoSeleccionado))
        {
            string texto =
                ArchivoSeleccionado.Trim();

            int indexEspacio =
                texto.IndexOf(
                    " - Grupo:");

            if (indexEspacio > 0)
            {
                texto =
                    texto
                        .Substring(
                            0,
                            indexEspacio)
                        .Trim();
            }

            int indexSegundoEspacio =
                texto.IndexOf(' ');

            if (indexSegundoEspacio <= 0)
            {
                claveMateria =
                    texto.Replace(
                        ' ',
                        '_');
            }
            else
            {
                string clave =
                    texto[..indexSegundoEspacio]
                        .Trim();

                string nombre =
                    texto[
                        (indexSegundoEspacio + 1)..]
                        .Trim();

                claveMateria =
                    string.IsNullOrWhiteSpace(
                        nombre)
                        ? clave
                        : $"{clave}_{nombre}";
            }
        }

        if (string.IsNullOrWhiteSpace(
                claveMateria))
        {
            return;
        }

        var jsonService =
            new ParcialJsonService();

        var m1 =
            jsonService.ObtenerMateria(
                $"{claveMateria}_P1");

        var m2 =
            jsonService.ObtenerMateria(
                $"{claveMateria}_P2");

        var m3 =
            jsonService.ObtenerMateria(
                $"{claveMateria}_P3");

        if (m1 == null ||
            m2 == null ||
            m3 == null)
        {
            return;
        }

        bool p1Activa =
            m1.Calificaciones.TryGetValue(
                "$CONFIG$",
                out var c1) &&
            c1.TryGetValue(
                "AsistenciaActiva",
                out var aa1) &&
            aa1 > 0;

        bool p2Activa =
            m2.Calificaciones.TryGetValue(
                "$CONFIG$",
                out var c2) &&
            c2.TryGetValue(
                "AsistenciaActiva",
                out var aa2) &&
            aa2 > 0;

        bool p3Activa =
            m3.Calificaciones.TryGetValue(
                "$CONFIG$",
                out var c3) &&
            c3.TryGetValue(
                "AsistenciaActiva",
                out var aa3) &&
            aa3 > 0;

        int clasesP1 =
            p1Activa &&
            c1 != null &&
            c1.TryGetValue(
                "ClasesTotales",
                out var ct1) &&
            ct1 > 0
                ? (int)ct1
                : 0;

        int clasesP2 =
            p2Activa &&
            c2 != null &&
            c2.TryGetValue(
                "ClasesTotales",
                out var ct2) &&
            ct2 > 0
                ? (int)ct2
                : 0;

        int clasesP3 =
            p3Activa &&
            c3 != null &&
            c3.TryGetValue(
                "ClasesTotales",
                out var ct3) &&
            ct3 > 0
                ? (int)ct3
                : 0;

        foreach (var alumno in Alumnos)
        {
            string? califP1Str =
                alumno.Calificación["P1"];

            string? califP2Str =
                alumno.Calificación["P2"];

            string? califP3Str =
                alumno.Calificación["P3"];

            bool p1Valida =
                double.TryParse(
                    califP1Str,
                    out double p1Num);

            bool p2Valida =
                double.TryParse(
                    califP2Str,
                    out double p2Num);

            bool p3Valida =
                double.TryParse(
                    califP3Str,
                    out double p3Num);

            if (!p1Valida ||
                !p2Valida ||
                !p3Valida)
            {
                alumno.Calificación["SEM"] =
                    string.Empty;

                continue;
            }

            double promedio =
                (p1Num +
                 p2Num +
                 p3Num) /
                3.0;

            int promedioRedondeado =
                RedondearPromedio(
                    promedio);

            int faltasP1 =
                p1Activa &&
                m1.Calificaciones.TryGetValue(
                    alumno.Matricula,
                    out var cap1) &&
                cap1.TryGetValue(
                    "__Inasistencias__",
                    out var f1) &&
                f1 >= 0
                    ? (int)f1
                    : 0;

            int faltasP2 =
                p2Activa &&
                m2.Calificaciones.TryGetValue(
                    alumno.Matricula,
                    out var cap2) &&
                cap2.TryGetValue(
                    "__Inasistencias__",
                    out var f2) &&
                f2 >= 0
                    ? (int)f2
                    : 0;

            int faltasP3 =
                p3Activa &&
                m3.Calificaciones.TryGetValue(
                    alumno.Matricula,
                    out var cap3) &&
                cap3.TryGetValue(
                    "__Inasistencias__",
                    out var f3) &&
                f3 >= 0
                    ? (int)f3
                    : 0;

            int totalClases =
                clasesP1 +
                clasesP2 +
                clasesP3;

            int totalFaltas =
                faltasP1 +
                faltasP2 +
                faltasP3;

            bool cumpleAsistencia = true;

            if (totalClases > 0)
            {
                int asistencias =
                    totalClases -
                    totalFaltas;

                double porcentajeAsistencia =
                    ((double)asistencias /
                     totalClases) *
                    100.0;

                cumpleAsistencia =
                    porcentajeAsistencia >= 80.0;
            }

            alumno.Calificación["SEM"] =
                cumpleAsistencia
                    ? promedioRedondeado.ToString()
                    : "0";
        }
    }

    private int RedondearPromedio(
        double promedio)
    {
        if (promedio < 6.0)
        {
            return (int)Math.Floor(
                promedio);
        }

        return (int)Math.Round(
            promedio,
            MidpointRounding.AwayFromZero);
    }

    // ============================================================
    // SUSCRIBIR ALUMNOS
    // ============================================================

    private void SuscribirAlumnos()
    {
        foreach (var a
                 in _subscribedAlumnos)
        {
            try
            {
                a.Calificación.PropertyChanged -=
                    Alumno_CalificacionChanged;
            }
            catch
            {
            }
        }

        _subscribedAlumnos.Clear();

        foreach (var alumno in Alumnos)
        {
            if (alumno?.Calificación != null)
            {
                alumno.Calificación.PropertyChanged +=
                    Alumno_CalificacionChanged;

                _subscribedAlumnos.Add(
                    alumno);
            }
        }
    }

    // ============================================================
    // CAMBIO DE CALIFICACIÓN
    // ============================================================

    private void Alumno_CalificacionChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (_isUpdatingProgrammatically)
            return;

        if (e?.PropertyName != null &&
            e.PropertyName.StartsWith(
                "Item[",
                StringComparison.Ordinal))
        {
            TieneCambios = true;

            if (EsExtraSeleccionado)
            {
                ActualizarConteoEvaluadosExtra();
            }
        }
    }

    // ============================================================
    // DERECHO PRE
    // ============================================================

    public bool AlumnoTieneDerechoPre(
        string matricula)
    {
        if (string.IsNullOrWhiteSpace(
                matricula))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(
                _archivoCompletoActual))
        {
            return false;
        }

        string claveMateria =
            ObtenerClaveMateriaDesdeRuta(
                _archivoCompletoActual);

        if (string.IsNullOrWhiteSpace(
                claveMateria))
        {
            return false;
        }

        try
        {
            using var lite =
                new SqliteService();

            return lite.TieneDerechoPre(
                claveMateria,
                matricula);
        }
        catch
        {
            return false;
        }
    }
}