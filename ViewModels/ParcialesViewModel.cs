using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
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
    private readonly Dictionary<string, string> _mapaGrupos = new(StringComparer.OrdinalIgnoreCase);

    private MateriaParcial _materia = new();
    private bool _cargando;
    private int _cargasActivas = 0; 
    private string _claveMateria = string.Empty;
    private string _evaluacionActual = string.Empty;
    private string? _ultimaMatriculaSeleccionada;
    [ObservableProperty]
    private bool _tieneCambios;
    private string? _lastArchivoSeleccionado;
    private string? _lastEvaluacionSeleccionada;
    private bool _isReady = false;
    private bool _suspendUserEditMarking = false;
    private DateTime? _lastUserEditTime;
    private DateTime _lastLoadOrSaveTime = DateTime.MinValue;

    public ObservableCollection<Alumno> Alumnos => _mainVm.Alumnos;
    public ObservableCollection<ActividadParcialEditor> Actividades { get; } = new();
    public MainViewModel MainVm => _mainVm;

    [ObservableProperty] private Alumno? _alumnoSeleccionado;
    [ObservableProperty] private string _nombreMateria = string.Empty;
    [ObservableProperty] private string _nombreEvaluacion = string.Empty;
    [ObservableProperty] private string _nombreAlumno = string.Empty;
    [ObservableProperty] private string _matriculaAlumno = string.Empty;
    [ObservableProperty] private string _grupoAlumno = string.Empty;
    [ObservableProperty] private bool _mostrarGrupo = true;
    [ObservableProperty] private decimal _sumaPorcentajes;
    [ObservableProperty] private string _sumaPorcentajesTexto = "0%";
    [ObservableProperty] private string _porcentajeEstado = "Ok";
    [ObservableProperty] private bool _sumaValida = false;
    [ObservableProperty] private string _calificacionParcialTexto = "";
    [ObservableProperty] private string _estadoValidacion = "Sin cargar";
    [ObservableProperty] private string _estadoGuardado = string.Empty;

    [ObservableProperty] private bool _asistenciaActiva;
    [ObservableProperty] private string _clasesTotales = "";
    [ObservableProperty] private string _inasistencias = "";
    [ObservableProperty] private bool _alumnoConCapturaDirecta;
    [ObservableProperty] private bool _capturaDirectaActiva;
    [ObservableProperty] private string _leyendaCapturaDirecta = string.Empty;

    [ObservableProperty] private string _textoEvaluados = "";
    [ObservableProperty] private bool _faltanPorEvaluar = false;
    public List<AlumnoFaltante> ListaNoEvaluados { get; private set; } = new();

    public ParcialesViewModel(MainViewModel mainVm)
    {
        _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
        _parcialJsonService = new ParcialJsonService();

        CargarMapaGrupos();
        _mainVm.PropertyChanged += MainVm_PropertyChanged;

        if (_mainVm.Alumnos.Any())
        {
            _ultimaMatriculaSeleccionada = _mainVm.Alumnos.First().Matricula;
            AlumnoSeleccionado = _mainVm.Alumnos.First();
        }

        CargarContextoActual();
    }

    public void ActualizarConteoEvaluados()
    {
        if (_cargando || string.IsNullOrWhiteSpace(_evaluacionActual)) return;
        
        int total = Alumnos.Count;
        int evaluados = 0;
        var lista = new List<AlumnoFaltante>();

        string materiaMostrada = NombreMateria;
        int sepIdx = materiaMostrada.IndexOf(" - Grupo:");
        if (sepIdx > 0) materiaMostrada = materiaMostrada.Substring(0, sepIdx).Trim();

        foreach (var alumno in Alumnos)
        {
            bool hasCalif = !string.IsNullOrWhiteSpace(alumno.Calificación[_evaluacionActual]);
            
            bool hasFaltas = true;
            if (AsistenciaActiva)
            {
                if (_materia.Calificaciones.TryGetValue(alumno.Matricula, out var caps))
                {
                    if (!caps.TryGetValue("__Inasistencias__", out var f) || f < 0)
                        hasFaltas = false;
                }
                else
                {
                    hasFaltas = false;
                }
            }

            if (hasCalif && hasFaltas)
            {
                evaluados++;
            }
            else
            {
                string razon = !hasCalif && !hasFaltas ? "Falta calificación e inasistencias" :
                               (!hasCalif ? "Falta calificación" : "Falta registrar inasistencias");
                
                string gp = ObtenerGrupoDesdeJson(alumno.Matricula) ?? alumno.Grupo ?? "S/G";

                lista.Add(new AlumnoFaltante { 
                    Materia = materiaMostrada,
                    Grupo = gp,
                    Matricula = alumno.Matricula, 
                    Nombre = alumno.Nombre, 
                    Razon = razon 
                });
            }
        }

        TextoEvaluados = $"{evaluados} de {total}";
        FaltanPorEvaluar = evaluados < total;
        ListaNoEvaluados = lista;
    }

    private void EditorChanged()
    {
        RecalcularTodo(guardarJson: false);
    }

    partial void OnAsistenciaActivaChanged(bool value)
    {
        RecalcularTodo(guardarJson: false);
    }

    partial void OnClasesTotalesChanged(string value)
    {
        RecalcularTodo(guardarJson: false);
    }

    partial void OnInasistenciasChanged(string value)
    {
        RecalcularTodo(guardarJson: false);
    }

    partial void OnCalificacionParcialTextoChanged(string value)
    {
        if (_cargando || _cargasActivas > 0) return; 
        if (AlumnoSeleccionado == null || string.IsNullOrWhiteSpace(_evaluacionActual)) return;

        if (!AlumnoConCapturaDirecta) return;

        try
        {
            AlumnoSeleccionado.Calificación[_evaluacionActual] = value ?? string.Empty;
            
            PersistirCapturasTemporales(AlumnoSeleccionado.Matricula);
            MarkUserEdited(); 
            ActualizarConteoEvaluados();
        }
        catch { }
    }

    partial void OnAlumnoConCapturaDirectaChanged(bool value)
    {
        if (_cargando || _cargasActivas > 0) return; 

        LeyendaCapturaDirecta = value ? "Calificación directa habilitada para este alumno — Esta función es para casos especiales." : string.Empty;

        foreach (var ed in Actividades)
        {
            ed.SetBloqueadoPorCapturaDirecta(value);
        }

        if (AlumnoSeleccionado != null)
        {
            PersistirCapturasTemporales(AlumnoSeleccionado.Matricula);
            MarkUserEdited();
        }

        if (!value)
        {
            RecalcularTodo(guardarJson: false, esCargaInicial: false, marcarCambios: false);
        }
    }

    public void MarkUserEdited()
    {
        if (_cargasActivas > 0 || _cargando || _suspendUserEditMarking)
            return;

        TieneCambios = true;
        _lastUserEditTime = DateTime.UtcNow;
    }

    [RelayCommand]
    private void AgregarActividad()
    {
        if (Actividades.Count >= 4) return;

        var nueva = new ActividadParcialEditor(() => EditorChanged());
        nueva.Activa = false;
        nueva.Nombre = string.Empty;
        nueva.Porcentaje = string.Empty;
        nueva.PuntajeMaximo = string.Empty;

        Actividades.Add(nueva);
        _materia.Actividades = Actividades.Select(a => a.ToModelo()).ToList();
        RecalcularTodo(guardarJson: false);
    }

    [RelayCommand]
    private void QuitarActividad(ActividadParcialEditor editor)
    {
        if (editor == null) return;
        if (Actividades.Contains(editor))
        {
            Actividades.Remove(editor);
            _materia.Actividades = Actividades.Select(a => a.ToModelo()).ToList();
            RecalcularTodo(guardarJson: false);
        }
    }

    public void PrepararGuardado()
    {
        RecalcularTodo(guardarJson: true);
    }

    private void MainVm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.ArchivoSeleccionado) ||
            e.PropertyName == nameof(MainViewModel.EvaluacionSeleccionada))
        {
            try
            {
                var main = sender as MainViewModel;
                string? nuevoArchivo = main?.ArchivoSeleccionado;
                string? nuevaEval = main?.EvaluacionSeleccionada;

                if (!_isReady)
                {
                    _lastArchivoSeleccionado = nuevoArchivo;
                    _lastEvaluacionSeleccionada = nuevaEval;
                    _isReady = true;
                    return;
                }

                bool archivoCambio = !string.Equals(nuevoArchivo, _lastArchivoSeleccionado, StringComparison.OrdinalIgnoreCase);
                bool evalCambio = !string.Equals(nuevaEval, _lastEvaluacionSeleccionada, StringComparison.OrdinalIgnoreCase);

                bool userEditedAfterLoad = _lastUserEditTime.HasValue && _lastUserEditTime.Value > _lastLoadOrSaveTime;

                if ((TieneCambios && userEditedAfterLoad) && (archivoCambio || evalCambio))
                {
                    var res = System.Windows.MessageBox.Show("Hay cambios sin guardar. ¿Deseas guardar antes de cambiar de materia/evaluación?\nSí = Guardar, No = Descartar, Cancelar = Volver a la selección previa.",
                        "Cambios sin guardar", System.Windows.MessageBoxButton.YesNoCancel, System.Windows.MessageBoxImage.Warning);

                    if (res == System.Windows.MessageBoxResult.Cancel)
                    {
                        if (archivoCambio && main != null)
                            main.ArchivoSeleccionado = _lastArchivoSeleccionado;
                        if (evalCambio && main != null)
                            main.EvaluacionSeleccionada = _lastEvaluacionSeleccionada;
                        return;
                    }

                    if (res == System.Windows.MessageBoxResult.Yes)
                    {
                        PrepararGuardado();
                        TieneCambios = false;
                        _lastUserEditTime = null;
                    }

                    if (res == System.Windows.MessageBoxResult.No)
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
    }

    partial void OnAlumnoSeleccionadoChanged(Alumno? value)
    {
        if (_cargando || _cargasActivas > 0) return;

        if (!string.IsNullOrWhiteSpace(_ultimaMatriculaSeleccionada))
        {
            PersistirCapturasTemporales(_ultimaMatriculaSeleccionada);
        }

        _ultimaMatriculaSeleccionada = value?.Matricula;

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
            RecalcularTodo(guardarJson: false, esCargaInicial: true, marcarCambios: false);
        }
        finally
        {
            await System.Threading.Tasks.Task.Delay(250);
            _cargasActivas--;
            if (_cargasActivas <= 0)
            {
                _cargasActivas = 0;
                _suspendUserEditMarking = false;
            }
        }
    }

    private string ObtenerNombreEvaluacionVisual(string? eval)
    {
        if (string.IsNullOrWhiteSpace(eval)) return string.Empty;
        return eval.Trim().ToUpperInvariant() switch
        {
            "P1" => "PARCIAL 1",
            "P2" => "PARCIAL 2",
            "P3" => "PARCIAL 3",
            "SEM" => "SEMESTRAL",
            "EXTRA" => "EXTRAORDINARIO/INTER",
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
            NombreMateria = _mainVm.ArchivoSeleccionado ?? string.Empty;
            NombreEvaluacion = ObtenerNombreEvaluacionVisual(_mainVm.EvaluacionSeleccionada);
            _evaluacionActual = _mainVm.EvaluacionSeleccionada ?? string.Empty;

            MostrarGrupo = !string.Equals(_evaluacionActual, "EXTRA", StringComparison.OrdinalIgnoreCase) &&
                           !string.Equals(_evaluacionActual, "SEM", StringComparison.OrdinalIgnoreCase);

            _claveMateria = !string.IsNullOrWhiteSpace(_mainVm.ArchivoCompletoActual)
                ? ObtenerClaveMateriaDesdeNombreArchivo(_mainVm.ArchivoCompletoActual)
                : ObtenerClaveMateriaDesdeNombreVisual(_mainVm.ArchivoSeleccionado);

            if (string.IsNullOrWhiteSpace(_claveMateria) || string.IsNullOrWhiteSpace(_evaluacionActual))
            {
                LimpiarVista();
                return;
            }

            string claveMateriaEval = $"{_claveMateria}_{_evaluacionActual}";
            _materia = _parcialJsonService.ObtenerMateria(claveMateriaEval) ?? new MateriaParcial();

            if (_materia.Calificaciones.TryGetValue("$CONFIG$", out var config))
            {
                AsistenciaActiva = config.TryGetValue("AsistenciaActiva", out var aa) && aa > 0;
                ClasesTotales = config.TryGetValue("ClasesTotales", out var ct) && ct >= 0 ? ct.ToString() : "";
            }
            else
            {
                AsistenciaActiva = false;
                ClasesTotales = "";
            }

            NormalizarActividadesEnMateria();
            ReconstruirActividadesEnPantalla();
            RestaurarAlumnoSeleccionado();
            ActualizarDatosAlumnoSeleccionado();
            CargarCapturasDelAlumnoSeleccionado();
            
            RecalcularTodo(guardarJson: false, esCargaInicial: true, marcarCambios: false);
            
            TieneCambios = false;
            _lastUserEditTime = null;
            _lastArchivoSeleccionado = _mainVm.ArchivoSeleccionado;
            _lastEvaluacionSeleccionada = _mainVm.EvaluacionSeleccionada;
            _lastLoadOrSaveTime = DateTime.UtcNow;
        }
        finally
        {
            await System.Threading.Tasks.Task.Delay(400); 
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
        for (int i = 0; i < 4; i++) Actividades.Add(CreateBlankEditor());
        SumaPorcentajes = 0;
        SumaPorcentajesTexto = "0%";
        CalificacionParcialTexto = "";
        EstadoValidacion = "Sin materia cargada";
        EstadoGuardado = string.Empty;
        NombreAlumno = string.Empty;
        MatriculaAlumno = string.Empty;
        GrupoAlumno = string.Empty;
        MostrarGrupo = false;

        AsistenciaActiva = false;
        ClasesTotales = "";
        Inasistencias = "";

        TieneCambios = false;
        _lastUserEditTime = null;
        ActualizarConteoEvaluados();
    }

    private void RestaurarAlumnoSeleccionado()
    {
        if (!string.IsNullOrWhiteSpace(_ultimaMatriculaSeleccionada))
        {
            var alumno = Alumnos.FirstOrDefault(a => string.Equals(a.Matricula, _ultimaMatriculaSeleccionada, StringComparison.OrdinalIgnoreCase));
            if (alumno != null)
            {
                AlumnoSeleccionado = alumno;
                return;
            }
        }

        if (AlumnoSeleccionado == null && Alumnos.Any())
            AlumnoSeleccionado = Alumnos.First();
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

        NombreAlumno = AlumnoSeleccionado.Nombre;
        MatriculaAlumno = AlumnoSeleccionado.Matricula;
        GrupoAlumno = ObtenerGrupoDesdeJson(AlumnoSeleccionado.Matricula) ?? AlumnoSeleccionado.Grupo;
    }

    private void CargarCapturasDelAlumnoSeleccionado()
    {
        if (AlumnoSeleccionado == null)
        {
            foreach (var actividad in Actividades) actividad.PuntajeObtenido = "";
            Inasistencias = "";
            AlumnoConCapturaDirecta = false;
            LeyendaCapturaDirecta = string.Empty;
            CalificacionParcialTexto = "";
            foreach (var ed in Actividades) ed.SetBloqueadoPorCapturaDirecta(false);
            return;
        }

        if (_materia.Calificaciones.TryGetValue(AlumnoSeleccionado.Matricula, out var capturas))
        {
            bool alumnoCapturaDirecta = capturas.TryGetValue("__CAPTURA_DIRECTA__", out var cdVal) && cdVal > 0;
            
            AlumnoConCapturaDirecta = alumnoCapturaDirecta;
            LeyendaCapturaDirecta = alumnoCapturaDirecta ? "Calificación directa habilitada para este alumno — para cambios diríjase al área de Servicios Escolares." : string.Empty;

            foreach (var ed in Actividades)
            {
                ed.SetBloqueadoPorCapturaDirecta(AlumnoConCapturaDirecta);
                if (!string.IsNullOrWhiteSpace(ed.Nombre) && capturas.TryGetValue(ed.Nombre, out var valor))
                {
                    ed.PuntajeObtenido = valor.ToString("0.##", CultureInfo.InvariantCulture);
                }
                else
                {
                    ed.PuntajeObtenido = "";
                }
            }
            
            Inasistencias = capturas.TryGetValue("__Inasistencias__", out var ina) && ina >= 0 ? ina.ToString() : "";

            if (alumnoCapturaDirecta)
            {
                if (capturas.TryGetValue("__CALIF_DIRECTA__", out var califDirectaVal))
                {
                    CalificacionParcialTexto = califDirectaVal.ToString("0.##", CultureInfo.InvariantCulture);
                }
                else
                {
                    try
                    {
                        CalificacionParcialTexto = AlumnoSeleccionado.Calificación[_evaluacionActual] ?? "";
                    }
                    catch
                    {
                        CalificacionParcialTexto = "";
                    }
                }
            }
            return;
        }

        foreach (var actividad in Actividades) actividad.PuntajeObtenido = "";
        Inasistencias = "";
        AlumnoConCapturaDirecta = false;
        LeyendaCapturaDirecta = string.Empty;
        CalificacionParcialTexto = "";
        foreach (var ed in Actividades) ed.SetBloqueadoPorCapturaDirecta(false);
    }

    private void NormalizarActividadesEnMateria()
    {
        _materia.Actividades = (_materia.Actividades ?? new List<ActividadParcial>()).Take(4).ToList();
    }

    private void ReconstruirActividadesEnPantalla()
    {
        Actividades.Clear();
        for (int i = 0; i < _materia.Actividades.Count; i++)
        {
            var modelo = _materia.Actividades[i];
            var editor = new ActividadParcialEditor(() => EditorChanged());
            editor.CargarDesdeModelo(modelo);
            Actividades.Add(editor);
        }
        while (Actividades.Count < 4)
        {
            Actividades.Add(CreateBlankEditor());
        }
    }

    private ActividadParcialEditor CreateBlankEditor()
    {
        var ed = new ActividadParcialEditor(() => EditorChanged());
        ed.Activa = false;
        ed.Nombre = string.Empty;
        ed.Porcentaje = string.Empty;
        ed.PuntajeMaximo = string.Empty;
        ed.PuntajeObtenido = string.Empty;
        return ed;
    }

    private void PersistirCapturasTemporales(string? matricula)
    {
        if (string.IsNullOrWhiteSpace(matricula) || matricula == "$CONFIG$") return;
        var capturas = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        
        capturas["__CAPTURA_DIRECTA__"] = AlumnoConCapturaDirecta ? 1.0 : 0.0;
        
        if (AlumnoConCapturaDirecta)
        {
            if (double.TryParse(CalificacionParcialTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out double dval))
            {
                capturas["__CALIF_DIRECTA__"] = dval;
            }
        }

        foreach (var actividad in Actividades)
        {
            if (!string.IsNullOrWhiteSpace(actividad.Nombre))
            {
                if (double.TryParse(actividad.PuntajeObtenido, NumberStyles.Any, CultureInfo.InvariantCulture, out double obt))
                {
                    capturas[actividad.Nombre.Trim()] = obt;
                }
                else
                {
                    capturas[actividad.Nombre.Trim()] = 0;
                }
            }
        }
        
        if (int.TryParse(Inasistencias, out int inaVal))
            capturas["__Inasistencias__"] = inaVal;
        else
            capturas["__Inasistencias__"] = -1;

        if (int.TryParse(ClasesTotales, out int ct))
            _materia.Calificaciones["$CONFIG$"] = new Dictionary<string, double> { { "AsistenciaActiva", AsistenciaActiva ? 1 : 0 }, { "ClasesTotales", ct } };
        else
            _materia.Calificaciones["$CONFIG$"] = new Dictionary<string, double> { { "AsistenciaActiva", AsistenciaActiva ? 1 : 0 }, { "ClasesTotales", -1 } };

        _materia.Calificaciones[matricula] = capturas;
    }

    private void RecalcularTodo(bool guardarJson, bool esCargaInicial = false, bool marcarCambios = true)
    {
        if (_cargasActivas > 0 && !esCargaInicial) return;

        decimal sumaPorcentajes = 0m;
        decimal acumulado = 0m;
        bool logicaCorrecta = true;

        var entradas = new List<(double porc, double max, double obt)>();

        foreach (var actividad in Actividades)
        {
            if (!actividad.Activa) continue;

            if (!double.TryParse(actividad.Porcentaje, NumberStyles.Any, CultureInfo.InvariantCulture, out double porc) || porc < 0 || porc > 100)
            {
                logicaCorrecta = false;
                continue;
            }

            if (!double.TryParse(actividad.PuntajeMaximo, NumberStyles.Any, CultureInfo.InvariantCulture, out double max) || max <= 0)
            {
                logicaCorrecta = false;
                continue;
            }

            if (string.IsNullOrWhiteSpace(actividad.PuntajeObtenido))
            {
                logicaCorrecta = false;
                continue;
            }

            if (!double.TryParse(actividad.PuntajeObtenido, NumberStyles.Any, CultureInfo.InvariantCulture, out double obt) || obt < 0 || obt > max)
            {
                logicaCorrecta = false;
                continue;
            }

            entradas.Add((porc, max, obt));
            sumaPorcentajes += (decimal)porc;
        }

        double scaling = 1.0;
        if (sumaPorcentajes > 0)
        {
            scaling = 100.0 / (double)sumaPorcentajes;
        }

        foreach (var (porc, max, obt) in entradas)
        {
            double porcNorm = porc * scaling;
            acumulado += ((decimal)obt / (decimal)max) * (decimal)porcNorm;
        }

        SumaPorcentajes = sumaPorcentajes;
        SumaPorcentajesTexto = $"{TruncarUnDecimal(sumaPorcentajes):0.0}%";

        if (sumaPorcentajes > 100m) PorcentajeEstado = "Over";
        else if (sumaPorcentajes < 100m) PorcentajeEstado = "Under";
        else PorcentajeEstado = "Ok";

        if (sumaPorcentajes > 0m && logicaCorrecta)
        {
            decimal calificacion = TruncarUnDecimal(acumulado / 10m);
            if (!AlumnoConCapturaDirecta)
            {
                CalificacionParcialTexto = calificacion.ToString("0.0", CultureInfo.InvariantCulture);

                if (AlumnoSeleccionado != null && !string.IsNullOrWhiteSpace(_evaluacionActual))
                {
                    AlumnoSeleccionado.Calificación[_evaluacionActual] = CalificacionParcialTexto;
                }
            }
        }
        else
        {
            if (!AlumnoConCapturaDirecta) 
            {
                CalificacionParcialTexto = "";
                if (AlumnoSeleccionado != null && !string.IsNullOrWhiteSpace(_evaluacionActual))
                {
                    AlumnoSeleccionado.Calificación[_evaluacionActual] = "";
                }
            }
        }

        if (AlumnoSeleccionado != null)
        {
            PersistirCapturasTemporales(AlumnoSeleccionado.Matricula);
        }

        if (guardarJson) GuardarEnJsonLocal();

        ActualizarConteoEvaluados();

        if (!guardarJson && marcarCambios && _cargasActivas == 0 && !_suspendUserEditMarking)
        {
            TieneCambios = true;
            _lastUserEditTime = DateTime.UtcNow;
        }
    }

    private void GuardarEnJsonLocal()
    {
        if (string.IsNullOrWhiteSpace(_claveMateria) || string.IsNullOrWhiteSpace(_evaluacionActual)) return;

        int clasesT = -1;
        if (int.TryParse(ClasesTotales, out int parsedCt)) clasesT = parsedCt;

        var config = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["AsistenciaActiva"] = AsistenciaActiva ? 1 : 0,
            ["ClasesTotales"] = clasesT
        };
        _materia.Calificaciones["$CONFIG$"] = config;

        if (AlumnoSeleccionado != null && !string.IsNullOrWhiteSpace(AlumnoSeleccionado.Matricula))
        {
            PersistirCapturasTemporales(AlumnoSeleccionado.Matricula);
        }

        double acumuladoPorcentajes = 0.0;
        foreach (var ed in Actividades)
        {
            if (!ed.Activa) continue;
            var text = (ed.Porcentaje ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(text)) continue;
            text = text.Replace(',', '.');
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double p))
            {
                acumuladoPorcentajes += p;
            }
        }

        _materia.PorcentajeAcumulado = acumuladoPorcentajes;
        _materia.Actividades = Actividades.Select(a => a.ToModelo()).ToList();

        string claveMateriaEval = $"{_claveMateria}_{_evaluacionActual}";
        _parcialJsonService.GuardarMateria(claveMateriaEval, _materia);

        TieneCambios = false;
        _lastUserEditTime = null;
        _lastArchivoSeleccionado = _mainVm.ArchivoSeleccionado;
        _lastEvaluacionSeleccionada = _mainVm.EvaluacionSeleccionada;
        _lastLoadOrSaveTime = DateTime.UtcNow;
    }

    private static string ObtenerClaveMateriaDesdeNombreVisual(string? nombreVisual)
    {
        if (string.IsNullOrWhiteSpace(nombreVisual)) return string.Empty;
        string texto = nombreVisual.Trim();
        int indexEspacio = texto.IndexOf(" - Grupo:");
        if(indexEspacio > 0) texto = texto.Substring(0, indexEspacio).Trim();

        int firstSpace = texto.IndexOf(' ');
        if (firstSpace <= 0) return texto.Replace(' ', '_');

        string clave = texto[..firstSpace].Trim();
        string nombre = texto[(firstSpace + 1)..].Trim();
        return string.IsNullOrWhiteSpace(nombre) ? clave : $"{clave}_{nombre}";
    }

    private static string ObtenerClaveMateriaDesdeNombreArchivo(string rutaCompleta)
    {
        try
        {
            var nombre = Path.GetFileNameWithoutExtension(rutaCompleta);
            if (string.IsNullOrWhiteSpace(nombre)) return string.Empty;
            return nombre.Trim().Replace(' ', '_');
        }
        catch
        {
            return string.Empty;
        }
    }

    private string ObtenerGrupoDesdeJson(string matricula)
    {
        return (!string.IsNullOrWhiteSpace(matricula) && _mapaGrupos.TryGetValue(matricula.Trim(), out var grupo)) ? grupo : string.Empty;
    }

    private void CargarMapaGrupos()
    {
        _mapaGrupos.Clear();
        try
        {
            using var lite = new LiteDbService();
            var grupos = lite.GetGrupos();
            foreach (var kv in grupos)
            {
                _mapaGrupos[kv.Key] = kv.Value;
            }
        }
        catch { }
    }

    private static decimal TruncarUnDecimal(decimal valor) => Math.Truncate(valor * 10m) / 10m;

    [RelayCommand] private void Inicio() { if (Alumnos.Any()) AlumnoSeleccionado = Alumnos.First(); }
    [RelayCommand] private void Anterior() { if (AlumnoSeleccionado != null && Alumnos.Any()) { int i = Alumnos.IndexOf(AlumnoSeleccionado); if (i > 0) AlumnoSeleccionado = Alumnos[i - 1]; } }
    [RelayCommand] private void Siguiente() { if (AlumnoSeleccionado != null && Alumnos.Any()) { int i = Alumnos.IndexOf(AlumnoSeleccionado); if (i >= 0 && i < Alumnos.Count - 1) AlumnoSeleccionado = Alumnos[i + 1]; } }
    [RelayCommand] private void Final() { if (Alumnos.Any()) AlumnoSeleccionado = Alumnos.Last(); }
}

