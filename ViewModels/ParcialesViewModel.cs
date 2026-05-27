using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.ViewModels;

public partial class ParcialesViewModel : ObservableObject
{
    private readonly MainViewModel _mainVm;
    private readonly ParcialJsonService _parcialJsonService;
    private readonly Dictionary<string, string> _mapaGrupos = new(StringComparer.OrdinalIgnoreCase);

    private MateriaParcial _materia = new();
    private bool _cargando;
    private string _claveMateria = string.Empty;
    private string _evaluacionActual = string.Empty;
    private string? _ultimaMatriculaSeleccionada;

    public ObservableCollection<Alumno> Alumnos => _mainVm.Alumnos;

    public ObservableCollection<ActividadParcialEditor> Actividades { get; } = new();

    [ObservableProperty]
    private Alumno? _alumnoSeleccionado;

    [ObservableProperty]
    private string _nombreMateria = string.Empty;

    [ObservableProperty]
    private string _nombreEvaluacion = string.Empty;

    [ObservableProperty]
    private string _nombreAlumno = string.Empty;

    [ObservableProperty]
    private string _matriculaAlumno = string.Empty;

    [ObservableProperty]
    private string _grupoAlumno = string.Empty;

    [ObservableProperty]
    private decimal _sumaPorcentajes;

    [ObservableProperty]
    private string _sumaPorcentajesTexto = "0%";

    [ObservableProperty]
    private string _calificacionParcialTexto = "0.0";

    [ObservableProperty]
    private string _estadoValidacion = "Sin cargar";

    [ObservableProperty]
    private string _estadoGuardado = string.Empty;

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

