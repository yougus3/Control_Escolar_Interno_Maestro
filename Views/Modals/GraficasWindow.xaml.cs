using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views.Modals
{
    public partial class GraficasWindow : Window
    {
        private readonly List<Alumno> _alumnos = new();

        private readonly int[] _frecuencias =
            new int[11];

        private string _evaluacionActual = "P1";

        private double? _promedio;

        private int _alumnosEvaluados;

        private int _alumnosExcluidos;

        private int _aprobados;

        private int _reprobados;

        public GraficasWindow()
        {
            InitializeComponent();

            Loaded +=
                GraficasWindow_Loaded;
        }

        // ============================================================
        // CARGA
        // ============================================================

        private void GraficasWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            ObtenerEvaluacionActual();

            CargarAlumnos();

            CalcularDatos();

            ActualizarInterfaz();

            DibujarGraficaPromedio();

            DibujarGraficaDistribucion();
        }

        // ============================================================
        // OBTENER EVALUACIÓN ACTUAL
        // ============================================================

        private void ObtenerEvaluacionActual()
        {
            object? origen =
                Owner?.DataContext;

            object? valor =
                ObtenerPropiedad(
                    origen,
                    "NombreEvaluacionSeleccionada");

            if (valor == null)
            {
                object? parcialesVm =
                    ObtenerPropiedad(
                        origen,
                        "ParcialesVm");

                valor =
                    ObtenerPropiedad(
                        parcialesVm,
                        "NombreEvaluacionSeleccionada");
            }

            string texto =
                valor?.ToString()?.Trim() ?? string.Empty;

            _evaluacionActual =
                NormalizarEvaluacion(texto);

            TbEvaluacion.Text =
                ObtenerNombreEvaluacion(
                    _evaluacionActual);
        }

        private static string NormalizarEvaluacion(
            string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return "P1";

            string valor =
                texto.Trim()
                     .ToUpperInvariant();

            if (valor == "P1" ||
                valor.Contains("PARCIAL 1"))
            {
                return "P1";
            }

            if (valor == "P2" ||
                valor.Contains("PARCIAL 2"))
            {
                return "P2";
            }

            if (valor == "P3" ||
                valor.Contains("PARCIAL 3"))
            {
                return "P3";
            }

            if (valor == "SEM" ||
                valor.Contains("SEMESTRAL"))
            {
                return "SEM";
            }

            if (valor == "PRE" ||
                valor.Contains("PREEXTRAORDINARIO"))
            {
                return "PRE";
            }

            if (valor == "EXTRA" ||
                valor.Contains("EXTRAORDINARIO"))
            {
                return "EXTRA";
            }

            return "P1";
        }

        private static string ObtenerNombreEvaluacion(
            string id)
        {
            return id switch
            {
                "P1" => "PARCIAL 1",
                "P2" => "PARCIAL 2",
                "P3" => "PARCIAL 3",
                "SEM" => "SEMESTRAL",
                "PRE" => "PREEXTRAORDINARIO",
                "EXTRA" => "EXTRAORDINARIO",
                _ => id
            };
        }

        // ============================================================
        // OBTENER ALUMNOS
        // ============================================================

        private void CargarAlumnos()
        {
            _alumnos.Clear();

            object? origen =
                Owner?.DataContext;

            IEnumerable? enumerable =
                BuscarAlumnos(
                    origen);

            if (enumerable == null)
            {
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
                        enumerable =
                            BuscarAlumnos(
                                ventana.DataContext);
                    }
                }
                catch
                {
                }
            }

            if (enumerable == null)
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

                _alumnos.Add(alumno);
            }
        }

        private static IEnumerable? BuscarAlumnos(
            object? origen)
        {
            if (origen == null)
                return null;

            try
            {
                object? alumnos =
                    ObtenerPropiedad(
                        origen,
                        "Alumnos");

                if (alumnos is IEnumerable enumerable)
                    return enumerable;
            }
            catch
            {
            }

            try
            {
                object? parcialesVm =
                    ObtenerPropiedad(
                        origen,
                        "ParcialesVm");

                if (parcialesVm != null)
                {
                    object? alumnos =
                        ObtenerPropiedad(
                            parcialesVm,
                            "Alumnos");

                    if (alumnos is IEnumerable enumerable)
                        return enumerable;
                }
            }
            catch
            {
            }

            return null;
        }

        // ============================================================
        // REFLEXIÓN
        // ============================================================

        private static object? ObtenerPropiedad(
            object? objeto,
            string nombre)
        {
            if (objeto == null)
                return null;

            try
            {
                PropertyInfo? propiedad =
                    objeto.GetType()
                          .GetProperty(
                              nombre,
                              BindingFlags.Public |
                              BindingFlags.Instance |
                              BindingFlags.IgnoreCase);

                return propiedad?.GetValue(objeto);
            }
            catch
            {
                return null;
            }
        }

        // ============================================================
        // CÁLCULO DE DATOS
        // ============================================================

        private void CalcularDatos()
        {
            Array.Clear(
                _frecuencias,
                0,
                _frecuencias.Length);

            _promedio = null;

            _alumnosEvaluados = 0;

            _alumnosExcluidos = 0;

            _aprobados = 0;

            _reprobados = 0;

            var calificaciones =
                new List<double>();

            foreach (Alumno alumno in _alumnos)
            {
                string texto;

                try
                {
                    texto =
                        alumno.Calificación[_evaluacionActual];
                }
                catch
                {
                    _alumnosExcluidos++;
                    continue;
                }

                if (!TryObtenerCalificacion(
                        texto,
                        out double calificacion))
                {
                    _alumnosExcluidos++;
                    continue;
                }

                calificacion =
                    Math.Clamp(
                        calificacion,
                        0.0,
                        10.0);

                calificaciones.Add(
                    calificacion);

                _alumnosEvaluados++;

                // ====================================================
                // APROBADOS / REPROBADOS
                // ====================================================

                if (calificacion >= 7.0)
                {
                    _aprobados++;
                }
                else
                {
                    _reprobados++;
                }

                // ====================================================
                // DISTRIBUCIÓN
                // ====================================================

                int indice;

                if (calificacion >= 10.0)
                {
                    indice = 10;
                }
                else
                {
                    indice =
                        (int)Math.Truncate(
                            calificacion);

                    indice =
                        Math.Clamp(
                            indice,
                            0,
                            10);
                }

                _frecuencias[indice]++;
            }

            if (calificaciones.Count > 0)
            {
                _promedio =
                    calificaciones.Average();
            }
        }

        private static bool TryObtenerCalificacion(
            string? texto,
            out double valor)
        {
            valor = 0;

            if (string.IsNullOrWhiteSpace(texto))
                return false;

            string normalizado =
                texto.Trim()
                     .Replace(
                         ',',
                         '.');

            return double.TryParse(
                normalizado,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out valor) &&
                   !double.IsNaN(valor) &&
                   !double.IsInfinity(valor);
        }

        // ============================================================
        // INTERFAZ
        // ============================================================

        private void ActualizarInterfaz()
        {
            TbAlumnosEvaluados.Text =
                _alumnosEvaluados.ToString(
                    CultureInfo.InvariantCulture);

            if (_alumnosEvaluados > 0)
            {
                double porcentajeAprobados =
                    (double)_aprobados /
                    _alumnosEvaluados *
                    100.0;

                double porcentajeReprobados =
                    (double)_reprobados /
                    _alumnosEvaluados *
                    100.0;

                TbAprobados.Text =
                    $"{_aprobados} ({porcentajeAprobados:0.00}%)";

                TbReprobados.Text =
                    $"{_reprobados} ({porcentajeReprobados:0.00}%)";
            }
            else
            {
                TbAprobados.Text =
                    "0 (0.00%)";

                TbReprobados.Text =
                    "0 (0.00%)";
            }

            if (_promedio.HasValue)
            {
                TbPromedio.Text =
                    _promedio.Value.ToString(
                        "0.00",
                        CultureInfo.InvariantCulture);

                TbPromedioDetalle.Text =
                    $"Promedio calculado con {_alumnosEvaluados} alumno(s).";
            }
            else
            {
                TbPromedio.Text =
                    "--";

                TbPromedioDetalle.Text =
                    "Sin calificaciones numéricas.";
            }
        }

        // ============================================================
        // GRÁFICA DEL PROMEDIO
        // ============================================================

        private void CanvasPromedio_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            DibujarGraficaPromedio();
        }

        private void DibujarGraficaPromedio()
        {
            if (CanvasPromedio == null)
                return;

            CanvasPromedio.Children.Clear();

            double ancho =
                CanvasPromedio.ActualWidth;

            double alto =
                CanvasPromedio.ActualHeight;

            if (ancho <= 0 ||
                alto <= 0)
            {
                return;
            }

            double izquierda = 24;
            double derecha = 24;

            double anchoBarra =
                Math.Max(
                    100,
                    ancho -
                    izquierda -
                    derecha);

            double y =
                Math.Min(
                    95,
                    alto / 2.0);

            double grosor = 24;

            // ========================================================
            // LÍNEA BASE
            // ========================================================

            var baseLine =
                new Rectangle
                {
                    Width = anchoBarra,
                    Height = grosor,
                    Fill =
                        new SolidColorBrush(
                            Color.FromRgb(
                                226,
                                232,
                                240)),
                    RadiusX = 8,
                    RadiusY = 8
                };

            Canvas.SetLeft(
                baseLine,
                izquierda);

            Canvas.SetTop(
                baseLine,
                y - grosor / 2);

            CanvasPromedio.Children.Add(
                baseLine);

            // ========================================================
            // VALOR DEL PROMEDIO
            // ========================================================

            if (_promedio.HasValue)
            {
                double promedio =
                    Math.Clamp(
                        _promedio.Value,
                        0.0,
                        10.0);

                double porcentaje =
                    promedio / 10.0;

                double anchoLleno =
                    anchoBarra *
                    porcentaje;

                var relleno =
                    new Rectangle
                    {
                        Width =
                            Math.Max(
                                0,
                                anchoLleno),
                        Height = grosor,
                        Fill =
                            new SolidColorBrush(
                                Color.FromRgb(
                                    27,
                                    54,
                                    93)),
                        RadiusX = 8,
                        RadiusY = 8
                    };

                Canvas.SetLeft(
                    relleno,
                    izquierda);

                Canvas.SetTop(
                    relleno,
                    y - grosor / 2);

                CanvasPromedio.Children.Add(
                    relleno);

                // ====================================================
                // MARCADOR
                // ====================================================

                double xMarcador =
                    izquierda +
                    anchoBarra *
                    porcentaje;

                var marcador =
                    new Ellipse
                    {
                        Width = 14,
                        Height = 14,
                        Fill =
                            new SolidColorBrush(
                                Colors.White),
                        Stroke =
                            new SolidColorBrush(
                                Color.FromRgb(
                                    27,
                                    54,
                                    93)),
                        StrokeThickness = 3
                    };

                Canvas.SetLeft(
                    marcador,
                    xMarcador - 7);

                Canvas.SetTop(
                    marcador,
                    y - 7);

                CanvasPromedio.Children.Add(
                    marcador);

                // ====================================================
                // TEXTO DEL PROMEDIO
                // ====================================================

                var texto =
                    new TextBlock
                    {
                        Text =
                            promedio.ToString(
                                "0.00",
                                CultureInfo.InvariantCulture),
                        FontSize = 18,
                        FontWeight = FontWeights.Bold,
                        Foreground =
                            new SolidColorBrush(
                                Color.FromRgb(
                                    27,
                                    54,
                                    93)),
                        Background =
                            new SolidColorBrush(
                                Colors.White),
                        Padding =
                            new Thickness(
                                6,
                                2,
                                6,
                                2)
                    };

                Canvas.SetLeft(
                    texto,
                    Math.Max(
                        0,
                        Math.Min(
                            ancho - 75,
                            xMarcador - 32)));

                Canvas.SetTop(
                    texto,
                    y - 58);

                CanvasPromedio.Children.Add(
                    texto);
            }

            // ========================================================
            // ESCALA 0 - 5 - 10
            // ========================================================

            AgregarTextoCanvas(
                CanvasPromedio,
                "0",
                izquierda - 4,
                y + 18,
                12,
                "#64748B");

            AgregarTextoCanvas(
                CanvasPromedio,
                "5",
                izquierda +
                anchoBarra / 2 -
                4,
                y + 18,
                12,
                "#64748B");

            AgregarTextoCanvas(
                CanvasPromedio,
                "10",
                izquierda +
                anchoBarra -
                9,
                y + 18,
                12,
                "#64748B");

            // ========================================================
            // LÍNEAS VERTICALES DE REFERENCIA
            // ========================================================

            AgregarLineaCanvas(
                CanvasPromedio,
                izquierda,
                y - 32,
                izquierda,
                y + 8,
                "#CBD5E1",
                1);

            AgregarLineaCanvas(
                CanvasPromedio,
                izquierda +
                anchoBarra / 2,
                y - 32,
                izquierda +
                anchoBarra / 2,
                y + 8,
                "#CBD5E1",
                1);

            AgregarLineaCanvas(
                CanvasPromedio,
                izquierda +
                anchoBarra,
                y - 32,
                izquierda +
                anchoBarra,
                y + 8,
                "#CBD5E1",
                1);
        }

        // ============================================================
        // GRÁFICA DE DISTRIBUCIÓN
        // ============================================================

        private void CanvasDistribucion_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            DibujarGraficaDistribucion();
        }

        private void DibujarGraficaDistribucion()
        {
            if (CanvasDistribucion == null)
                return;

            CanvasDistribucion.Children.Clear();

            double ancho =
                CanvasDistribucion.ActualWidth;

            double alto =
                CanvasDistribucion.ActualHeight;

            if (ancho <= 0 ||
                alto <= 0)
            {
                return;
            }

            const double margenIzquierdo = 52;
            const double margenDerecho = 18;
            const double margenSuperior = 16;
            const double margenInferior = 42;

            double anchoGrafica =
                ancho -
                margenIzquierdo -
                margenDerecho;

            double altoGrafica =
                alto -
                margenSuperior -
                margenInferior;

            if (anchoGrafica <= 0 ||
                altoGrafica <= 0)
            {
                return;
            }

            int maxFrecuencia =
                _frecuencias.Max();

            if (maxFrecuencia <= 0)
            {
                AgregarTextoCanvas(
                    CanvasDistribucion,
                    "Sin calificaciones numéricas",
                    margenIzquierdo + 20,
                    margenSuperior + altoGrafica / 2 - 10,
                    13,
                    "#64748B");

                return;
            }

            // ========================================================
            // ESCALA VERTICAL
            // ========================================================

            int paso =
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        maxFrecuencia / 5.0));

            int maxEscala =
                Math.Max(
                    paso,
                    paso *
                    (int)Math.Ceiling(
                        maxFrecuencia /
                        (double)paso));

            double altoPorUnidad =
                altoGrafica /
                maxEscala;

            // ========================================================
            // LÍNEAS HORIZONTALES
            // ========================================================

            for (int valor = 0;
                 valor <= maxEscala;
                 valor += paso)
            {
                double y =
                    margenSuperior +
                    altoGrafica -
                    valor *
                    altoPorUnidad;

                AgregarLineaCanvas(
                    CanvasDistribucion,
                    margenIzquierdo,
                    y,
                    margenIzquierdo +
                    anchoGrafica,
                    y,
                    "#E2E8F0",
                    1);

                AgregarTextoCanvas(
                    CanvasDistribucion,
                    valor.ToString(
                        CultureInfo.InvariantCulture),
                    4,
                    y - 8,
                    11,
                    "#64748B");
            }

            // ========================================================
            // EJE VERTICAL
            // ========================================================

            AgregarLineaCanvas(
                CanvasDistribucion,
                margenIzquierdo,
                margenSuperior,
                margenIzquierdo,
                margenSuperior +
                altoGrafica,
                "#94A3B8",
                1.2);

            // ========================================================
            // EJE HORIZONTAL
            // ========================================================

            AgregarLineaCanvas(
                CanvasDistribucion,
                margenIzquierdo,
                margenSuperior +
                altoGrafica,
                margenIzquierdo +
                anchoGrafica,
                margenSuperior +
                altoGrafica,
                "#94A3B8",
                1.2);

            // ========================================================
            // BARRAS
            // ========================================================

            const int cantidadBarras = 11;

            double espacio =
                anchoGrafica /
                cantidadBarras;

            double anchoBarra =
                Math.Max(
                    10,
                    espacio * 0.62);

            for (int i = 0;
                 i < cantidadBarras;
                 i++)
            {
                int frecuencia =
                    _frecuencias[i];

                double altura =
                    frecuencia *
                    altoPorUnidad;

                double x =
                    margenIzquierdo +
                    i * espacio +
                    (espacio -
                     anchoBarra) /
                    2;

                double y =
                    margenSuperior +
                    altoGrafica -
                    altura;

                if (frecuencia > 0)
                {
                    var barra =
                        new Rectangle
                        {
                            Width =
                                anchoBarra,
                            Height =
                                Math.Max(
                                    1,
                                    altura),
                            Fill =
                                new SolidColorBrush(
                                    Color.FromRgb(
                                        27,
                                        54,
                                        93)),
                            RadiusX = 4,
                            RadiusY = 4,
                            ToolTip =
                                $"Calificación {i}: {frecuencia} alumno(s)"
                        };

                    Canvas.SetLeft(
                        barra,
                        x);

                    Canvas.SetTop(
                        barra,
                        y);

                    CanvasDistribucion.Children.Add(
                        barra);

                    // =================================================
                    // NÚMERO SOBRE LA BARRA
                    // =================================================

                    var cantidad =
                        new TextBlock
                        {
                            Text =
                                frecuencia.ToString(
                                    CultureInfo.InvariantCulture),
                            FontSize = 11,
                            FontWeight =
                                FontWeights.SemiBold,
                            Foreground =
                                new SolidColorBrush(
                                    Color.FromRgb(
                                        27,
                                        54,
                                        93)),
                            HorizontalAlignment =
                                HorizontalAlignment.Center
                        };

                    Canvas.SetLeft(
                        cantidad,
                        x +
                        anchoBarra / 2 -
                        5);

                    Canvas.SetTop(
                        cantidad,
                        y - 18);

                    CanvasDistribucion.Children.Add(
                        cantidad);
                }

                // =====================================================
                // ETIQUETA DE CALIFICACIÓN
                // =====================================================

                var etiqueta =
                    new TextBlock
                    {
                        Text =
                            i.ToString(
                                CultureInfo.InvariantCulture),
                        FontSize = 11,
                        Foreground =
                            new SolidColorBrush(
                                Color.FromRgb(
                                    71,
                                    85,
                                    105)),
                        Width =
                            Math.Max(
                                18,
                                espacio),
                        TextAlignment =
                            TextAlignment.Center
                    };

                Canvas.SetLeft(
                    etiqueta,
                    margenIzquierdo +
                    i * espacio);

                Canvas.SetTop(
                    etiqueta,
                    margenSuperior +
                    altoGrafica +
                    10);

                CanvasDistribucion.Children.Add(
                    etiqueta);
            }

            // ========================================================
            // TÍTULO EJE Y
            // ========================================================

            var ejeY =
                new TextBlock
                {
                    Text = "ALUMNOS",
                    FontSize = 10,
                    FontWeight =
                        FontWeights.Bold,
                    Foreground =
                        new SolidColorBrush(
                            Color.FromRgb(
                                100,
                                116,
                                139)),
                    RenderTransform =
                        new RotateTransform(-90)
                };

            Canvas.SetLeft(
                ejeY,
                2);

            Canvas.SetTop(
                ejeY,
                margenSuperior +
                altoGrafica / 2 +
                30);

            CanvasDistribucion.Children.Add(
                ejeY);
        }

        // ============================================================
        // UTILIDADES DE DIBUJO
        // ============================================================

        private static void AgregarLineaCanvas(
            Canvas canvas,
            double x1,
            double y1,
            double x2,
            double y2,
            string color,
            double grosor)
        {
            var linea =
                new Line
                {
                    X1 = x1,
                    Y1 = y1,
                    X2 = x2,
                    Y2 = y2,
                    Stroke =
                        new SolidColorBrush(
                            (Color)ColorConverter.ConvertFromString(
                                color)),
                    StrokeThickness =
                        grosor
                };

            canvas.Children.Add(
                linea);
        }

        private static void AgregarTextoCanvas(
            Canvas canvas,
            string texto,
            double left,
            double top,
            double fontSize,
            string color)
        {
            var bloque =
                new TextBlock
                {
                    Text = texto,
                    FontSize = fontSize,
                    Foreground =
                        new SolidColorBrush(
                            (Color)ColorConverter.ConvertFromString(
                                color))
                };

            Canvas.SetLeft(
                bloque,
                left);

            Canvas.SetTop(
                bloque,
                top);

            canvas.Children.Add(
                bloque);
        }

        // ============================================================
        // CERRAR
        // ============================================================

        private void Cerrar_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}