public partial class ActividadParcialEditor : ObservableObject
{
    private readonly Action _notificarCambio;
    private bool _bloqueadoPorCapturaDirecta = false;

    [ObservableProperty] private bool _estaCompleta;

    [ObservableProperty] private bool _activa;
    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private string _porcentaje = string.Empty;
    [ObservableProperty] private string _puntajeMaximo = string.Empty;
    [ObservableProperty] private string _puntajeObtenido = string.Empty;

    public ActividadParcialEditor(Action notificarCambio)
    {
        _notificarCambio = notificarCambio;
    }

    public void SetBloqueadoPorCapturaDirecta(bool bloqueado)
    {
        _bloqueadoPorCapturaDirecta = bloqueado;
        OnPropertyChanged(nameof(IsPuntajeEditable));
        OnPropertyChanged(nameof(IsPuntajeEditableFinal));
    }

    public bool IsPuntajeEditable => !_bloqueadoPorCapturaDirecta && Activa;

    public bool IsPuntajeEditableFinal => !_bloqueadoPorCapturaDirecta && Activa && EstaCompleta;

    public void CargarDesdeModelo(ActividadParcial modelo)
    {
        Activa = modelo.Activa;
        Nombre = modelo.Nombre;
        Porcentaje = modelo.Porcentaje == 0 ? "" : modelo.Porcentaje.ToString(CultureInfo.InvariantCulture);
        PuntajeMaximo = modelo.PuntajeMaximo == 0 ? "" : modelo.PuntajeMaximo.ToString(CultureInfo.InvariantCulture);
        PuntajeObtenido = "";

        ActualizarVistaInmediata();
    }