    private void MainVm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.ArchivoSeleccionado) ||
            e.PropertyName == nameof(MainViewModel.EvaluacionSeleccionada))
        {
            CargarContextoActual();
        }
    }

    partial void OnAlumnoSeleccionadoChanged(Alumno? value)
    {
        if (_cargando)
            return;

        _ultimaMatriculaSeleccionada = value?.Matricula;

        ActualizarDatosAlumnoSeleccionado();
        CargarCapturasDelAlumnoSeleccionado();
        RecalcularTodo(guardarJson: false);
    }

    private void CargarContextoActual()
    {
        _cargando = true;

        try
        {
            NombreMateria = _mainVm.ArchivoSeleccionado ?? string.Empty;
            NombreEvaluacion = _mainVm.EvaluacionSeleccionada ?? string.Empty;
            _evaluacionActual = NombreEvaluacion;

            _claveMateria = ObtenerClaveMateriaDesdeNombreVisual(_mainVm.ArchivoSeleccionado);

            if (string.IsNullOrWhiteSpace(_claveMateria))
            {
                LimpiarVista();
                return;
            }

            _materia = _parcialJsonService.ObtenerMateria(_claveMateria) ?? new MateriaParcial();

            NormalizarActividadesEnMateria();
            ReconstruirActividadesEnPantalla();
            RestaurarAlumnoSeleccionado();
            ActualizarDatosAlumnoSeleccionado();
            CargarCapturasDelAlumnoSeleccionado();
        }
        finally
        {
            _cargando = false;
        }

        RecalcularTodo(guardarJson: false);
    }

    private void LimpiarVista()
    {
        Actividades.Clear();
        SumaPorcentajes = 0;
        SumaPorcentajesTexto = "0%";
        CalificacionParcialTexto = "0.0";
        EstadoValidacion = "Sin materia cargada";
        EstadoGuardado = string.Empty;
        NombreAlumno = string.Empty;
        MatriculaAlumno = string.Empty;
        GrupoAlumno = string.Empty;
    }

    private void RestaurarAlumnoSeleccionado()
    {
        if (!string.IsNullOrWhiteSpace(_ultimaMatriculaSeleccionada))
        {
            var alumno = Alumnos.FirstOrDefault(a =>
                string.Equals(a.Matricula, _ultimaMatriculaSeleccionada, StringComparison.OrdinalIgnoreCase));

            if (alumno != null)
            {
                AlumnoSeleccionado = alumno;
                return;
            }
        }

        if (AlumnoSeleccionado == null && Alumnos.Any())
        {
            AlumnoSeleccionado = Alumnos.First();
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

        NombreAlumno = AlumnoSeleccionado.Nombre;
        MatriculaAlumno = AlumnoSeleccionado.Matricula;
        GrupoAlumno = ObtenerGrupoDesdeJson(AlumnoSeleccionado.Matricula);

        if (string.IsNullOrWhiteSpace(GrupoAlumno))
        {
            GrupoAlumno = AlumnoSeleccionado.Grupo;
        }
    }

    private void CargarCapturasDelAlumnoSeleccionado()
    {
        if (AlumnoSeleccionado == null)
        {
            foreach (var actividad in Actividades)
            {
                actividad.PuntajeObtenido = 0;
            }

            return;
        }

        if (_materia.Calificaciones.TryGetValue(AlumnoSeleccionado.Matricula, out var capturas))
        {
            foreach (var actividad in Actividades)
            {
                if (!string.IsNullOrWhiteSpace(actividad.Nombre) &&
                    capturas.TryGetValue(actividad.Nombre, out var valor))
                {
                    actividad.PuntajeObtenido = valor;
                }
                else
                {
                    actividad.PuntajeObtenido = 0;
                }
            }

            return;
        }

        foreach (var actividad in Actividades)
        {
            actividad.PuntajeObtenido = 0;
        }
    }

    private void NormalizarActividadesEnMateria()
    {
        var existentes = (_materia.Actividades ?? new List<ActividadParcial>())
            .Take(4)
            .ToList();

        while (existentes.Count < 4)
        {
            existentes.Add(CrearActividadPorDefecto(existentes.Count));
        }

        _materia.Actividades = existentes;
    }

    private void ReconstruirActividadesEnPantalla()
    {
        Actividades.Clear();

        for (int i = 0; i < 4; i++)
        {
            var modelo = i < _materia.Actividades.Count
                ? _materia.Actividades[i]
                : CrearActividadPorDefecto(i);

            var editor = new ActividadParcialEditor(ValidarCambioActividad, NotificarCambioBloqueado);
            editor.CargarDesdeModelo(modelo);
            Actividades.Add(editor);
        }
    }

    private static ActividadParcial CrearActividadPorDefecto(int indice)
    {
        return indice switch
        {
            0 => new ActividadParcial
            {
                Activa = true,
                Nombre = "TRABAJOS",
                Porcentaje = 80,
                PuntajeMaximo = 5
            },
            1 => new ActividadParcial
            {
                Activa = true,
                Nombre = "EXAMEN",
                Porcentaje = 20,
                PuntajeMaximo = 100
            },
            _ => new ActividadParcial
            {
                Activa = false,
                Nombre = string.Empty,
                Porcentaje = 0,
                PuntajeMaximo = 0
            }
        };
    }

    private bool ValidarCambioActividad(ActividadParcialEditor editor)
    {
        if (editor.Porcentaje < 0 || editor.Porcentaje > 100)
            return false;

        if (editor.PuntajeMaximo < 0 || editor.PuntajeObtenido < 0)
            return false;

        if (editor.PuntajeMaximo > 0 && editor.PuntajeObtenido > editor.PuntajeMaximo)
            return false;

        decimal suma = Actividades
            .Where(a => a.Activa)
            .Sum(a => (decimal)a.Porcentaje);

        if (suma > 100m)
            return false;

        return true;
    }

    private void NotificarCambioBloqueado(string motivo)
    {
        EstadoValidacion = motivo;
    }

    private void RecalcularTodo(bool guardarJson)
    {
        decimal sumaPorcentajes = 0m;
        decimal acumulado = 0m;

        foreach (var actividad in Actividades)
        {
            if (!actividad.Activa)
                continue;

            sumaPorcentajes += (decimal)actividad.Porcentaje;

            if (actividad.PuntajeMaximo > 0)
            {
                decimal obtenido = (decimal)actividad.PuntajeObtenido;
                decimal maximo = (decimal)actividad.PuntajeMaximo;
                decimal porcentaje = (decimal)actividad.Porcentaje;

                acumulado += (obtenido / maximo) * porcentaje;
            }
        }

        SumaPorcentajes = sumaPorcentajes;
        SumaPorcentajesTexto = $"{TruncarUnDecimal(sumaPorcentajes):0.0}%";

        decimal calificacion = TruncarUnDecimal(acumulado / 10m);
        CalificacionParcialTexto = calificacion.ToString("0.0", CultureInfo.InvariantCulture);

        bool porcentajesCorrectos = Math.Abs(sumaPorcentajes - 100m) < 0.0001m;

        EstadoValidacion = porcentajesCorrectos
            ? "Porcentajes correctos"
            : $"Los porcentajes suman {sumaPorcentajes:0.0}% y deben dar 100%";

        if (guardarJson)
        {
            GuardarEnJsonLocal();
        }

        if (porcentajesCorrectos && AlumnoSeleccionado != null && !string.IsNullOrWhiteSpace(_evaluacionActual))
        {
            AlumnoSeleccionado.ValorSeleccionado = CalificacionParcialTexto;
        }

        EstadoGuardado = guardarJson
            ? "Guardado en Data/parciales.json"
            : string.Empty;
    }

    private void GuardarEnJsonLocal()
    {
        if (string.IsNullOrWhiteSpace(_claveMateria))
            return;

        _materia.Actividades = Actividades
            .Select(a => a.ToModelo())
            .ToList();

        if (AlumnoSeleccionado != null && !string.IsNullOrWhiteSpace(AlumnoSeleccionado.Matricula))
        {
            var capturas = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            foreach (var actividad in Actividades)
            {
                if (actividad.Activa && !string.IsNullOrWhiteSpace(actividad.Nombre))
                {
                    capturas[actividad.Nombre.Trim()] = actividad.PuntajeObtenido;
                }
            }

            _materia.Calificaciones[AlumnoSeleccionado.Matricula] = capturas;
        }

        _parcialJsonService.GuardarMateria(_claveMateria, _materia);
    }

    private static string ObtenerClaveMateriaDesdeNombreVisual(string? nombreVisual)
    {
        if (string.IsNullOrWhiteSpace(nombreVisual))
            return string.Empty;

        string texto = nombreVisual.Trim();
        int indexEspacio = texto.IndexOf(' ');

        if (indexEspacio <= 0)
            return texto.Replace(' ', '_');

        string clave = texto[..indexEspacio].Trim();
        string nombre = texto[(indexEspacio + 1)..].Trim();

        if (string.IsNullOrWhiteSpace(nombre))
            return clave;

        return $"{clave}_{nombre}";
    }

    private string ObtenerGrupoDesdeJson(string matricula)
    {
        if (string.IsNullOrWhiteSpace(matricula))
            return string.Empty;

        if (_mapaGrupos.TryGetValue(matricula.Trim(), out var grupo))
            return grupo;

        return string.Empty;
    }

    private void CargarMapaGrupos()
    {
        _mapaGrupos.Clear();

        try
        {
            string rutaJson = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "grupo.json");

            if (!File.Exists(rutaJson))
                return;

            string json = File.ReadAllText(rutaJson, Encoding.UTF8);

            var datos = JsonSerializer.Deserialize<List<List<string>>>(json);

            if (datos == null)
                return;

            foreach (var relacion in datos)
            {
                if (relacion.Count < 2)
                    continue;

                string matricula = relacion[0].Trim();
                string grupo = relacion[1].Trim();

                if (!_mapaGrupos.ContainsKey(matricula))
                {
                    _mapaGrupos.Add(matricula, grupo);
                }
            }
        }
        catch
        {
        }
    }

    private static decimal TruncarUnDecimal(decimal valor)
    {
        return Math.Truncate(valor * 10m) / 10m;
    }

    [RelayCommand]
    private void Guardar()
    {
        RecalcularTodo(guardarJson: true);

        if (Math.Abs(SumaPorcentajes - 100m) >= 0.0001m)
        {
            return;
        }

        if (AlumnoSeleccionado != null && !string.IsNullOrWhiteSpace(_evaluacionActual))
        {
            AlumnoSeleccionado.ValorSeleccionado = CalificacionParcialTexto;
        }
    }

    [RelayCommand]
    private void Inicio()
    {
        if (Alumnos.Any())
            AlumnoSeleccionado = Alumnos.First();
    }

    [RelayCommand]
    private void Anterior()
    {
        if (AlumnoSeleccionado == null || !Alumnos.Any())
            return;

        int indice = Alumnos.IndexOf(AlumnoSeleccionado);
        if (indice > 0)
            AlumnoSeleccionado = Alumnos[indice - 1];
    }

    [RelayCommand]
    private void Siguiente()
    {
        if (AlumnoSeleccionado == null || !Alumnos.Any())
            return;

        int indice = Alumnos.IndexOf(AlumnoSeleccionado);
        if (indice >= 0 && indice < Alumnos.Count - 1)
            AlumnoSeleccionado = Alumnos[indice + 1];
    }

    [RelayCommand]
    private void Final()
    {
        if (Alumnos.Any())
            AlumnoSeleccionado = Alumnos.Last();
    }
}

