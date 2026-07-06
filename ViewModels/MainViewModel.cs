using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
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

    public string? ArchivoCompletoActual => _archivoCompletoActual;

    public ParcialesViewModel ParcialesVm { get; }

    [ObservableProperty] private string _rutaUsb = string.Empty;
    [ObservableProperty] private bool _rutaUsbEditable = true;
    [ObservableProperty] private string _currentView = "List";
    [ObservableProperty] private bool _tieneCambios;

    // Propiedades exclusivas para ExtraView NavBar
    [ObservableProperty] private string _nombreMateriaExtra = string.Empty;
    [ObservableProperty] private string _nombreProfesorExtra = string.Empty;
    [ObservableProperty] private string _textoEvaluadosExtra = string.Empty;
    [ObservableProperty] private bool _faltanPorEvaluarExtra = false;
    public List<AlumnoFaltante> ListaNoEvaluadosExtra { get; private set; } = new();

    private string? _archivoSeleccionado;
    public string? ArchivoSeleccionado
    {
        get => _archivoSeleccionado;
        set
        {
            if (_archivoSeleccionado != value)
            {
                if (!ManejarCambiosPendientes())
                {
                    OnPropertyChanged(nameof(ArchivoSeleccionado));
                    return;
                }
                SetProperty(ref _archivoSeleccionado, value);
                CargarArchivoSeleccionado(value);
            }
        }
    }

    private string? _evaluacionSeleccionada;
    public string? EvaluacionSeleccionada
    {
        get => _evaluacionSeleccionada;
        set
        {
            if (_evaluacionSeleccionada != value)
            {
                if (!ManejarCambiosPendientes())
                {
                    OnPropertyChanged(nameof(EvaluacionSeleccionada));
                    return;
                }
                SetProperty(ref _evaluacionSeleccionada, value);
                CambiarEvaluacion(value);
            }
        }
    }

    // AHORA USA EL OBJETO CON ID Y NOMBRE VISUAL
    public ObservableCollection<EvaluacionItem> EvaluacionesDisponibles { get; } = new();
    
    public ObservableCollection<string> ArchivosDisponibles { get; } = new();
    public ObservableCollection<Alumno> Alumnos { get; } = new();

    private readonly System.Collections.Generic.List<Alumno> _subscribedAlumnos = new();

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
        catch { }

        EscanearUsb();
    }

    public void ActualizarConteoEvaluadosExtra()
    {
        if (CurrentView != "Extra") return;
        
        int total = Alumnos.Count;
        int evaluados = 0;
        var lista = new List<AlumnoFaltante>();

        foreach (var alumno in Alumnos)
        {
            if (!string.IsNullOrWhiteSpace(alumno.Calificación["EXTRA"]))
            {
                evaluados++;
            }
            else
            {
                lista.Add(new AlumnoFaltante {
                    Materia = NombreMateriaExtra,
                    Grupo = alumno.Grupo,
                    Matricula = alumno.Matricula,
                    Nombre = alumno.Nombre,
                    Razon = "Falta calificación de extraordinario"
                });
            }
        }

        TextoEvaluadosExtra = $"{evaluados} de {total}";
        FaltanPorEvaluarExtra = evaluados < total;
        ListaNoEvaluadosExtra = lista;
    }

    private bool ManejarCambiosPendientes()
    {
        if (TieneCambios)
        {
            var res = MessageBox.Show("Hay cambios sin guardar en esta vista. ¿Deseas guardar antes de cambiar?\n\nSí = Guardar y continuar\nNo = Descartar cambios\nCancelar = Quedarse aquí",
                "Cambios sin guardar", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

            if (res == MessageBoxResult.Cancel) return false;
            if (res == MessageBoxResult.Yes) Guardar();
            
            TieneCambios = false;
        }
        return true;
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
        if (string.IsNullOrWhiteSpace(rutaCompleta)) return string.Empty;
        string nombre = Path.GetFileNameWithoutExtension(rutaCompleta);
        return string.IsNullOrWhiteSpace(nombre) ? string.Empty : nombre.Trim().Replace(' ', '_');
    }

    private bool EvaluacionEstaHabilitada(string evaluacion)
    {
        if (string.IsNullOrWhiteSpace(evaluacion)) return false;

        return evaluacion.Trim().ToUpperInvariant() switch
        {
            "P1" => _configuracionActual.Parcial1Habilitado,
            "P2" => _configuracionActual.Parcial2Habilitado,
            "P3" => _configuracionActual.Parcial3Habilitado,
            "SEM" => _configuracionActual.SemestralHabilitado,
            "EXTRA" => _configuracionActual.ExtraHabilitado,
            _ => true
        };
    }

    private string ObtenerNombreEvaluacionVisual(string eval)
    {
        return eval.ToUpperInvariant() switch
        {
            "P1" => "PARCIAL 1",
            "P2" => "PARCIAL 2",
            "P3" => "PARCIAL 3",
            "SEM" => "SEMESTRAL",
            "EXTRA" => "EXTRAORDINARIO/INTER",
            _ => eval
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
        TieneCambios = false;

        _archivoSeleccionado = null;
        OnPropertyChanged(nameof(ArchivoSeleccionado));
        
        _evaluacionSeleccionada = null;
        OnPropertyChanged(nameof(EvaluacionSeleccionada));
        
        CurrentView = "List";

        var archivos = _scannerService.ObtenerArchivosCap(RutaUsb);

        foreach (var archivo in archivos)
        {
            var info = _parserService.ObtenerInfoParaCombo(archivo);
            string nombreCombo;

            if (info.IsExtra)
            {
                string profesorMostrar = string.IsNullOrWhiteSpace(info.NombreProfesor) ? "SIN REGISTRO" : info.NombreProfesor;
                nombreCombo = $"{info.NombreBase} - Grupo: {profesorMostrar}";
            }
            else
            {
                var resTemp = _parserService.ProcesarArchivoCompleto(archivo);
                string grupoNormal = resTemp.Alumnos.FirstOrDefault()?.Grupo ?? "S/G";
                nombreCombo = $"{info.NombreBase} - Grupo: {grupoNormal}";
            }

            _mapaArchivos[nombreCombo] = archivo;
            ArchivosDisponibles.Add(nombreCombo);
        }

        if (ArchivosDisponibles.Any())
        {
            ArchivoSeleccionado = ArchivosDisponibles.First();
        }

        OnPropertyChanged(nameof(EsExtraSeleccionado));
    }

    private void CargarArchivoSeleccionado(string? value)
    {
        _isUpdatingProgrammatically = true;
        
        Alumnos.Clear();
        EvaluacionesDisponibles.Clear();
        _evaluacionIdPorNombre.Clear();
        
        _evaluacionSeleccionada = null;
        OnPropertyChanged(nameof(EvaluacionSeleccionada));
        
        CurrentView = "List";
        _archivoCompletoActual = null;

        if (string.IsNullOrWhiteSpace(value) || !_mapaArchivos.TryGetValue(value, out string? rutaCompleta))
        {
            OnPropertyChanged(nameof(EsExtraSeleccionado));
            _isUpdatingProgrammatically = false;
            return;
        }

        if (!File.Exists(rutaCompleta))
        {
            OnPropertyChanged(nameof(EsExtraSeleccionado));
            _isUpdatingProgrammatically = false;
            return;
        }

        _archivoCompletoActual = rutaCompleta;
        string claveMateria = ObtenerClaveMateriaDesdeRuta(rutaCompleta);
        _configuracionActual = _configuracionService.ObtenerConfiguracion(claveMateria);

        var infoCombo = _parserService.ObtenerInfoParaCombo(rutaCompleta);
        NombreMateriaExtra = infoCombo.NombreBase;
        NombreProfesorExtra = string.IsNullOrWhiteSpace(infoCombo.NombreProfesor) ? "SIN REGISTRO" : infoCombo.NombreProfesor;

        var resultado = _parserService.ProcesarArchivoCompleto(rutaCompleta);

        foreach (var kvp in resultado.EvaluacionIdPorNombre)
        {
            _evaluacionIdPorNombre[kvp.Key] = kvp.Value;
        }

        bool esExtra = resultado.EvaluacionesDisponibles.Any(e => e.Equals("EXTRA", StringComparison.OrdinalIgnoreCase));
        
        if (esExtra)
        {
            EvaluacionesDisponibles.Add(new EvaluacionItem { Id = "EXTRA", Nombre = "EXTRAORDINARIO/INTER" });
            EvaluacionSeleccionada = "EXTRA";
        }
        else
        {
            foreach (var eval in resultado.EvaluacionesDisponibles)
            {
                if (EvaluacionEstaHabilitada(eval))
                {
                    EvaluacionesDisponibles.Add(new EvaluacionItem { Id = eval, Nombre = ObtenerNombreEvaluacionVisual(eval) });
                }
            }

            if (EvaluacionesDisponibles.Count == 0) EvaluacionesDisponibles.Add(new EvaluacionItem { Id = "P1", Nombre = "PARCIAL 1" });
            EvaluacionSeleccionada = EvaluacionesDisponibles.LastOrDefault()?.Id;
        }

        foreach (var alumno in resultado.Alumnos)
        {
            Alumnos.Add(alumno);
        }

        SuscribirAlumnos();
        OnPropertyChanged(nameof(EsExtraSeleccionado));
        
        TieneCambios = false;
        _isUpdatingProgrammatically = false;
        
        if (esExtra) 
        {
            CambiarEvaluacion("EXTRA");
            ActualizarConteoEvaluadosExtra();
        }
    }

    private void CambiarEvaluacion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            CurrentView = "List";
            OnPropertyChanged(nameof(EsExtraSeleccionado));
            return;
        }
        
        _isUpdatingProgrammatically = true;
        string valorMayusculas = value.ToUpperInvariant();
        
        // Barrera de seguridad Exclusiva EXTRA usando validación del objeto EvaluacionItem
        if (valorMayusculas != "EXTRA" && EvaluacionesDisponibles.Count == 1 && EvaluacionesDisponibles.Any(e => e.Id == "EXTRA"))
        {
            valorMayusculas = "EXTRA";
        }

        if (valorMayusculas == "SEM")
        {
            CurrentView = "Semestral";
            SincronizarCalificacionSemestral();
        }
        else if (valorMayusculas == "EXTRA")
        {
            CurrentView = "Extra";
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
        TieneCambios = false;
        _isUpdatingProgrammatically = false;
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

        _isUpdatingProgrammatically = true;
        SincronizarCalificacionSemestral();
        _isUpdatingProgrammatically = false;

        if (!string.Equals(EvaluacionSeleccionada, "SEM", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(EvaluacionSeleccionada, "EXTRA", StringComparison.OrdinalIgnoreCase) &&
            _evaluacionIdPorNombre.TryGetValue("SEM", out var idSem))
        {
            _writerService.GuardarEvaluacion(
                _archivoCompletoActual,
                Alumnos.ToList(),
                "SEM",
                idSem);
        }

        TieneCambios = false;
        ParcialesVm.TieneCambios = false;

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
            catch { }
        }

        if (string.IsNullOrWhiteSpace(claveMateria) && !string.IsNullOrWhiteSpace(ArchivoSeleccionado))
        {
            string texto = ArchivoSeleccionado.Trim();
            int indexEspacio = texto.IndexOf(" - Grupo:");
            if (indexEspacio > 0)
            {
                texto = texto.Substring(0, indexEspacio).Trim();
            }
            int indexSegundoEspacio = texto.IndexOf(' ');
            if (indexSegundoEspacio <= 0) claveMateria = texto.Replace(' ', '_');
            else
            {
                string clave = texto[..indexSegundoEspacio].Trim();
                string nombre = texto[(indexSegundoEspacio + 1)..].Trim();
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

        int clasesP1 = p1Activa && c1 != null && c1.TryGetValue("ClasesTotales", out var ct1) && ct1 > 0 ? (int)ct1 : 0;
        int clasesP2 = p2Activa && c2 != null && c2.TryGetValue("ClasesTotales", out var ct2) && ct2 > 0 ? (int)ct2 : 0;
        int clasesP3 = p3Activa && c3 != null && c3.TryGetValue("ClasesTotales", out var ct3) && ct3 > 0 ? (int)ct3 : 0;

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

            int faltasP1 = p1Activa && m1.Calificaciones.TryGetValue(alumno.Matricula, out var cap1) && cap1.TryGetValue("__Inasistencias__", out var f1) && f1 >= 0 ? (int)f1 : 0;
            int faltasP2 = p2Activa && m2.Calificaciones.TryGetValue(alumno.Matricula, out var cap2) && cap2.TryGetValue("__Inasistencias__", out var f2) && f2 >= 0 ? (int)f2 : 0;
            int faltasP3 = p3Activa && m3.Calificaciones.TryGetValue(alumno.Matricula, out var cap3) && cap3.TryGetValue("__Inasistencias__", out var f3) && f3 >= 0 ? (int)f3 : 0;

            int totalClases = clasesP1 + clasesP2 + clasesP3;
            int totalFaltas = faltasP1 + faltasP2 + faltasP3;

            bool cumpleAsistencia = true;
            if (totalClases > 0)
            {
                int asistencias = totalClases - totalFaltas;
                double porcentajeAsistencia = ((double)asistencias / totalClases) * 100.0;
                cumpleAsistencia = porcentajeAsistencia >= 80.0;
            }

            if (cumpleAsistencia)
            {
                alumno.Calificación["SEM"] = promedioRedondeado.ToString();
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

    private void SuscribirAlumnos()
    {
        foreach (var a in _subscribedAlumnos)
        {
            try { a.Calificación.PropertyChanged -= Alumno_CalificacionChanged; } catch { }
        }
        _subscribedAlumnos.Clear();

        foreach (var alumno in Alumnos)
        {
            if (alumno?.Calificación != null)
            {
                alumno.Calificación.PropertyChanged += Alumno_CalificacionChanged;
                _subscribedAlumnos.Add(alumno);
            }
        }
    }

    private void Alumno_CalificacionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isUpdatingProgrammatically) return; 
        
        if (e?.PropertyName != null && e.PropertyName.StartsWith("Item["))
        {
            TieneCambios = true;
            if (EsExtraSeleccionado)
            {
                ActualizarConteoEvaluadosExtra();
            }
        }
    }
}