    public ActividadParcial ToModelo()
    {
        double.TryParse(Porcentaje, NumberStyles.Any, CultureInfo.InvariantCulture, out double porc);
        double.TryParse(PuntajeMaximo, NumberStyles.Any, CultureInfo.InvariantCulture, out double max);
        return new ActividadParcial { Activa = Activa, Nombre = Nombre?.Trim() ?? string.Empty, Porcentaje = porc, PuntajeMaximo = max };
    }

    public string FraccionTexto
    {
        get
        {
            string obt = string.IsNullOrWhiteSpace(PuntajeObtenido) ? "" : PuntajeObtenido;
            string max = string.IsNullOrWhiteSpace(PuntajeMaximo) ? "" : PuntajeMaximo;
            if (string.IsNullOrEmpty(obt) && string.IsNullOrEmpty(max)) return "";
            return $"{obt} / {max}";
        }
    }

    public string ContribucionTexto
    {
        get
        {
            if (!Activa) return "";
            if (!double.TryParse(PuntajeMaximo, NumberStyles.Any, CultureInfo.InvariantCulture, out double max) || max <= 0) return "0.0";
            if (!double.TryParse(PuntajeObtenido, NumberStyles.Any, CultureInfo.InvariantCulture, out double obt) || obt < 0) return "0.0";
            if (!double.TryParse(Porcentaje, NumberStyles.Any, CultureInfo.InvariantCulture, out double porc)) return "0.0";

            decimal contribucion = ((decimal)obt / (decimal)max) * (decimal)porc;
            return (Math.Truncate(contribucion * 10m) / 10m).ToString("0.0", CultureInfo.InvariantCulture);
        }
    }

