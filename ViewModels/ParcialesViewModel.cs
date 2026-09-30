using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.ViewModels;

public class AlumnoFaltante
{
    public string Materia { get; set; } = string.Empty;
    public string Grupo { get; set; } = string.Empty;
    public string Matricula { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Razon { get; set; } = string.Empty;
}

public partial class ParcialesViewModel : ObservableObject
{
    private readonly MainViewModel _mainVm;
    private readonly ParcialJsonService _parcialJsonService;
    private readonly PreExtraordinarioService _preService;

    private readonly Dictionary<string, string> _mapaGrupos =
        new(StringComparer.OrdinalIgnoreCase);

    private MateriaParcial _materia = new();

    private bool _cargando;
    private int _cargasActivas = 0;

    private string _claveMateria = string.Empty;
    private string _evaluacionActual = string.Empty;

    private string? _ultimaMatriculaSeleccionada;

    [ObservableProperty] private bool _tieneCambios;

    private string? _lastArchivoSeleccionado;
    private string? _lastEvaluacionSeleccionada;

    private bool _isReady = false;
    private bool _suspendUserEditMarking = false;

    private DateTime? _lastUserEditTime;
    private DateTime _lastLoadOrSaveTime = DateTime.MinValue;

    // ============================================================
    // EVITAR RECÁLCULOS MASIVOS REENTRANTES
    // ============================================================

    private bool _recalculandoTodoParcial = false;

    public ObservableCollection<Alumno> Alumnos =>
        _mainVm.Alumnos;

    public ObservableCollection<ActividadParcialEditor> Actividades { get; } =
        new();

    public MainViewModel MainVm =>
        _mainVm;

    [ObservableProperty] private Alumno? _alumnoSeleccionado;

    [ObservableProperty] private string _nombreMateria = string.Empty;
    [ObservableProperty] private string _nombreProfesor = string.Empty;

    [ObservableProperty] private string _nombreEvaluacion = string.Empty;

    [ObservableProperty] private string _nombreAlumno = string.Empty;

    [ObservableProperty] private string _matriculaAlumno = string.Empty;

    [ObservableProperty] private string _grupoAlumno = string.Empty;

    [ObservableProperty] private bool _mostrarGrupo = true;

    [ObservableProperty] private decimal _sumaPorcentajes;

    [ObservableProperty] private string _sumaPorcentajesTexto = "0%";

    [ObservableProperty] private string _porcentajeEstado = "Ok";

    [ObservableProperty] private bool _sumaValida = false;

    [ObservableProperty] private string _calificacionParcialTexto = string.Empty;

    [ObservableProperty] private string _estadoValidacion = "Sin cargar";

    [ObservableProperty] private string _estadoGuardado = string.Empty;

    [ObservableProperty] private bool _asistenciaActiva;

    [ObservableProperty] private string _clasesTotales = string.Empty;

    [ObservableProperty] private string _inasistencias = string.Empty;

    [ObservableProperty] private bool _alumnoConCapturaDirecta;

    [ObservableProperty] private bool _capturaDirectaActiva;

    [ObservableProperty] private string _leyendaCapturaDirecta = string.Empty;

    [ObservableProperty] private bool _preActivo;

    [ObservableProperty] private bool _calificacionParcialEditable;

    [ObservableProperty] private string _textoEvaluados = string.Empty;

    [ObservableProperty] private bool _faltanPorEvaluar = false;

    public List<AlumnoFaltante> ListaNoEvaluados { get; private set; } =
        new();

    public ParcialesViewModel(MainViewModel mainVm)
    {
        _mainVm =
            mainVm ??
            throw new ArgumentNullException(nameof(mainVm));

        _parcialJsonService =
            new ParcialJsonService();

        _preService =
            new PreExtraordinarioService();

        CargarMapaGrupos();

        _mainVm.PropertyChanged +=
            MainVm_PropertyChanged;

        if (_mainVm.Alumnos.Any())
        {
            _ultimaMatriculaSeleccionada =
                _mainVm.Alumnos.First().Matricula;

            AlumnoSeleccionado =
                _mainVm.Alumnos.First();
        }

        CargarContextoActual();
    }

    public void ActualizarConteoEvaluados()
    {
        if (string.IsNullOrWhiteSpace(_evaluacionActual))
        {
            TextoEvaluados = "0 de 0";
            FaltanPorEvaluar = false;
            ListaNoEvaluados = new List<AlumnoFaltante>();
            return;
        }

        int total =
            Alumnos.Count;

        int evaluados = 0;

        var lista =
            new List<AlumnoFaltante>();

        string materiaMostrada =
            NombreMateria;

        int sepIdx =
            materiaMostrada.IndexOf(
                " - Grupo:");

        if (sepIdx > 0)
        {
            materiaMostrada =
                materiaMostrada
                    .Substring(
                        0,
                        sepIdx)
                    .Trim();
        }

        foreach (var alumno in Alumnos)
        {
            bool hasCalif =
                !string.IsNullOrWhiteSpace(
                    alumno.Calificación[
                        _evaluacionActual]);

            bool hasFaltas = true;

            if (AsistenciaActiva)
            {
                hasFaltas =
                    _materia.Calificaciones.TryGetValue(
                        alumno.Matricula,
                        out var caps) &&
                    caps.TryGetValue(
                        "__Inasistencias__",
                        out var f) &&
                    f >= 0;
            }

            if (hasCalif && hasFaltas)
            {
                evaluados++;
                continue;
            }

            string razon;

            if (!hasCalif && !hasFaltas)
            {
                razon =
                    "Falta calificación e inasistencias";
            }
            else if (!hasCalif)
            {
                razon =
                    "Falta calificación";
            }
            else
            {
                razon =
                    "Falta registrar inasistencias";
            }

            string gp =
                ObtenerGrupoDesdeJson(
                    alumno.Matricula);

            if (string.IsNullOrWhiteSpace(gp))
                gp = alumno.Grupo;

            if (string.IsNullOrWhiteSpace(gp))
                gp = "S/G";

            lista.Add(
                new AlumnoFaltante
                {
                    Materia = materiaMostrada,
                    Grupo = gp,
                    Matricula = alumno.Matricula,
                    Nombre = alumno.Nombre,
                    Razon = razon
                });
        }

        TextoEvaluados =
            $"{evaluados} de {total}";

        FaltanPorEvaluar =
            evaluados < total;

        ListaNoEvaluados =
            lista;
    }

    private void EditorChanged()
    {
        if (_recalculandoTodoParcial)
            return;

        // ============================================================
        // GUARDAR EN MEMORIA LA CAPTURA ACTUAL DEL ALUMNO
        // ANTES DE RECALCULAR EL PARCIAL COMPLETO.
        //
        // NO ES GUARDADO FÍSICO TODAVÍA.
        // ============================================================

        if (
            !_cargando &&
            _cargasActivas == 0 &&
            AlumnoSeleccionado != null)
        {
            PersistirCapturasTemporales(
                AlumnoSeleccionado.Matricula);
        }

        // ============================================================
        // ACTUALIZAR LA CONFIGURACIÓN EN _materia
        // ============================================================

        _materia.Actividades =
            Actividades
                .Select(a => a.ToModelo())
                .ToList();

        // ============================================================
        // RECALCULAR INMEDIATAMENTE EL ALUMNO MOSTRADO
        // ============================================================

        RecalcularTodo(
            guardarJson: false);

        // ============================================================
        // SI LA CONFIGURACIÓN ES VÁLIDA, RECALCULAR EN MEMORIA
        // TODOS LOS ALUMNOS DEL PARCIAL SELECCIONADO.
        //
        // NO SE GUARDA FÍSICAMENTE TODAVÍA.
        // ============================================================

        if (
            ConfiguracionActividadesValidaParaRecalculoMasivo())
        {
            RecalcularCalificacionesDeTodosLosAlumnosDelParcial(
                actualizarUiAlumnoActual: true);
        }
    }

    partial void OnAsistenciaActivaChanged(
        bool value)
    {
        // ============================================================
        // SOLO AL ACTIVAR ASISTENCIA MANUALMENTE:
        // llenar con 0 las inasistencias que estén vacías.
        //
        // Durante la carga de un parcial NO se ejecuta esto.
        // ============================================================

        if (
            value &&
            !_cargando &&
            _cargasActivas == 0 &&
            !_suspendUserEditMarking)
        {
            InicializarInasistenciasVacias();
        }

        RecalcularTodo(
            guardarJson: false);
    }