public partial class ActividadParcialEditor : ObservableObject
{
    private readonly Func<ActividadParcialEditor, bool> _validador;
    private readonly Action<string> _bloqueo;

    private bool _activaAnterior;
    private string _nombreAnterior = string.Empty;
    private double _porcentajeAnterior;
    private double _puntajeMaximoAnterior;
    private double _puntajeObtenidoAnterior;

    [ObservableProperty]
    private bool _activa;

    [ObservableProperty]
    private string _nombre = string.Empty;

    [ObservableProperty]
    private double _porcentaje;

    [ObservableProperty]
    private double _puntajeMaximo;

    [ObservableProperty]
    private double _puntajeObtenido;

    public ActividadParcialEditor(Func<ActividadParcialEditor, bool> validador, Action<string> bloqueo)
    {
        _validador = validador;
        _bloqueo = bloqueo;
    }

    public void CargarDesdeModelo(ActividadParcial modelo)
    {
        _activa = modelo.Activa;
        _nombre = modelo.Nombre;
        _porcentaje = modelo.Porcentaje;
        _puntajeMaximo = modelo.PuntajeMaximo;
        _puntajeObtenido = 0;

        _activaAnterior = _activa;
        _nombreAnterior = _nombre;
        _porcentajeAnterior = _porcentaje;
        _puntajeMaximoAnterior = _puntajeMaximo;
        _puntajeObtenidoAnterior = _puntajeObtenido;

        OnPropertyChanged(nameof(Activa));
        OnPropertyChanged(nameof(Nombre));
        OnPropertyChanged(nameof(Porcentaje));
        OnPropertyChanged(nameof(PuntajeMaximo));
        OnPropertyChanged(nameof(PuntajeObtenido));
        OnPropertyChanged(nameof(FraccionTexto));
        OnPropertyChanged(nameof(ContribucionTexto));
    }