    partial void OnActivaChanged(bool value)
    {
        if (value)
        {
            OnPropertyChanged(nameof(Nombre));
            OnPropertyChanged(nameof(Porcentaje));
        }
        NotificarActualizacion();
    }
    partial void OnNombreChanged(string value) => _notificarCambio?.Invoke();
    partial void OnPorcentajeChanged(string value) => NotificarActualizacion();
    partial void OnPuntajeMaximoChanged(string value) => NotificarActualizacion();
    partial void OnPuntajeObtenidoChanged(string value) => NotificarActualizacion();

    private void NotificarActualizacion()
    {
        ActualizarVistaInmediata();
        ActualizarEstadoCompleto();
        _notificarCambio?.Invoke();
    }

    private void ActualizarVistaInmediata()
    {
        OnPropertyChanged(nameof(FraccionTexto));
        OnPropertyChanged(nameof(ContribucionTexto));
    }

    private void ActualizarEstadoCompleto()
    {
        bool completo = !string.IsNullOrWhiteSpace(Nombre)
                        && !string.IsNullOrWhiteSpace(Porcentaje)
                        && !string.IsNullOrWhiteSpace(PuntajeMaximo);
        if (EstaCompleta != completo)
        {
            EstaCompleta = completo;
            OnPropertyChanged(nameof(IsPuntajeEditable));
            OnPropertyChanged(nameof(IsPuntajeEditableFinal));
        }
    }
}