    private string ObtenerNombreProfesorDesdeCap(
        string rutaCompleta)
    {
        if (string.IsNullOrWhiteSpace(rutaCompleta) ||
            !File.Exists(rutaCompleta))
        {
            return string.Empty;
        }

        try
        {
            Encoding.RegisterProvider(
                CodePagesEncodingProvider.Instance);

            var encodingCap =
                Encoding.GetEncoding("iso-8859-1");

            bool dentroParametros = false;

            foreach (var linea in
                     File.ReadLines(
                         rutaCompleta,
                         encodingCap))
            {
                string texto =
                    linea.Trim();

                if (texto.Equals(
                        "[Parametros]",
                        StringComparison.OrdinalIgnoreCase))
                {
                    dentroParametros = true;
                    continue;
                }

                if (dentroParametros &&
                    texto.StartsWith("[") &&
                    texto.EndsWith("]"))
                {
                    break;
                }

                if (!dentroParametros ||
                    !texto.Contains('='))
                {
                    continue;
                }

                var partes =
                    texto.Split('=', 2);

                if (partes.Length != 2)
                    continue;

                if (partes[0].Trim().Equals(
                        "NombreProfesor",
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

    private void InicializarInasistenciasVacias()
    {
        foreach (var alumno in Alumnos)
        {
            if (alumno == null ||
                string.IsNullOrWhiteSpace(alumno.Matricula))
            {
                continue;
            }

            if (
                !_materia.Calificaciones.TryGetValue(
                    alumno.Matricula,
                    out var capturas))
            {
                capturas =
                    new Dictionary<string, double>(
                        StringComparer.OrdinalIgnoreCase);

                _materia.Calificaciones[
                        alumno.Matricula] =
                    capturas;
            }

            bool estaVacia =
                !capturas.TryGetValue(
                    "__Inasistencias__",
                    out double valor) ||
                valor < 0;

            if (!estaVacia)
                continue;

            capturas[
                    "__Inasistencias__"] =
                0.0;
        }

        if (AlumnoSeleccionado != null &&
            !string.IsNullOrWhiteSpace(
                AlumnoSeleccionado.Matricula))
        {
            if (
                _materia.Calificaciones.TryGetValue(
                    AlumnoSeleccionado.Matricula,
                    out var capturasActual) &&
                capturasActual.TryGetValue(
                    "__Inasistencias__",
                    out double valorActual) &&
                valorActual >= 0)
            {
                Inasistencias =
                    ((int)valorActual).ToString();
            }
            else
            {
                Inasistencias = "0";
            }
        }
    }

    partial void OnClasesTotalesChanged(
        string value)
    {
        RecalcularTodo(
            guardarJson: false);
    }

    partial void OnInasistenciasChanged(
        string value)
    {
        RecalcularTodo(
            guardarJson: false);
    }

    partial void OnCalificacionParcialTextoChanged(
        string value)
    {
        if (_cargando ||
            _cargasActivas > 0)
        {
            return;
        }

        if (
            AlumnoSeleccionado == null ||
            string.IsNullOrWhiteSpace(
                _evaluacionActual))
        {
            return;
        }

        if (!AlumnoConCapturaDirecta)
            return;

        try
        {
            AlumnoSeleccionado.Calificación[
                    _evaluacionActual] =
                value ?? string.Empty;

            PersistirCapturasTemporales(
                AlumnoSeleccionado.Matricula);

            MarkUserEdited();

            ActualizarConteoEvaluados();
        }
        catch
        {
        }
    }

    partial void OnAlumnoConCapturaDirectaChanged(
        bool value)
    {
        if (_cargando ||
            _cargasActivas > 0)
        {
            return;
        }

        LeyendaCapturaDirecta =
            value
                ? "Calificación directa habilitada para este alumno — para cambios diríjase al área de Servicios Escolares."
                : string.Empty;

        foreach (var ed in Actividades)
        {
            ed.SetBloqueadoPorCapturaDirecta(
                value);
        }

        CalificacionParcialEditable =
            value && !PreActivo;

        if (AlumnoSeleccionado != null)
        {
            PersistirCapturasTemporales(
                AlumnoSeleccionado.Matricula);

            MarkUserEdited();
        }

        if (!value)
        {
            RecalcularTodo(
                guardarJson: false,
                esCargaInicial: false,
                marcarCambios: false);
        }
    }

    public void MarkUserEdited()
    {
        if (_cargasActivas > 0 ||
            _cargando ||
            _suspendUserEditMarking)
        {
            return;
        }

        TieneCambios = true;

        _lastUserEditTime =
            DateTime.UtcNow;
    }

    [RelayCommand]
    private void AgregarActividad()
    {
        if (Actividades.Count >= 4)
            return;

        int numero =
            Actividades.Count + 1;

        bool esActividad1 =
            numero == 1;

        var nueva =
            new ActividadParcialEditor(
                () => EditorChanged(),
                numero,
                esActividad1);

        nueva.Activa =
            esActividad1;

        nueva.Nombre =
            string.Empty;

        nueva.Porcentaje =
            string.Empty;

        nueva.PuntajeMaximo =
            string.Empty;

        Actividades.Add(nueva);

        _materia.Actividades =
            Actividades
                .Select(a => a.ToModelo())
                .ToList();

        RecalcularTodo(
            guardarJson: false);
    }

    [RelayCommand]
    private void QuitarActividad(
        ActividadParcialEditor editor)
    {
        if (editor == null)
            return;

        if (!Actividades.Contains(editor))
            return;

        if (Actividades.Count <= 1)
            return;

        Actividades.Remove(editor);

        for (int i = 0; i < Actividades.Count; i++)
        {
            Actividades[i].EstablecerNumeroActividad(
                i + 1);
        }

        _materia.Actividades =
            Actividades
                .Select(a => a.ToModelo())
                .ToList();

        RecalcularTodo(
            guardarJson: false);
    }

    public void PrepararGuardado()
    {
        // ============================================================
        // NORMALIZAR EL ALUMNO ACTUAL
        //
        // Una actividad activa vacía se considera SC al guardar.
        // ============================================================

        foreach (var ed in Actividades)
        {
            if (
                ed.Activa &&
                string.IsNullOrWhiteSpace(
                    ed.PuntajeObtenido))
            {
                ed.EstablecerPuntajeDesdeCarga(
                    "SC");
            }
        }

        // ============================================================
        // PERSISTIR EN MEMORIA LA CAPTURA ACTUAL
        // ============================================================

        if (AlumnoSeleccionado != null)
        {
            PersistirCapturasTemporales(
                AlumnoSeleccionado.Matricula);
        }

        // ============================================================
        // ASEGURAR QUE LA CONFIGURACIÓN ACTUAL ESTÉ EN _materia
        // ============================================================

        _materia.Actividades =
            Actividades
                .Select(a => a.ToModelo())
                .ToList();

        // ============================================================
        // RECALCULAR TODOS LOS ALUMNOS DEL PARCIAL SELECCIONADO
        //
        // ESTO OCURRE ANTES DE GuardarEnJsonLocal()
        // Y ANTES DE CapWriterService.GuardarEvaluacion().
        // ============================================================

        RecalcularCalificacionesDeTodosLosAlumnosDelParcial(
            actualizarUiAlumnoActual: true);

        // ============================================================
        // GUARDAR PARCIALES.DB
        // ============================================================

        GuardarEnJsonLocal();
    }

    // ============================================================
    // VALIDAR CONFIGURACIÓN ACTUAL PARA RECÁLCULO MASIVO
    // ============================================================

    private bool ConfiguracionActividadesValidaParaRecalculoMasivo()
    {
        decimal suma =
            0m;

        bool existeActividadActiva =
            false;

        foreach (var actividad in Actividades)
        {
            if (!actividad.Activa)
                continue;

            existeActividadActiva =
                true;

            if (string.IsNullOrWhiteSpace(
                    actividad.Nombre))
            {
                return false;
            }

            if (
                !double.TryParse(
                    actividad.Porcentaje,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double porc) ||
                porc < 0 ||
                porc > 100)
            {
                return false;
            }

            if (
                !double.TryParse(
                    actividad.PuntajeMaximo,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double max) ||
                max <= 0)
            {
                return false;
            }

            suma +=
                (decimal)porc;
        }

        return
            existeActividadActiva &&
            suma > 0m;
    }

    // ============================================================
    // RECALCULAR LA CALIFICACIÓN DE UN ALUMNO UTILIZANDO
    // LAS CAPTURAS GUARDADAS EN _materia.Calificaciones.
    //
    // REGLAS:
    // - TODAS las actividades activas deben tener captura.
    // - SC (-1) cuenta como captura.
    // - SC NO aporta puntos al cálculo.
    // - Las actividades con puntaje numérico sí aportan.
    // - Si no existe ningún puntaje numérico, no se genera
    //   calificación.
    // - Si un puntaje supera el máximo actual, no se genera
    //   una calificación válida.
    // ============================================================

    private bool IntentarCalcularCalificacionDesdeCapturas(
        Dictionary<string, double> capturas,
        out string calificacion)
    {
        calificacion =
            string.Empty;

        if (capturas == null)
            return false;

        decimal sumaPorcentajes =
            0m;

        decimal acumulado =
            0m;

        var entradas =
            new List<(double porc, double max, double obt)>();

        bool todasLasActividadesCapturadas =
            true;

        bool hayPuntajesNumericos =
            false;

        foreach (var actividad in Actividades)
        {
            if (!actividad.Activa)
                continue;

            // ========================================================
            // CONFIGURACIÓN
            // ========================================================

            if (
                string.IsNullOrWhiteSpace(
                    actividad.Nombre))
            {
                return false;
            }

            if (
                !double.TryParse(
                    actividad.Porcentaje,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double porc) ||
                porc < 0 ||
                porc > 100)
            {
                return false;
            }

            if (
                !double.TryParse(
                    actividad.PuntajeMaximo,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double max) ||
                max <= 0)
            {
                return false;
            }

            sumaPorcentajes +=
                (decimal)porc;

            string nombreActividad =
                actividad.Nombre.Trim();

            // ========================================================
            // TODA ACTIVIDAD ACTIVA DEBE TENER CAPTURA
            //
            // Número = capturada
            // SC (-1) = capturada
            // Ausencia de clave = no capturada
            // ========================================================

            if (
                !capturas.TryGetValue(
                    nombreActividad,
                    out double valor))
            {
                todasLasActividadesCapturadas =
                    false;

                continue;
            }

            // ========================================================
            // SC:
            // CAPTURA VÁLIDA, PERO NO PARTICIPA EN EL CÁLCULO.
            // ========================================================

            if (valor == -1)
            {
                continue;
            }

            // ========================================================
            // OTROS VALORES NEGATIVOS NO SON VÁLIDOS.
            // ========================================================

            if (valor < 0)
            {
                return false;
            }

            // ========================================================
            // EL PUNTAJE NO PUEDE SUPERAR EL MÁXIMO ACTUAL.
            //
            // Esta comprobación NO sustituye el modal.
            // Solamente evita generar una calificación inválida.
            // ========================================================

            if (valor > max)
            {
                return false;
            }

            hayPuntajesNumericos =
                true;

            entradas.Add(
                (porc, max, valor));
        }

        // ============================================================
        // FALTA ALGUNA ACTIVIDAD ACTIVA.
        // ============================================================

        if (!todasLasActividadesCapturadas)
        {
            return false;
        }

        // ============================================================
        // TODAS LAS ACTIVIDADES SON SC.
        // ============================================================

        if (!hayPuntajesNumericos)
        {
            return false;
        }

        if (sumaPorcentajes <= 0m)
        {
            return false;
        }

        // ============================================================
        // CONSERVAR LA MISMA NORMALIZACIÓN QUE YA USA CEIM.
        // ============================================================

        double scaling =
            100.0 /
            (double)sumaPorcentajes;

        foreach (
            var (porc, max, obt)
            in entradas)
        {
            double porcNorm =
                porc *
                scaling;

            acumulado +=
                ((decimal)obt /
                 (decimal)max) *
                (decimal)porcNorm;
        }

        decimal resultado =
            TruncarUnDecimal(
                acumulado / 10m);

        calificacion =
            resultado.ToString(
                "0.0",
                CultureInfo.InvariantCulture);

        return true;
    }

    // ============================================================
    // RECALCULAR TODOS LOS ALUMNOS DEL PARCIAL ACTUAL
    //
    // SOLO trabaja sobre:
    //     _claveMateria + _evaluacionActual
    //
    // NO toca otras materias ni otros parciales.
    //
    // Actualiza directamente:
    //     Alumno.Calificación[_evaluacionActual]
    //
    // Esos valores son los que posteriormente recibe
    // CapWriterService.
    // ============================================================

    private void RecalcularCalificacionesDeTodosLosAlumnosDelParcial(
        bool actualizarUiAlumnoActual)
    {
        if (
            _recalculandoTodoParcial ||
            string.IsNullOrWhiteSpace(
                _evaluacionActual))
        {
            return;
        }

        _recalculandoTodoParcial =
            true;

        try
        {
            // ========================================================
            // ASEGURAR QUE _materia TIENE LA CONFIGURACIÓN ACTUAL
            // ========================================================

            _materia.Actividades =
                Actividades
                    .Select(a => a.ToModelo())
                    .ToList();

            foreach (var alumno in Alumnos)
            {
                if (
                    alumno == null ||
                    string.IsNullOrWhiteSpace(
                        alumno.Matricula))
                {
                    continue;
                }

                // ====================================================
                // CAPTURA DIRECTA
                //
                // NO SE RECALCULA.
                // ====================================================

                if (
                    _materia.Calificaciones.TryGetValue(
                        alumno.Matricula,
                        out var capturas))
                {
                    bool capturaDirecta =
                        capturas.TryGetValue(
                            "__CAPTURA_DIRECTA__",
                            out double capturaDirectaValor) &&
                        capturaDirectaValor > 0;

                    if (capturaDirecta)
                    {
                        continue;
                    }

                    // =================================================
                    // PRE PERSISTIDO
                    //
                    // NO TOCAR LA CALIFICACIÓN QUE PRE YA CONTROLA.
                    // =================================================

                    bool tienePre =
                        _preService != null &&
                        !string.IsNullOrWhiteSpace(
                            _claveMateria) &&
                        _preService
                            .ObtenerEstadoPre(
                                _claveMateria,
                                alumno.Matricula)
                            .TienePRE;

                    if (tienePre)
                    {
                        continue;
                    }

                    // =================================================
                    // RECALCULAR CON LAS CAPTURAS EXISTENTES.
                    // =================================================

                    bool calculable =
                        IntentarCalcularCalificacionDesdeCapturas(
                            capturas,
                            out string nuevaCalificacion);

                    if (calculable)
                    {
                        alumno.Calificación[
                                _evaluacionActual] =
                            nuevaCalificacion;
                    }
                    else
                    {
                        alumno.Calificación[
                                _evaluacionActual] =
                            string.Empty;
                    }
                }
                else
                {
                    // =================================================
                    // NO HAY CAPTURA PARA EL ALUMNO.
                    // LA CALIFICACIÓN DEBE QUEDAR VACÍA.
                    // =================================================

                    alumno.Calificación[
                            _evaluacionActual] =
                        string.Empty;
                }
            }

            // ========================================================
            // ACTUALIZAR LA UI DEL ALUMNO ACTUAL
            // ========================================================

            if (
                actualizarUiAlumnoActual &&
                AlumnoSeleccionado != null)
            {
                CalificacionParcialTexto =
                    AlumnoSeleccionado.Calificación[
                        _evaluacionActual]
                    ?? string.Empty;
            }

            ActualizarConteoEvaluados();
        }
        finally
        {
            _recalculandoTodoParcial =
                false;
        }
    }

    private void MainVm_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (
            e.PropertyName !=
            nameof(
                MainViewModel.ArchivoSeleccionado) &&
            e.PropertyName !=
            nameof(
                MainViewModel.EvaluacionSeleccionada))
        {
            return;
        }

        try
        {
            var main =
                sender as MainViewModel;

            string? nuevoArchivo =
                main?.ArchivoSeleccionado;

            string? nuevaEval =
                main?.EvaluacionSeleccionada;

            if (!_isReady)
            {
                _lastArchivoSeleccionado =
                    nuevoArchivo;

                _lastEvaluacionSeleccionada =
                    nuevaEval;

                _isReady = true;
                return;
            }

            bool archivoCambio =
                !string.Equals(
                    nuevoArchivo,
                    _lastArchivoSeleccionado,
                    StringComparison.OrdinalIgnoreCase);

            bool evalCambio =
                !string.Equals(
                    nuevaEval,
                    _lastEvaluacionSeleccionada,
                    StringComparison.OrdinalIgnoreCase);

            bool userEditedAfterLoad =
                _lastUserEditTime.HasValue &&
                _lastUserEditTime.Value >
                _lastLoadOrSaveTime;

            if (
                TieneCambios &&
                userEditedAfterLoad &&
                (archivoCambio || evalCambio))
            {
                var res =
                    System.Windows.MessageBox.Show(
                        "Hay cambios sin guardar. ¿Deseas guardar antes de cambiar de materia/evaluación?\nSí = Guardar, No = Descartar, Cancelar = Volver a la selección previa.",
                        "Cambios sin guardar",
                        System.Windows.MessageBoxButton.YesNoCancel,
                        System.Windows.MessageBoxImage.Warning);

                if (
                    res ==
                    System.Windows.MessageBoxResult.Cancel)
                {
                    if (
                        archivoCambio &&
                        main != null)
                    {
                        main.ArchivoSeleccionado =
                            _lastArchivoSeleccionado;
                    }

                    if (
                        evalCambio &&
                        main != null)
                    {
                        main.EvaluacionSeleccionada =
                            _lastEvaluacionSeleccionada;
                    }

                    return;
                }

                if (
                    res ==
                    System.Windows.MessageBoxResult.Yes)
                {
                    PrepararGuardado();

                    TieneCambios = false;
                    _lastUserEditTime = null;
                }

                if (
                    res ==
                    System.Windows.MessageBoxResult.No)
                {
                    TieneCambios = false;
                    _lastUserEditTime = null;
                }
            }

            CargarContextoActual();
        }
        catch
        {
            CargarContextoActual();
        }
    }

    partial void OnAlumnoSeleccionadoChanged(
        Alumno? value)
    {
        if (_cargando ||
            _cargasActivas > 0)
        {
            return;
        }

        if (
            !string.IsNullOrWhiteSpace(
                _ultimaMatriculaSeleccionada))
        {
            PersistirCapturasTemporales(
                _ultimaMatriculaSeleccionada);
        }

        _ultimaMatriculaSeleccionada =
            value?.Matricula;

        AplicarCambioAlumnoAsync();
    }

    private async void AplicarCambioAlumnoAsync()
    {
        _cargasActivas++;
        _suspendUserEditMarking = true;

        try
        {
            ActualizarDatosAlumnoSeleccionado();

            CargarCapturasDelAlumnoSeleccionado();

            RecalcularTodo(
                guardarJson: false,
                esCargaInicial: true,
                marcarCambios: false);

            ActualizarConteoEvaluados();
        }
        finally
        {
            await System.Threading.Tasks.Task.Delay(
                250);

            _cargasActivas--;

            if (_cargasActivas <= 0)
            {
                _cargasActivas = 0;
                _suspendUserEditMarking = false;
            }
        }
    }

    private string ObtenerNombreEvaluacionVisual(
        string? eval)
    {
        if (string.IsNullOrWhiteSpace(eval))
            return string.Empty;

        return eval
                .Trim()
                .ToUpperInvariant() switch
            {
                "P1" => "PARCIAL 1",
                "P2" => "PARCIAL 2",
                "P3" => "PARCIAL 3",
                "SEM" => "SEMESTRAL",
                "PREEXTRAORDINARIO" => "PREEXTRAORDINARIO",
                "EXTRA" => "EXTRAORDINARIO",
                _ => eval
            };
    }

    public async void CargarContextoActual()
    {
        _cargasActivas++;
        _cargando = true;
        _suspendUserEditMarking = true;

        try
        {
            NombreMateria =
                _mainVm.ArchivoSeleccionado ??
                string.Empty;

            NombreProfesor =
                !string.IsNullOrWhiteSpace(
                    _mainVm.ArchivoCompletoActual)
                    ? ObtenerNombreProfesorDesdeCap(
                        _mainVm.ArchivoCompletoActual)
                    : string.Empty;

            NombreEvaluacion =
                ObtenerNombreEvaluacionVisual(
                    _mainVm.EvaluacionSeleccionada);

            _evaluacionActual =
                _mainVm.EvaluacionSeleccionada ??
                string.Empty;

            MostrarGrupo =
                !string.Equals(
                    _evaluacionActual,
                    "EXTRA",
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    _evaluacionActual,
                    "SEM",
                    StringComparison.OrdinalIgnoreCase);

            _claveMateria =
                !string.IsNullOrWhiteSpace(
                    _mainVm.ArchivoCompletoActual)
                    ? ObtenerClaveMateriaDesdeNombreArchivo(
                        _mainVm.ArchivoCompletoActual)
                    : ObtenerClaveMateriaDesdeNombreVisual(
                        _mainVm.ArchivoSeleccionado);

            if (
                string.IsNullOrWhiteSpace(
                    _claveMateria) ||
                string.IsNullOrWhiteSpace(
                    _evaluacionActual))
            {
                LimpiarVista();
                return;
            }

            string claveMateriaEval =
                $"{_claveMateria}_{_evaluacionActual}";

            _materia =
                _parcialJsonService
                    .ObtenerMateria(
                        claveMateriaEval) ??
                new MateriaParcial();

            if (
                _materia.Calificaciones
                .TryGetValue(
                    "$CONFIG$",
                    out var config))
            {
                AsistenciaActiva =
                    config.TryGetValue(
                        "AsistenciaActiva",
                        out var aa) &&
                    aa > 0;

                ClasesTotales =
                    config.TryGetValue(
                        "ClasesTotales",
                        out var ct) &&
                    ct >= 0
                        ? ct.ToString()
                        : string.Empty;
            }
            else
            {
                AsistenciaActiva = false;
                ClasesTotales = string.Empty;
            }

            NormalizarActividadesEnMateria();

            ReconstruirActividadesEnPantalla();

            RestaurarAlumnoSeleccionado();

            ActualizarDatosAlumnoSeleccionado();

            CargarCapturasDelAlumnoSeleccionado();

            RecalcularTodo(
                guardarJson: false,
                esCargaInicial: true,
                marcarCambios: false);

            ActualizarConteoEvaluados();

            TieneCambios = false;
            _lastUserEditTime = null;

            _lastArchivoSeleccionado =
                _mainVm.ArchivoSeleccionado;

            _lastEvaluacionSeleccionada =
                _mainVm.EvaluacionSeleccionada;

            _lastLoadOrSaveTime =
                DateTime.UtcNow;
        }
        finally
        {
            await System.Threading.Tasks.Task.Delay(
                400);

            _cargasActivas--;

            if (_cargasActivas <= 0)
            {
                _cargasActivas = 0;
                _suspendUserEditMarking = false;
                _cargando = false;
            }
        }
    }

    private void LimpiarVista()
    {
        Actividades.Clear();

        for (int i = 0; i < 4; i++)
        {
            Actividades.Add(
                CreateBlankEditor(
                    i + 1,
                    i == 0));
        }

        SumaPorcentajes = 0;
        SumaPorcentajesTexto = "0%";
        CalificacionParcialTexto =
            string.Empty;

        EstadoValidacion =
            "Sin materia cargada";

        EstadoGuardado =
            string.Empty;

        NombreAlumno =
            string.Empty;

        MatriculaAlumno =
            string.Empty;

        GrupoAlumno =
            string.Empty;

        MostrarGrupo = false;
        AsistenciaActiva = false;
        ClasesTotales = string.Empty;
        Inasistencias = string.Empty;
        TieneCambios = false;
        _lastUserEditTime = null;

        ActualizarConteoEvaluados();
    }

    private void RestaurarAlumnoSeleccionado()
    {
        if (
            !string.IsNullOrWhiteSpace(
                _ultimaMatriculaSeleccionada))
        {
            var alumno =
                Alumnos.FirstOrDefault(a =>
                    string.Equals(
                        a.Matricula,
                        _ultimaMatriculaSeleccionada,
                        StringComparison.OrdinalIgnoreCase));

            if (alumno != null)
            {
                AlumnoSeleccionado =
                    alumno;

                return;
            }
        }

        if (
            AlumnoSeleccionado == null &&
            Alumnos.Any())
        {
            AlumnoSeleccionado =
                Alumnos.First();
        }
    }

    private void ActualizarDatosAlumnoSeleccionado()
    {
        if (AlumnoSeleccionado == null)
        {
            NombreAlumno = string.Empty;
            MatriculaAlumno = string.Empty;
            GrupoAlumno = string.Empty;
            return;
        }

        NombreAlumno =
            AlumnoSeleccionado.Nombre;

        MatriculaAlumno =
            AlumnoSeleccionado.Matricula;

        GrupoAlumno =
            ObtenerGrupoDesdeJson(
                AlumnoSeleccionado.Matricula);

        if (string.IsNullOrWhiteSpace(
                GrupoAlumno))
        {
            GrupoAlumno =
                AlumnoSeleccionado.Grupo;
        }
    }

    private void CargarCapturasDelAlumnoSeleccionado()
    {
        if (AlumnoSeleccionado == null)
        {
            foreach (var actividad in Actividades)
            {
                actividad.EstablecerPuntajeDesdeCarga(
                    actividad.Activa
                        ? "SC"
                        : string.Empty);

                actividad.SetBloqueadoPorPre(
                    false);
            }

            Inasistencias =
                string.Empty;

            AlumnoConCapturaDirecta =
                false;

            LeyendaCapturaDirecta =
                string.Empty;

            CalificacionParcialTexto =
                string.Empty;

            PreActivo = false;

            foreach (var ed in Actividades)
            {
                ed.SetBloqueadoPorCapturaDirecta(
                    false);
            }

            return;
        }

        if (
            _materia.Calificaciones.TryGetValue(
                AlumnoSeleccionado.Matricula,
                out var capturas))
        {
            bool cd =
                capturas.TryGetValue(
                    "__CAPTURA_DIRECTA__",
                    out var cdVal) &&
                cdVal > 0;

            AlumnoConCapturaDirecta =
                cd;

            LeyendaCapturaDirecta =
                cd
                    ? "Calificación directa habilitada para este alumno — para cambios diríjase al área de Servicios Escolares."
                    : string.Empty;

            PreActivo = false;

            foreach (var ed in Actividades)
            {
                ed.SetBloqueadoPorCapturaDirecta(
                    AlumnoConCapturaDirecta);

                ed.SetBloqueadoPorPre(false);

                if (
                    !string.IsNullOrWhiteSpace(
                        ed.Nombre) &&
                    capturas.TryGetValue(
                        ed.Nombre,
                        out var valor))
                {
                    if (valor == -1)
                    {
                        ed.EstablecerPuntajeDesdeCarga(
                            "SC");

                        ed.SetBloqueadoPorPre(
                            false);

                        continue;
                    }

                    string claveMateriaBase =
                        string.Empty;

                    try
                    {
                        if (
                            MainVm != null &&
                            !string.IsNullOrWhiteSpace(
                                MainVm.ArchivoCompletoActual))
                        {
                            claveMateriaBase =
                                Path.GetFileNameWithoutExtension(
                                        MainVm.ArchivoCompletoActual)?
                                    .Trim()
                                    .Replace(
                                        ' ',
                                        '_')
                                ?? string.Empty;
                        }
                    }
                    catch
                    {
                        claveMateriaBase =
                            string.Empty;
                    }

                    bool tienePre =
                        _preService != null &&
                        !string.IsNullOrWhiteSpace(
                            claveMateriaBase) &&
                        AlumnoSeleccionado != null &&
                        _preService
                            .ObtenerEstadoPre(
                                claveMateriaBase,
                                AlumnoSeleccionado.Matricula)
                            .TienePRE;

                    if (tienePre)
                    {
                        decimal d =
                            (decimal)valor;

                        ed.EstablecerPuntajeDesdeCarga(
                            TruncarUnDecimal(d)
                                .ToString(
                                    "0.0",
                                    CultureInfo.InvariantCulture));

                        ed.SetBloqueadoPorPre(
                            true);

                        PreActivo = true;
                    }
                    else
                    {
                        ed.EstablecerPuntajeDesdeCarga(
                            valor.ToString(
                                "0.##",
                                CultureInfo.InvariantCulture));

                        ed.SetBloqueadoPorPre(
                            false);
                    }
                }
                else
                {
                    ed.EstablecerPuntajeDesdeCarga(
                        ed.Activa
                            ? "SC"
                            : string.Empty);

                    ed.SetBloqueadoPorPre(
                        false);
                }
            }

            Inasistencias =
                capturas.TryGetValue(
                    "__Inasistencias__",
                    out var ina) &&
                ina >= 0
                    ? ina.ToString()
                    : string.Empty;

            if (AlumnoConCapturaDirecta)
            {
                if (
                    capturas.TryGetValue(
                        "__CALIF_DIRECTA__",
                        out var califDirectaVal))
                {
                    CalificacionParcialTexto =
                        califDirectaVal.ToString(
                            "0.##",
                            CultureInfo.InvariantCulture);
                }
                else
                {
                    try
                    {
                        CalificacionParcialTexto =
                            AlumnoSeleccionado
                                .Calificación[
                                    _evaluacionActual]
                            ?? string.Empty;
                    }
                    catch
                    {
                        CalificacionParcialTexto =
                            string.Empty;
                    }
                }
            }
            else
            {
                CalificacionParcialTexto =
                    AlumnoSeleccionado
                        .Calificación[
                            _evaluacionActual]
                    ?? string.Empty;
            }

            return;
        }

        // ================================================================
        // EL ALUMNO NO TENÍA CAPTURAS GUARDADAS
        // ================================================================

        foreach (var actividad in Actividades)
        {
            actividad.EstablecerPuntajeDesdeCarga(
                actividad.Activa
                    ? "SC"
                    : string.Empty);

            actividad.SetBloqueadoPorPre(
                false);
        }

        Inasistencias =
            string.Empty;

        AlumnoConCapturaDirecta =
            false;

        LeyendaCapturaDirecta =
            string.Empty;

        CalificacionParcialTexto =
            AlumnoSeleccionado
                .Calificación[
                    _evaluacionActual]
            ?? string.Empty;

        PreActivo = false;

        foreach (var ed in Actividades)
        {
            ed.SetBloqueadoPorCapturaDirecta(
                false);
        }
    }

    private void NormalizarActividadesEnMateria()
    {
        _materia.Actividades =
            (_materia.Actividades ??
             new List<ActividadParcial>())
            .Take(4)
            .ToList();

        while (
            _materia.Actividades.Count <
            4)
        {
            _materia.Actividades.Add(
                new ActividadParcial
                {
                    Activa = false,
                    Nombre = string.Empty,
                    Porcentaje = 0.0,
                    PuntajeMaximo = 0.0
                });
        }

        _materia.Actividades[0].Activa =
            true;
    }

    private void ReconstruirActividadesEnPantalla()
    {
        Actividades.Clear();

        for (
            int i = 0;
            i < _materia.Actividades.Count;
            i++)
        {
            var modelo =
                _materia.Actividades[i];

            var editor =
                new ActividadParcialEditor(
                    () => EditorChanged(),
                    i + 1,
                    i == 0);

            editor.CargarDesdeModelo(
                modelo);

            Actividades.Add(editor);
        }

        while (Actividades.Count < 4)
        {
            int numero =
                Actividades.Count + 1;

            bool esActividad1 =
                numero == 1;

            Actividades.Add(
                CreateBlankEditor(
                    numero,
                    esActividad1));
        }

        if (Actividades.Count > 0)
            Actividades[0].Activa = true;
    }

    private ActividadParcialEditor CreateBlankEditor(
        int numeroActividad,
        bool esActividad1 = false)
    {
        var ed =
            new ActividadParcialEditor(
                () => EditorChanged(),
                numeroActividad,
                esActividad1);

        ed.Activa =
            esActividad1;

        ed.Nombre =
            string.Empty;

        ed.Porcentaje =
            string.Empty;

        ed.PuntajeMaximo =
            string.Empty;

        ed.EstablecerPuntajeDesdeCarga(
            esActividad1
                ? "SC"
                : string.Empty);

        return ed;
    }

    private void PersistirCapturasTemporales(
        string? matricula)
    {
        if (
            string.IsNullOrWhiteSpace(matricula) ||
            matricula == "$CONFIG$")
        {
            return;
        }

        var capturas =
            new Dictionary<string, double>(
                StringComparer.OrdinalIgnoreCase);

        // ============================================================
        // CAPTURA DIRECTA
        // ============================================================

        capturas[
                "__CAPTURA_DIRECTA__"] =
            AlumnoConCapturaDirecta
                ? 1.0
                : 0.0;

        if (AlumnoConCapturaDirecta)
        {
            if (
                double.TryParse(
                    CalificacionParcialTexto,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double dval))
            {
                capturas[
                        "__CALIF_DIRECTA__"] =
                    dval;
            }
        }

        // ============================================================
        // ACTIVIDADES
        // ============================================================

        foreach (var actividad in Actividades)
        {
            if (string.IsNullOrWhiteSpace(
                    actividad.Nombre))
            {
                continue;
            }

            string nombreActividad =
                actividad.Nombre.Trim();

            string valorTexto =
                actividad.PuntajeObtenido?
                    .Trim() ??
                string.Empty;

            // --------------------------------------------------------
            // SC = -1
            // --------------------------------------------------------

            if (
                string.Equals(
                    valorTexto,
                    "SC",
                    StringComparison.OrdinalIgnoreCase))
            {
                capturas[nombreActividad] = -1;
                continue;
            }

            // --------------------------------------------------------
            // VACÍO = NO SE GUARDA
            //
            // IMPORTANTE:
            // No convertimos aquí vacío a SC.
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    valorTexto))
            {
                continue;
            }

            // --------------------------------------------------------
            // CALIFICACIÓN NUMÉRICA
            // --------------------------------------------------------

            if (
                double.TryParse(
                    valorTexto,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double obt))
            {
                capturas[nombreActividad] =
                    obt;

                continue;
            }

            // --------------------------------------------------------
            // TEXTO DESCONOCIDO
            // --------------------------------------------------------

            continue;
        }

        // ============================================================
        // INASISTENCIAS
        // ============================================================

        if (
            int.TryParse(
                Inasistencias,
                out int inaVal))
        {
            capturas[
                    "__Inasistencias__"] =
                inaVal;
        }
        else
        {
            capturas[
                    "__Inasistencias__"] =
                -1;
        }

        // ============================================================
        // CONFIGURACIÓN DE ASISTENCIA
        // ============================================================

        if (
            int.TryParse(
                ClasesTotales,
                out int ct))
        {
            _materia.Calificaciones[
                    "$CONFIG$"] =
                new Dictionary<string, double>
                {
                    {
                        "AsistenciaActiva",
                        AsistenciaActiva
                            ? 1
                            : 0
                    },
                    {
                        "ClasesTotales",
                        ct
                    }
                };
        }
        else
        {
            _materia.Calificaciones[
                    "$CONFIG$"] =
                new Dictionary<string, double>
                {
                    {
                        "AsistenciaActiva",
                        AsistenciaActiva
                            ? 1
                            : 0
                    },
                    {
                        "ClasesTotales",
                        -1
                    }
                };
        }

        // ============================================================
        // GUARDAR CAPTURAS DEL ALUMNO
        // ============================================================

        _materia.Calificaciones[
                matricula] =
            capturas;
    }

    // ============================================================
    // COMPROBAR SI ALGÚN PUNTAJE DE ESTA ACTIVIDAD
    // SUPERA EL NUEVO PUNTAJE MÁXIMO
    // ============================================================

    public bool ExistePuntajeFueraDeLimite(
        ActividadParcialEditor actividad,
        double nuevoMaximo)
    {
        if (_cargando ||
            _cargasActivas > 0)
        {
            return false;
        }

        if (actividad == null)
            return false;

        string nombreActividad =
            actividad.Nombre?
                .Trim() ??
            string.Empty;

        if (string.IsNullOrWhiteSpace(
                nombreActividad))
        {
            return false;
        }

        foreach (
            var entrada
            in _materia.Calificaciones)
        {
            if (
                string.Equals(
                    entrada.Key,
                    "$CONFIG$",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var capturas =
                entrada.Value;

            if (
                !capturas.TryGetValue(
                    nombreActividad,
                    out double puntaje))
            {
                continue;
            }

            if (puntaje < 0)
                continue;

            if (puntaje > nuevoMaximo)
                return true;
        }

        return false;
    }

    // ============================================================
    // ELIMINAR LOS PUNTAJES OBTENIDOS DE UNA SOLA ACTIVIDAD
    // ============================================================

    public void EliminarPuntajesDeActividad(
        ActividadParcialEditor actividad)
    {
        if (actividad == null)
            return;

        string nombreActividad =
            actividad.Nombre?
                .Trim() ??
            string.Empty;

        if (string.IsNullOrWhiteSpace(
                nombreActividad))
        {
            return;
        }

        foreach (
            var entrada
            in _materia.Calificaciones)
        {
            if (
                string.Equals(
                    entrada.Key,
                    "$CONFIG$",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            entrada.Value.Remove(
                nombreActividad);
        }

        actividad.EstablecerPuntajeDesdeCarga(
            actividad.Activa
                ? "SC"
                : string.Empty);

        // ============================================================
        // LA CAPTURA DE ESTA ACTIVIDAD YA NO ES VÁLIDA PARA
        // EL ALUMNO MOSTRADO HASTA QUE VUELVA A CAPTURARSE.
        //
        // EL RECÁLCULO INMEDIATO DETERMINA EL ESTADO CORRECTO
        // DE LA CALIFICACIÓN VISIBLE.
        // ============================================================

        CalificacionParcialTexto =
            string.Empty;

        if (
            AlumnoSeleccionado != null &&
            !string.IsNullOrWhiteSpace(
                _evaluacionActual))
        {
            AlumnoSeleccionado
                .Calificación[
                    _evaluacionActual] =
                string.Empty;
        }

        RecalcularTodo(
            guardarJson: false,
            esCargaInicial: false,
            marcarCambios: true);

        // ============================================================
        // RECALCULAR TODOS LOS ALUMNOS DEL PARCIAL EN MEMORIA.
        //
        // Como la captura de esta actividad fue eliminada de todos,
        // los alumnos que ya no tengan todas las actividades activas
        // capturadas quedan con la calificación vacía.
        // ============================================================

        if (
            ConfiguracionActividadesValidaParaRecalculoMasivo())
        {
            RecalcularCalificacionesDeTodosLosAlumnosDelParcial(
                actualizarUiAlumnoActual: true);
        }
    }

    private void RecalcularTodo(
        bool guardarJson,
        bool esCargaInicial = false,
        bool marcarCambios = true)
    {
        if (_cargasActivas > 0 && !esCargaInicial)
            return;

        decimal sumaPorcentajes = 0m;
        decimal acumulado = 0m;

        bool logicaCorrecta = true;
        bool hayPuntajesNumericos = false;

        var entradas =
            new List<(double porc, double max, double obt)>();

        foreach (var actividad in Actividades)
        {
            if (!actividad.Activa)
                continue;

            if (
                !double.TryParse(
                    actividad.Porcentaje,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double porc) ||
                porc < 0 ||
                porc > 100)
            {
                logicaCorrecta = false;
                continue;
            }

            sumaPorcentajes +=
                (decimal)porc;

            if (
                !double.TryParse(
                    actividad.PuntajeMaximo,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double max) ||
                max <= 0)
            {
                logicaCorrecta = false;
                continue;
            }

            // ========================================================
            // TODAS LAS ACTIVIDADES ACTIVAS DEBEN TENER CAPTURA
            //
            // SC = CAPTURA VÁLIDA
            // ========================================================

            if (
                string.IsNullOrWhiteSpace(
                    actividad.PuntajeObtenido))
            {
                logicaCorrecta = false;
                continue;
            }

            string puntajeTexto =
                actividad.PuntajeObtenido.Trim();

            // ========================================================
            // SC:
            // NO INVALIDA LA CALIFICACIÓN.
            // NO APORTA PUNTOS.
            // ========================================================

            if (
                puntajeTexto.Equals(
                    "SC",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (
                !double.TryParse(
                    puntajeTexto,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double obt) ||
                obt < 0 ||
                obt > max)
            {
                logicaCorrecta = false;
                continue;
            }

            hayPuntajesNumericos =
                true;

            entradas.Add(
                (porc, max, obt));
        }

        double scaling =
            1.0;

        if (sumaPorcentajes > 0)
        {
            scaling =
                100.0 /
                (double)sumaPorcentajes;
        }

        foreach (
            var (porc, max, obt)
            in entradas)
        {
            double porcNorm =
                porc *
                scaling;

            acumulado +=
                ((decimal)obt /
                 (decimal)max) *
                (decimal)porcNorm;
        }

        SumaPorcentajes =
            sumaPorcentajes;

        SumaPorcentajesTexto =
            $"{TruncarUnDecimal(sumaPorcentajes):0.0}%";

        if (sumaPorcentajes > 100m)
        {
            PorcentajeEstado = "Over";
        }
        else if (sumaPorcentajes < 100m)
        {
            PorcentajeEstado = "Under";
        }
        else
        {
            PorcentajeEstado = "Ok";
        }

        SumaValida =
            sumaPorcentajes == 100m;

        // ============================================================
        // CALIFICACIÓN DEL ALUMNO ACTUAL
        // ============================================================

        if (
            sumaPorcentajes > 0m &&
            logicaCorrecta &&
            hayPuntajesNumericos)
        {
            decimal calificacion =
                TruncarUnDecimal(
                    acumulado / 10m);

            if (!AlumnoConCapturaDirecta)
            {
                CalificacionParcialTexto =
                    calificacion.ToString(
                        "0.0",
                        CultureInfo.InvariantCulture);

                if (
                    AlumnoSeleccionado != null &&
                    !string.IsNullOrWhiteSpace(
                        _evaluacionActual))
                {
                    AlumnoSeleccionado
                            .Calificación[
                                _evaluacionActual] =
                        CalificacionParcialTexto;
                }
            }
        }
        else
        {
            if (!AlumnoConCapturaDirecta)
            {
                if (!esCargaInicial)
                {
                    CalificacionParcialTexto =
                        string.Empty;

                    if (
                        AlumnoSeleccionado != null &&
                        !string.IsNullOrWhiteSpace(
                            _evaluacionActual))
                    {
                        AlumnoSeleccionado
                                .Calificación[
                                    _evaluacionActual] =
                            string.Empty;
                    }
                }
                else
                {
                    if (
                        AlumnoSeleccionado != null &&
                        !string.IsNullOrWhiteSpace(
                            _evaluacionActual))
                    {
                        CalificacionParcialTexto =
                            AlumnoSeleccionado
                                .Calificación[
                                    _evaluacionActual]
                            ?? string.Empty;
                    }
                }
            }
        }

        // ============================================================
        // PERSISTIR EN MEMORIA LA CAPTURA ACTUAL
        // ============================================================

        if (AlumnoSeleccionado != null)
        {
            PersistirCapturasTemporales(
                AlumnoSeleccionado.Matricula);
        }

        // ============================================================
        // GUARDADO FÍSICO
        // ============================================================

        if (guardarJson)
        {
            GuardarEnJsonLocal();
        }

        ActualizarConteoEvaluados();

        if (
            !guardarJson &&
            marcarCambios &&
            _cargasActivas == 0 &&
            !_suspendUserEditMarking)
        {
            TieneCambios = true;

            _lastUserEditTime =
                DateTime.UtcNow;
        }
    }

    private void GuardarEnJsonLocal()
    {
        if (
            string.IsNullOrWhiteSpace(
                _claveMateria) ||
            string.IsNullOrWhiteSpace(
                _evaluacionActual))
        {
            return;
        }

        int clasesT = -1;

        if (
            int.TryParse(
                ClasesTotales,
                out int parsedCt))
        {
            clasesT =
                parsedCt;
        }

        var config =
            new Dictionary<string, double>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["AsistenciaActiva"] =
                    AsistenciaActiva
                        ? 1
                        : 0,

                ["ClasesTotales"] =
                    clasesT
            };

        _materia.Calificaciones[
                "$CONFIG$"] =
            config;

        if (
            AlumnoSeleccionado != null &&
            !string.IsNullOrWhiteSpace(
                AlumnoSeleccionado.Matricula))
        {
            PersistirCapturasTemporales(
                AlumnoSeleccionado.Matricula);
        }

        double acumuladoPorcentajes =
            0.0;

        foreach (var ed in Actividades)
        {
            if (!ed.Activa)
                continue;

            var text =
                (ed.Porcentaje ??
                 string.Empty)
                .Trim()
                .Replace(
                    ',',
                    '.');

            if (
                double.TryParse(
                    text,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double p))
            {
                acumuladoPorcentajes +=
                    p;
            }
        }

        _materia.PorcentajeAcumulado =
            acumuladoPorcentajes;

        _materia.Actividades =
            Actividades
                .Select(a => a.ToModelo())
                .ToList();

        string claveMateriaEval =
            $"{_claveMateria}_{_evaluacionActual}";

        _parcialJsonService.GuardarMateria(
            claveMateriaEval,
            _materia);

        TieneCambios = false;

        _lastUserEditTime =
            null;

        _lastArchivoSeleccionado =
            _mainVm.ArchivoSeleccionado;

        _lastEvaluacionSeleccionada =
            _mainVm.EvaluacionSeleccionada;

        _lastLoadOrSaveTime =
            DateTime.UtcNow;
    }

    private static string ObtenerClaveMateriaDesdeNombreVisual(
        string? nombreVisual)
    {
        if (
            string.IsNullOrWhiteSpace(
                nombreVisual))
        {
            return string.Empty;
        }

        string texto =
            nombreVisual.Trim();

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

        int firstSpace =
            texto.IndexOf(' ');

        if (firstSpace <= 0)
        {
            return texto.Replace(
                ' ',
                '_');
        }

        string clave =
            texto[..firstSpace]
                .Trim();

        string nombre =
            texto[
                    (firstSpace + 1)..]
                .Trim();

        return string.IsNullOrWhiteSpace(nombre)
            ? clave
            : $"{clave}_{nombre}";
    }

    private static string ObtenerClaveMateriaDesdeNombreArchivo(
        string rutaCompleta)
    {
        try
        {
            var nombre =
                Path.GetFileNameWithoutExtension(
                    rutaCompleta);

            if (
                string.IsNullOrWhiteSpace(
                    nombre))
            {
                return string.Empty;
            }

            return nombre
                .Trim()
                .Replace(
                    ' ',
                    '_');
        }
        catch
        {
            return string.Empty;
        }
    }

    private string ObtenerGrupoDesdeJson(
        string matricula)
    {
        if (
            string.IsNullOrWhiteSpace(
                matricula))
        {
            return string.Empty;
        }

        return _mapaGrupos.TryGetValue(
            matricula.Trim(),
            out var grupo)
            ? grupo
            : string.Empty;
    }

    private void CargarMapaGrupos()
    {
        _mapaGrupos.Clear();

        try
        {
            using var lite =
                new SqliteService();

            var grupos =
                lite.GetGrupos();

            foreach (var kv in grupos)
            {
                _mapaGrupos[
                        kv.Key] =
                    kv.Value;
            }
        }
        catch
        {
        }
    }

    private static decimal TruncarUnDecimal(
        decimal valor)
    {
        return Math.Truncate(
            valor * 10m) / 10m;
    }

    [RelayCommand]
    private void Inicio()
    {
        if (Alumnos.Any())
        {
            AlumnoSeleccionado =
                Alumnos.First();
        }
    }

    [RelayCommand]
    private void Anterior()
    {
        if (
            AlumnoSeleccionado != null &&
            Alumnos.Any())
        {
            int i =
                Alumnos.IndexOf(
                    AlumnoSeleccionado);

            if (i > 0)
            {
                AlumnoSeleccionado =
                    Alumnos[i - 1];
            }
        }
    }

    [RelayCommand]
    private void Siguiente()
    {
        if (
            AlumnoSeleccionado != null &&
            Alumnos.Any())
        {
            int i =
                Alumnos.IndexOf(
                    AlumnoSeleccionado);

            if (
                i >= 0 &&
                i < Alumnos.Count - 1)
            {
                AlumnoSeleccionado =
                    Alumnos[i + 1];
            }
        }
    }

    [RelayCommand]
    private void Final()
    {
        if (Alumnos.Any())
        {
            AlumnoSeleccionado =
                Alumnos.Last();
        }
    }
}

public partial class ActividadParcialEditor : ObservableObject
{
    private readonly Action _notificarCambio;

    private bool _bloqueadoPorCapturaDirecta =
        false;

    private bool _bloqueadoPorPre =
        false;

    private bool _cargandoPuntaje =
        false;

    [ObservableProperty] private bool _estaCompleta;

    [ObservableProperty] private bool _activa;

    [ObservableProperty] private string _nombre =
        string.Empty;

    [ObservableProperty] private string _porcentaje =
        string.Empty;

    [ObservableProperty] private string _puntajeMaximo =
        string.Empty;

    [ObservableProperty] private string _puntajeObtenido =
        string.Empty;

    [ObservableProperty] private int _numeroActividad;

    public bool EsActividad1 { get; }

    public string TextoActividad =>
        $"ACTIVIDAD {NumeroActividad}";

    public string NombreParaPuntajes =>
        Activa
            ? Nombre
            : string.Empty;

    public ActividadParcialEditor(
        Action notificarCambio,
        int numeroActividad,
        bool esActividad1 = false)
    {
        _notificarCambio =
            notificarCambio;

        NumeroActividad =
            numeroActividad;

        EsActividad1 =
            esActividad1;

        if (EsActividad1)
            _activa = true;
    }

    public void EstablecerNumeroActividad(
        int numero)
    {
        NumeroActividad =
            numero;

        OnPropertyChanged(
            nameof(TextoActividad));
    }

    public void SetBloqueadoPorCapturaDirecta(
        bool bloqueado)
    {
        _bloqueadoPorCapturaDirecta =
            bloqueado;

        OnPropertyChanged(
            nameof(IsPuntajeEditable));

        OnPropertyChanged(
            nameof(IsPuntajeEditableFinal));
    }

    public void SetBloqueadoPorPre(
        bool bloqueado)
    {
        _bloqueadoPorPre =
            bloqueado;

        OnPropertyChanged(
            nameof(IsPuntajeEditable));

        OnPropertyChanged(
            nameof(IsPuntajeEditableFinal));
    }

    public void EstablecerPuntajeDesdeCarga(
        string? valor)
    {
        _cargandoPuntaje = true;

        try
        {
            PuntajeObtenido =
                valor ?? string.Empty;
        }
        finally
        {
            _cargandoPuntaje = false;
        }

        OnPropertyChanged(
            nameof(DisplayPuntajeObtenido));

        OnPropertyChanged(
            nameof(FraccionTexto));

        OnPropertyChanged(
            nameof(ContribucionTexto));
    }

    public void RefrescarVista()
    {
        OnPropertyChanged(
            nameof(TextoActividad));

        OnPropertyChanged(
            nameof(NombreParaPuntajes));

        OnPropertyChanged(
            nameof(FraccionTexto));

        OnPropertyChanged(
            nameof(ContribucionTexto));

        OnPropertyChanged(
            nameof(IsPuntajeEditable));

        OnPropertyChanged(
            nameof(IsPuntajeEditableFinal));
    }

    public bool IsPuntajeEditable =>
        !_bloqueadoPorCapturaDirecta &&
        !_bloqueadoPorPre &&
        Activa;

    public bool IsPuntajeEditableFinal =>
        !_bloqueadoPorCapturaDirecta &&
        !_bloqueadoPorPre &&
        Activa &&
        EstaCompleta;

    partial void OnActivaChanged(
        bool value)
    {
        if (EsActividad1 && !value)
        {
            _activa = true;

            OnPropertyChanged(
                nameof(Activa));

            RefrescarVista();

            return;
        }

        // ============================================================
        // AL ACTIVAR UNA ACTIVIDAD SIN PUNTAJE, INICIALIZAR EN SC
        // ============================================================

        if (
            value &&
            !_cargandoPuntaje &&
            string.IsNullOrWhiteSpace(
                PuntajeObtenido))
        {
            EstablecerPuntajeDesdeCarga(
                "SC");
        }

        RefrescarVista();

        NotificarActualizacion();
    }

    partial void OnNombreChanged(
        string value)
    {
        OnPropertyChanged(
            nameof(NombreParaPuntajes));

        _notificarCambio?.Invoke();
    }

    partial void OnPorcentajeChanged(
        string value)
    {
        NotificarActualizacion();
    }

    partial void OnPuntajeMaximoChanged(
        string value)
    {
        NotificarActualizacion();
    }

    partial void OnPuntajeObtenidoChanged(
        string value)
    {
        // ============================================================
        // AQUÍ YA NO SE CONVIERTE VACÍO EN SC.
        // ============================================================

        if (_cargandoPuntaje)
        {
            return;
        }

        NotificarActualizacion();

        OnPropertyChanged(
            nameof(DisplayPuntajeObtenido));
    }

    partial void OnNumeroActividadChanged(
        int value)
    {
        OnPropertyChanged(
            nameof(TextoActividad));
    }

    private void NotificarActualizacion()
    {
        ActualizarVistaInmediata();

        ActualizarEstadoCompleto();

        _notificarCambio?.Invoke();
    }

    private void ActualizarVistaInmediata()
    {
        OnPropertyChanged(
            nameof(FraccionTexto));

        OnPropertyChanged(
            nameof(ContribucionTexto));

        OnPropertyChanged(
            nameof(NombreParaPuntajes));
    }

    private void ActualizarEstadoCompleto()
    {
        bool completo =
            !string.IsNullOrWhiteSpace(
                Nombre) &&
            !string.IsNullOrWhiteSpace(
                Porcentaje) &&
            !string.IsNullOrWhiteSpace(
                PuntajeMaximo);

        if (EstaCompleta != completo)
        {
            EstaCompleta =
                completo;

            OnPropertyChanged(
                nameof(IsPuntajeEditable));

            OnPropertyChanged(
                nameof(IsPuntajeEditableFinal));
        }
    }

    public void CargarDesdeModelo(
        ActividadParcial modelo)
    {
        Activa =
            EsActividad1 ||
            modelo.Activa;

        Nombre =
            modelo.Nombre;

        Porcentaje =
            modelo.Porcentaje == 0
                ? string.Empty
                : modelo.Porcentaje.ToString(
                    CultureInfo.InvariantCulture);

        PuntajeMaximo =
            modelo.PuntajeMaximo == 0
                ? string.Empty
                : modelo.PuntajeMaximo.ToString(
                    CultureInfo.InvariantCulture);

        EstablecerPuntajeDesdeCarga(
            string.Empty);

        RefrescarVista();

        ActualizarEstadoCompleto();
    }

    public ActividadParcial ToModelo()
    {
        double.TryParse(
            Porcentaje,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out double porc);

        double.TryParse(
            PuntajeMaximo,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out double max);

        return new ActividadParcial
        {
            Activa =
                EsActividad1 ||
                Activa,

            Nombre =
                Nombre?.Trim() ??
                string.Empty,

            Porcentaje =
                porc,

            PuntajeMaximo =
                max
        };
    }

    public string FraccionTexto
    {
        get
        {
            if (!Activa)
                return string.Empty;

            string obtRaw =
                string.IsNullOrWhiteSpace(
                    PuntajeObtenido)
                    ? string.Empty
                    : PuntajeObtenido;

            string maxRaw =
                string.IsNullOrWhiteSpace(
                    PuntajeMaximo)
                    ? string.Empty
                    : PuntajeMaximo;

            if (
                string.IsNullOrEmpty(
                    obtRaw) &&
                string.IsNullOrEmpty(
                    maxRaw))
            {
                return string.Empty;
            }

            string obtDisplay;

            if (
                string.Equals(
                    obtRaw,
                    "SC",
                    StringComparison.OrdinalIgnoreCase))
            {
                obtDisplay =
                    "SC";
            }
            else if (
                double.TryParse(
                    obtRaw,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double obtVal))
            {
                if (_bloqueadoPorPre)
                {
                    obtDisplay =
                        Services.NumberUtils
                            .ToSmartString(
                                obtVal);
                }
                else
                {
                    obtDisplay =
                        obtVal.ToString(
                            "0.##",
                            CultureInfo.InvariantCulture);
                }
            }
            else
            {
                obtDisplay =
                    obtRaw;
            }

            string maxDisplay;

            if (
                double.TryParse(
                    maxRaw,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double maxVal))
            {
                maxDisplay =
                    maxVal.ToString(
                        "0.##",
                        CultureInfo.InvariantCulture);
            }
            else
            {
                maxDisplay =
                    maxRaw;
            }

            if (
                string.IsNullOrEmpty(
                    obtDisplay) &&
                string.IsNullOrEmpty(
                    maxDisplay))
            {
                return string.Empty;
            }

            return
                $"{obtDisplay} / {maxDisplay}";
        }
    }

    public string ContribucionTexto
    {
        get
        {
            if (!Activa)
                return string.Empty;

            if (
                !double.TryParse(
                    PuntajeMaximo,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double max) ||
                max <= 0)
            {
                return "0.0";
            }

            if (
                !double.TryParse(
                    PuntajeObtenido,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double obt) ||
                obt < 0)
            {
                return "0.0";
            }

            if (
                !double.TryParse(
                    Porcentaje,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double porc))
            {
                return "0.0";
            }

            decimal contribucion =
                ((decimal)obt /
                 (decimal)max) *
                (decimal)porc;

            return
            (
                Math.Truncate(
                    contribucion * 10m) /
                10m
            ).ToString(
                "0.0",
                CultureInfo.InvariantCulture);
        }
    }

    public string DisplayPuntajeObtenido
    {
        get =>
            GetDisplayPuntaje(
                PuntajeObtenido);

        set
        {
            PuntajeObtenido =
                value;

            OnPropertyChanged(
                nameof(DisplayPuntajeObtenido));
        }
    }

    private string GetDisplayPuntaje(
        string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        if (
            string.Equals(
                raw,
                "SC",
                StringComparison.OrdinalIgnoreCase))
        {
            return raw;
        }

        string texto =
            raw.Trim();

        if (texto.EndsWith(
                ".",
                StringComparison.Ordinal))
        {
            return texto;
        }

        if (
            double.TryParse(
                texto,
                System.Globalization.NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out double d))
        {
            return Services.NumberUtils
                .ToSmartString(d);
        }

        return raw;
    }
}