using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views.Modals
{
    public partial class PorcentajesWindow : Window
    {
        private readonly Dictionary<string, ParcialInfo> _parciales;

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

            _inicializando = false;

            CmbParcial.SelectedValue = "P1";

            if (_parciales.TryGetValue(
                    "P1",
                    out ParcialInfo? parcial))
            {
                MostrarParcial(parcial);
            }
        }

        // ============================================================
        // CONSTRUIR INFORMACIÓN DE UN PARCIAL
        // ============================================================

        private static ParcialInfo CrearParcialInfo(
            MateriaParcial? materia)
        {
            var resultado = new ParcialInfo
            {
                Actividades = new List<ActividadInfo>()
            };

            if (materia?.Actividades != null)
            {
                resultado.Actividades =
                    materia.Actividades
                        .Take(4)
                        .Select(a => new ActividadInfo
                        {
                            Activa = a?.Activa ?? false,
                            Nombre = a?.Nombre ?? string.Empty,
                            Porcentaje = a?.Porcentaje ?? 0,
                            PuntajeMaximo = a?.PuntajeMaximo ?? 0
                        })
                        .ToList();
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

            if (resultado.Actividades.Count > 4)
            {
                resultado.Actividades =
                    resultado.Actividades
                        .Take(4)
                        .ToList();
            }

            if (materia == null)
                return resultado;

            resultado.PorcentajeAcumulado =
                materia.PorcentajeAcumulado;

            // ========================================================
            // NÚMERO DE CLASES
            // ========================================================

            if (materia.Calificaciones != null &&
                materia.Calificaciones.TryGetValue(
                    "$CONFIG$",
                    out var config))
            {
                if (config != null &&
                    config.TryGetValue(
                        "ClasesTotales",
                        out double clases))
                {
                    resultado.NumeroClases =
                        clases >= 0
                            ? (int)clases
                            : 0;
                }
            }

            return resultado;
        }

        // ============================================================
        // CAMBIO DE PARCIAL
        // ============================================================

        private void CmbParcial_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_inicializando)
                return;

            if (CmbParcial.SelectedValue == null)
                return;

            string id =
                CmbParcial.SelectedValue
                    .ToString()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(id))
                return;

            if (_parciales.TryGetValue(
                    id,
                    out ParcialInfo? parcial) &&
                parcial != null)
            {
                MostrarParcial(parcial);
            }
        }

        // ============================================================
        // MOSTRAR PARCIAL
        // ============================================================

        private void MostrarParcial(
            ParcialInfo parcial)
        {
            if (parcial == null)
                return;

            while (parcial.Actividades.Count < 4)
            {
                parcial.Actividades.Add(
                    new ActividadInfo
                    {
                        Activa = false,
                        Nombre = string.Empty,
                        Porcentaje = 0,
                        PuntajeMaximo = 0
                    });
            }

            MostrarActividad(
                parcial.Actividades[0],
                TbActividad1Nombre,
                TbActividad1Porcentaje,
                TbActividad1Maximo);

            MostrarActividad(
                parcial.Actividades[1],
                TbActividad2Nombre,
                TbActividad2Porcentaje,
                TbActividad2Maximo);

            MostrarActividad(
                parcial.Actividades[2],
                TbActividad3Nombre,
                TbActividad3Porcentaje,
                TbActividad3Maximo);

            MostrarActividad(
                parcial.Actividades[3],
                TbActividad4Nombre,
                TbActividad4Porcentaje,
                TbActividad4Maximo);

            TbNumeroClases.Text =
                parcial.NumeroClases > 0
                    ? parcial.NumeroClases.ToString(
                        CultureInfo.InvariantCulture)
                    : "0";

            TbPorcentajeAcumulado.Text =
                FormatearPorcentaje(
                    parcial.PorcentajeAcumulado);
        }

        // ============================================================
        // MOSTRAR ACTIVIDAD
        // ============================================================

        private static void MostrarActividad(
            ActividadInfo? actividad,
            TextBlock nombre,
            TextBlock porcentaje,
            TextBlock maximo)
        {
            if (actividad == null ||
                !actividad.Activa)
            {
                nombre.Text = "No activa";
                porcentaje.Text = "0.0%";
                maximo.Text = "Máx. 0";

                nombre.Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(
                            148,
                            163,
                            184));

                porcentaje.Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(
                            148,
                            163,
                            184));

                maximo.Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(
                            148,
                            163,
                            184));

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
        }

        // ============================================================
        // FORMATEAR PORCENTAJE
        // ============================================================

        private static string FormatearPorcentaje(
            double valor)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:0.0}%",
                valor);
        }

        // ============================================================
        // FORMATEAR PUNTAJE MÁXIMO
        // ============================================================

        private static string FormatearPuntajeMaximo(
            double valor)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:0.##}",
                valor);
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

        // ============================================================
        // MODELOS INTERNOS
        // ============================================================

        private sealed class ParcialComboItem
        {
            public string Id { get; set; } =
                string.Empty;

            public string Nombre { get; set; } =
                string.Empty;
        }

        private sealed class ParcialInfo
        {
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