    public ActividadParcial ToModelo()
    {
        return new ActividadParcial
        {
            Activa = Activa,
            Nombre = Nombre?.Trim() ?? string.Empty,
            Porcentaje = Porcentaje,
            PuntajeMaximo = PuntajeMaximo
        };
    }

    public string FraccionTexto
    {
        get
        {
            string obtenido = PuntajeObtenido.ToString("0.##", CultureInfo.InvariantCulture);
            string maximo = PuntajeMaximo.ToString("0.##", CultureInfo.InvariantCulture);
            return $"{obtenido} / {maximo}";
        }
    }

    public string ContribucionTexto
    {
        get
        {
            decimal contribucion = CalcularContribucion();
            return contribucion.ToString("0.0", CultureInfo.InvariantCulture);
        }
    }

    private decimal CalcularContribucion()
    {
        if (!Activa || PuntajeMaximo <= 0)
            return 0m;

        decimal obtenido = (decimal)PuntajeObtenido;
        decimal maximo = (decimal)PuntajeMaximo;
        decimal porcentaje = (decimal)Porcentaje;

        decimal contribucion = (obtenido / maximo) * porcentaje;
        return Math.Truncate(contribucion * 10m) / 10m;
    }

    partial void OnActivaChanged(bool value)
    {
        if (!_validador(this))
        {
            _activa = _activaAnterior;
            OnPropertyChanged(nameof(Activa));
            _bloqueo("No puedes pasar de 100% ni activar más actividades si ya se llegó al tope.");
            return;
        }

        _activaAnterior = value;
        OnPropertyChanged(nameof(ContribucionTexto));
    }

    partial void OnNombreChanged(string value)
    {
        _nombreAnterior = value;
    }

    partial void OnPorcentajeChanged(double value)
    {
        if (!_validador(this))
        {
            _porcentaje = _porcentajeAnterior;
            OnPropertyChanged(nameof(Porcentaje));
            OnPropertyChanged(nameof(ContribucionTexto));
            _bloqueo("El porcentaje no puede pasar de 100% en total.");
            return;
        }

        _porcentajeAnterior = value;
        OnPropertyChanged(nameof(ContribucionTexto));
    }

    partial void OnPuntajeMaximoChanged(double value)
    {
        if (!_validador(this))
        {
            _puntajeMaximo = _puntajeMaximoAnterior;
            OnPropertyChanged(nameof(PuntajeMaximo));
            OnPropertyChanged(nameof(FraccionTexto));
            OnPropertyChanged(nameof(ContribucionTexto));
            _bloqueo("El puntaje máximo no puede ser menor que el obtenido.");
            return;
        }

        _puntajeMaximoAnterior = value;
        OnPropertyChanged(nameof(FraccionTexto));
        OnPropertyChanged(nameof(ContribucionTexto));
    }

    partial void OnPuntajeObtenidoChanged(double value)
    {
        if (!_validador(this))
        {
            _puntajeObtenido = _puntajeObtenidoAnterior;
            OnPropertyChanged(nameof(PuntajeObtenido));
            OnPropertyChanged(nameof(FraccionTexto));
            OnPropertyChanged(nameof(ContribucionTexto));
            _bloqueo("El puntaje obtenido no puede ser negativo ni mayor al máximo.");
            return;
        }

        _puntajeObtenidoAnterior = value;
        OnPropertyChanged(nameof(FraccionTexto));
        OnPropertyChanged(nameof(ContribucionTexto));
    }
}