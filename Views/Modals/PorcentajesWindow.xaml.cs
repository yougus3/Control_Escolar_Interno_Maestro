using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views.Modals
{
    public partial class PorcentajesWindow : Window
    {
        private readonly Dictionary<string, ParcialInfo> _parciales;

        private readonly List<AlumnoInfo> _alumnos = new();

        private AlumnoInfo? _alumnoActual;

        private bool _inicializando = true;

        public PorcentajesWindow(
            MateriaParcial? parcial1,
            MateriaParcial? parcial2,
            MateriaParcial? parcial3)
        {
            InitializeComponent();

            _parciales =
                new Dictionary<string, ParcialInfo>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ["P1"] = CrearParcialInfo(parcial1),
                    ["P2"] = CrearParcialInfo(parcial2),
                    ["P3"] = CrearParcialInfo(parcial3)
                };

            CmbParcial.DisplayMemberPath =
                nameof(ParcialComboItem.Nombre);

            CmbParcial.SelectedValuePath =
                nameof(ParcialComboItem.Id);

            CmbParcial.ItemsSource =
                new List<ParcialComboItem>
                {
                    new ParcialComboItem
                    {
                        Id = "P1",
                        Nombre = "PARCIAL 1"
                    },
                    new ParcialComboItem
                    {
                        Id = "P2",
                        Nombre = "PARCIAL 2"
                    },
                    new ParcialComboItem
                    {
                        Id = "P3",
                        Nombre = "PARCIAL 3"
                    }
                };

            CmbParcial.SelectionChanged +=
                CmbParcial_SelectionChanged;

            LstAlumnos.SelectionChanged +=
                LstAlumnos_SelectionChanged;

            Loaded +=
                PorcentajesWindow_Loaded;

            _inicializando = false;

            CmbParcial.SelectedValue = "P1";
        }

        private void PapelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.Tag is not AlumnoInfo alumnoInfo)
                return;

            Alumno alumno =
                alumnoInfo.Alumno;

            var datosParciales =
                new Dictionary<
                    string,
                    (
                    string calif,
                    string estado,
                    int faltas,
                    int totalClases
                    )>();

            int totalClasesAcumuladas = 0;
            int faltasAcumuladas = 0;

            foreach (string eval in new[] { "P1", "P2", "P3" })
            {
                string calif =
                    alumno.Calificación[eval];

                string estado =
                    "Sin evaluar";

                int faltas = -1;
                int totalClases = -1;

                if (_parciales.TryGetValue(
                        eval,
                        out ParcialInfo? parcial) &&
                    parcial.Materia != null)
                {
                    bool asistenciaActiva = false;

                    if (
                        parcial.Materia.Calificaciones != null &&
                        parcial.Materia.Calificaciones.TryGetValue(
                            "$CONFIG$",
                            out var config) &&
                        config != null)
                    {
                        asistenciaActiva =
                            config.TryGetValue(
                                "AsistenciaActiva",
                                out var aa) &&
                            aa > 0;

                        if (
                            asistenciaActiva &&
                            config.TryGetValue(
                                "ClasesTotales",
                                out var ct) &&
                            ct > 0)
                        {
                            totalClases =
                                (int)ct;
                        }
                    }

                    if (
                        asistenciaActiva &&
                        parcial.Materia.Calificaciones.TryGetValue(
                            alumno.Matricula,
                            out var capturas))
                    {
                        if (
                            capturas.TryGetValue(
                                "__Inasistencias__",
                                out var f) &&
                            f >= 0)
                        {
                            faltas =
                                (int)f;
                        }
                    }

                    if (
                        double.TryParse(
                            calif,
                            NumberStyles.Any,
                            CultureInfo.InvariantCulture,
                            out double califNum))
                    {
                        if (
                            asistenciaActiva &&
                            totalClases > 0)
                        {
                            totalClasesAcumuladas +=
                                totalClases;

                            if (faltas >= 0)
                                faltasAcumuladas +=
                                    faltas;

                            int asistencias =
                                totalClases -
                                (faltas >= 0
                                    ? faltas
                                    : 0);

                            double porcentajeAsistencia =
                                (double)asistencias /
                                totalClases *
                                100.0;

                            if (porcentajeAsistencia < 80)
                            {
                                estado = "NP";
                            }
                            else
                            {
                                estado =
                                    califNum >= 7.0
                                        ? "Aprobado"
                                        : "Reprobado";
                            }
                        }
                        else
                        {
                            estado =
                                califNum >= 7.0
                                    ? "Aprobado"
                                    : "Reprobado";
                        }
                    }
                }

                datosParciales[eval] =
                (
                    calif,
                    estado,
                    faltas,
                    totalClases
                );
            }

            // =========================================================
            // SEMESTRAL
            // =========================================================

            string califSem =
                alumno.Calificación["SEM"];

            string estadoSem =
                "Sin evaluar";

            int faltasSem = -1;
            int totalClasesSem = -1;

            if (
                double.TryParse(
                    califSem,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double califSemNum))
            {
                totalClasesSem =
                    totalClasesAcumuladas;

                faltasSem =
                    faltasAcumuladas;

                if (totalClasesSem > 0)
                {
                    double porcentajeAsistencia =
                        (double)(
                            totalClasesSem -
                            faltasSem) /
                        totalClasesSem *
                        100.0;

                    if (porcentajeAsistencia < 80)
                    {
                        estadoSem = "NP";
                    }
                    else
                    {
                        estadoSem =
                            califSemNum >= 7.0
                                ? "Aprobado"
                                : "Reprobado";
                    }
                }
                else
                {
                    estadoSem =
                        califSemNum >= 7.0
                            ? "Aprobado"
                            : "Reprobado";
                }
            }

            datosParciales["SEM"] =
            (
                califSem,
                estadoSem,
                faltasSem,
                totalClasesSem
            );

            var infoWindow =
                new InfoAlumnoWindow(
                    alumno,
                    datosParciales)
                {
                    Owner = this
                };

            infoWindow.ShowDialog();
        }


        private void PorcentajesWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            CargarListaAlumnos();

            LstAlumnos.ItemsSource =
                _alumnos;

            if (_alumnos.Count > 0)
            {
                _alumnoActual =
                    _alumnos[0];

                LstAlumnos.SelectedIndex = 0;

                MostrarAlumnoSeleccionado();
            }
            else
            {
                MostrarSinAlumno();
            }

            MostrarParcialActual();
        }

        private void CargarListaAlumnos()
        {
            _alumnos.Clear();

            object? origen =
                ObtenerOrigenAlumnos();

            if (origen is not IEnumerable enumerable)
                return;

            foreach (object? item in enumerable)
            {
                if (item is not Alumno alumno)
                    continue;

                if (string.IsNullOrWhiteSpace(
                        alumno.Matricula))
                {
                    continue;
                }

                _alumnos.Add(
                    new AlumnoInfo
                    {
                        Alumno = alumno
                    });
            }
        }

        private object? ObtenerOrigenAlumnos()
        {
            try
            {
                Window? owner = Owner;

                if (owner != null)
                {
                    object? alumnos =
                        BuscarAlumnosEnObjeto(
                            owner.DataContext);

                    if (alumnos != null)
                        return alumnos;
                }
            }
            catch
            {
            }

            try
            {
                Window? ventana =
                    Application.Current?
                        .Windows
                        .OfType<Window>()
                        .FirstOrDefault(w =>
                            w != this &&
                            w.IsVisible);

                if (ventana != null)
                {
                    object? alumnos =
                        BuscarAlumnosEnObjeto(
                            ventana.DataContext);

                    if (alumnos != null)
                        return alumnos;
                }
            }
            catch
            {
            }

            return null;
        }

        private object? BuscarAlumnosEnObjeto(
            object? objeto)
        {
            if (objeto == null)
                return null;

            try
            {
                PropertyInfo? propAlumnos =
                    objeto.GetType()
                        .GetProperty(
                            "Alumnos",
                            BindingFlags.Public |
                            BindingFlags.Instance |
                            BindingFlags.IgnoreCase);

                if (propAlumnos != null)
                {
                    object? valor =
                        propAlumnos.GetValue(
                            objeto);

                    if (valor is IEnumerable)
                        return valor;
                }
            }
            catch
            {
            }

            try
            {
                PropertyInfo? propParcialesVm =
                    objeto.GetType()
                        .GetProperty(
                            "ParcialesVm",
                            BindingFlags.Public |
                            BindingFlags.Instance |
                            BindingFlags.IgnoreCase);

                if (propParcialesVm != null)
                {
                    object? parcialesVm =
                        propParcialesVm.GetValue(
                            objeto);

                    if (parcialesVm != null)
                    {
                        PropertyInfo? propAlumnos =
                            parcialesVm.GetType()
                                .GetProperty(
                                    "Alumnos",
                                    BindingFlags.Public |
                                    BindingFlags.Instance |
                                    BindingFlags.IgnoreCase);

                        if (propAlumnos != null)
                        {
                            object? valor =
                                propAlumnos.GetValue(
                                    parcialesVm);

                            if (valor is IEnumerable)
                                return valor;
                        }
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        private static ParcialInfo CrearParcialInfo(
            MateriaParcial? materia)
        {
            var resultado =
                new ParcialInfo
                {
                    Materia = materia
                };

            if (materia != null)
            {
                resultado.PorcentajeAcumulado =
                    materia.PorcentajeAcumulado;

                if (materia.Calificaciones != null &&
                    materia.Calificaciones.TryGetValue(
                        "$CONFIG$",
                        out var config) &&
                    config != null &&
                    config.TryGetValue(
                        "ClasesTotales",
                        out double clases))
                {
                    resultado.NumeroClases =
                        clases >= 0
                            ? (int)clases
                            : 0;
                }

                if (materia.Actividades != null)
                {
                    resultado.Actividades =
                        materia.Actividades
                            .Take(4)
                            .Select(a =>
                                new ActividadInfo
                                {
                                    Activa =
                                        a?.Activa ??
                                        false,

                                    Nombre =
                                        a?.Nombre ??
                                        string.Empty,

                                    Porcentaje =
                                        a?.Porcentaje ??
                                        0,

                                    PuntajeMaximo =
                                        a?.PuntajeMaximo ??
                                        0
                                })
                            .ToList();
                }
            }

            while (resultado.Actividades.Count < 4)
            {
                resultado.Actividades.Add(
                    new ActividadInfo
                    {
                        Activa = false,
                        Nombre = string.Empty,
                        Porcentaje = 0,
                        PuntajeMaximo = 0
                    });
            }

            return resultado;
        }

        private void CmbParcial_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_inicializando)
                return;

            MostrarParcialActual();
        }

        private void LstAlumnos_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (LstAlumnos.SelectedIndex < 0 ||
                LstAlumnos.SelectedIndex >= _alumnos.Count)
            {
                return;
            }

            _alumnoActual =
                _alumnos[
                    LstAlumnos.SelectedIndex];

            MostrarAlumnoSeleccionado();
            MostrarParcialActual();
        }

        private void MostrarAlumnoSeleccionado()
        {
            if (_alumnoActual == null)
            {
                MostrarSinAlumno();
                return;
            }

            TbAlumnoNombre.Text =
                _alumnoActual.Alumno.Nombre;

            TbAlumnoMatricula.Text =
                _alumnoActual.Alumno.Matricula;
        }

        private void MostrarSinAlumno()
        {
            TbAlumnoNombre.Text =
                "Sin alumno seleccionado";

            TbAlumnoMatricula.Text =
                string.Empty;

            TbCalificacionGeneral.Text =
                "--";
        }

        private void MostrarParcialActual()
        {
            string id =
                CmbParcial.SelectedValue?
                    .ToString() ??
                "P1";

            if (!_parciales.TryGetValue(
                    id,
                    out ParcialInfo? parcial))
            {
                return;
            }

            MostrarActividades(parcial);

            MostrarDatosGenerales(parcial);

            MostrarCalificacionGeneral(id);
        }

        private void MostrarActividades(
            ParcialInfo parcial)
        {
            MostrarActividad(
                parcial.Actividades[0],
                TbActividad1Nombre,
                TbActividad1Porcentaje,
                TbActividad1Maximo,
                TbActividad1Calificacion);

            MostrarActividad(
                parcial.Actividades[1],
                TbActividad2Nombre,
                TbActividad2Porcentaje,
                TbActividad2Maximo,
                TbActividad2Calificacion);

            MostrarActividad(
                parcial.Actividades[2],
                TbActividad3Nombre,
                TbActividad3Porcentaje,
                TbActividad3Maximo,
                TbActividad3Calificacion);

            MostrarActividad(
                parcial.Actividades[3],
                TbActividad4Nombre,
                TbActividad4Porcentaje,
                TbActividad4Maximo,
                TbActividad4Calificacion);
        }

        private void MostrarActividad(
            ActividadInfo? actividad,
            TextBlock nombre,
            TextBlock porcentaje,
            TextBlock maximo,
            TextBlock calificacion)
        {
            if (actividad == null ||
                !actividad.Activa)
            {
                nombre.Text = "NO ACTIVA";
                porcentaje.Text = "0.0%";
                maximo.Text = "Máx. 0";
                calificacion.Text = "—";

                Brush gris =
                    new SolidColorBrush(
                        Color.FromRgb(
                            148,
                            163,
                            184));

                nombre.Foreground = gris;
                porcentaje.Foreground = gris;
                maximo.Foreground = gris;
                calificacion.Foreground = gris;

                return;
            }

            nombre.Text =
                string.IsNullOrWhiteSpace(
                    actividad.Nombre)
                    ? "Sin nombre"
                    : actividad.Nombre.Trim();

            porcentaje.Text =
                FormatearPorcentaje(
                    actividad.Porcentaje);

            maximo.Text =
                $"Máx. {FormatearPuntajeMaximo(actividad.PuntajeMaximo)}";

            calificacion.Text =
                ObtenerCalificacionActividad(
                    actividad);

            nombre.Foreground =
                new SolidColorBrush(
                    Color.FromRgb(
                        51,
                        65,
                        85));

            porcentaje.Foreground =
                new SolidColorBrush(
                    Color.FromRgb(
                        27,
                        54,
                        93));

            maximo.Foreground =
                new SolidColorBrush(
                    Color.FromRgb(
                        100,
                        116,
                        139));

            calificacion.Foreground =
                new SolidColorBrush(
                    Color.FromRgb(
                        27,
                        54,
                        93));
        }

        private string ObtenerCalificacionActividad(
            ActividadInfo actividad)
        {
            if (_alumnoActual?.Alumno == null ||
                string.IsNullOrWhiteSpace(
                    actividad.Nombre))
            {
                return "—";
            }

            if (!TryObtenerParcialActual(
                    out ParcialInfo? parcial) ||
                parcial?.Materia == null)
            {
                return "—";
            }

            if (parcial.Materia.Calificaciones == null)
                return "—";

            string matricula =
                _alumnoActual.Alumno.Matricula.Trim();

            if (!parcial.Materia.Calificaciones.TryGetValue(
                    matricula,
                    out Dictionary<string, double>? capturas) ||
                capturas == null)
            {
                return "—";
            }

            string nombreActividad =
                actividad.Nombre.Trim();

            if (!capturas.TryGetValue(
                    nombreActividad,
                    out double valor))
            {
                return "—";
            }

            if (valor == -1)
                return "SC";

            return FormatearCalificacionActividad(
                valor);
        }

        private static string FormatearCalificacionActividad(
            double valor)
        {
            decimal truncado =
                Math.Truncate(
                    (decimal)valor * 10m) /
                10m;

            if (truncado ==
                Math.Truncate(truncado))
            {
                return truncado.ToString(
                    "0",
                    CultureInfo.InvariantCulture);
            }

            return truncado.ToString(
                "0.0",
                CultureInfo.InvariantCulture);
        }

        private void MostrarDatosGenerales(
            ParcialInfo parcial)
        {
            TbNumeroClases.Text =
                parcial.NumeroClases > 0
                    ? parcial.NumeroClases.ToString(
                        CultureInfo.InvariantCulture)
                    : "0";

            TbPorcentajeAcumulado.Text =
                FormatearPorcentaje(
                    parcial.PorcentajeAcumulado);
        }

        private void MostrarCalificacionGeneral(
            string idParcial)
        {
            string calificacion =
                ObtenerCalificacionGeneral(
                    idParcial);

            TbCalificacionGeneral.Text =
                calificacion;

            if (EsReprobatoria(calificacion))
            {
                TbCalificacionGeneral.Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(
                            220,
                            38,
                            38));

                TbCalificacionGeneral.TextDecorations =
                    TextDecorations.Underline;
            }
            else
            {
                TbCalificacionGeneral.Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(
                            27,
                            54,
                            93));

                TbCalificacionGeneral.TextDecorations =
                    null;
            }
        }

        private string ObtenerCalificacionGeneral(
            string idParcial)
        {
            if (_alumnoActual?.Alumno == null)
                return "--";

            string valor =
                _alumnoActual
                    .Alumno
                    .Calificación[idParcial];

            return string.IsNullOrWhiteSpace(valor)
                ? "--"
                : valor.Trim();
        }

        private bool TryObtenerParcialActual(
            out ParcialInfo? parcial)
        {
            string id =
                CmbParcial.SelectedValue?
                    .ToString() ??
                "P1";

            return _parciales.TryGetValue(
                id,
                out parcial);
        }

        private static bool EsReprobatoria(
            string valor)
        {
            if (string.IsNullOrWhiteSpace(valor) ||
                valor == "--")
            {
                return false;
            }

            if (!double.TryParse(
                    valor,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double d))
            {
                return false;
            }

            return d < 6.0;
        }

        private static string FormatearPorcentaje(
            double valor)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:0.0}%",
                valor);
        }

        private static string FormatearPuntajeMaximo(
            double valor)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:0.##}",
                valor);
        }

        private void Cerrar_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private sealed class ParcialComboItem
        {
            public string Id { get; set; } =
                string.Empty;

            public string Nombre { get; set; } =
                string.Empty;
        }

        private sealed class AlumnoInfo
        {
            public Alumno Alumno { get; set; } = null;

            public string Matricula =>
                Alumno.Matricula;

            public string Nombre =>
                Alumno.Nombre;
        }

        private sealed class ParcialInfo
        {
            public MateriaParcial? Materia { get; set; }

            public List<ActividadInfo> Actividades { get; set; } =
                new();

            public int NumeroClases { get; set; }

            public double PorcentajeAcumulado { get; set; }
        }

        private sealed class ActividadInfo
        {
            public bool Activa { get; set; }

            public string Nombre { get; set; } =
                string.Empty;

            public double Porcentaje { get; set; }

            public double PuntajeMaximo { get; set; }
        }
    }
}