using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.ViewModels;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views.Modals;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views;

public partial class ParcialesView : UserControl
{
    public ParcialesView()
    {
        InitializeComponent();
    }

    private void PuntajeObtenido_KeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        if (DataContext is not ParcialesViewModel vm)
            return;

        if (sender is not TextBox tb)
            return;

        vm.PrepararGuardado();

        var actividad =
            tb.DataContext as ActividadParcialEditor;

        int actividadIndex = -1;

        if (actividad != null)
        {
            actividadIndex =
                vm.Actividades.IndexOf(actividad);
        }

        int nextIndex = -1;

        for (
            int i = actividadIndex + 1;
            i < vm.Actividades.Count;
            i++)
        {
            if (vm.Actividades[i].Activa)
            {
                nextIndex = i;
                break;
            }
        }

        if (nextIndex >= 0)
        {
            var container =
                PuntajesItemsControl
                    .ItemContainerGenerator
                    .ContainerFromIndex(nextIndex)
                    as FrameworkElement;

            if (container != null)
            {
                var nextTb =
                    FindVisualChild<TextBox>(
                        container);

                if (nextTb != null)
                {
                    nextTb.Focus();
                    nextTb.SelectAll();
                }
            }

            e.Handled = true;
            return;
        }

        var alumnos =
            vm.Alumnos;

        if (
            vm.AlumnoSeleccionado != null &&
            alumnos.Any())
        {
            int idx =
                alumnos.IndexOf(
                    vm.AlumnoSeleccionado);

            if (idx < alumnos.Count - 1)
            {
                vm.AlumnoSeleccionado =
                    alumnos[idx + 1];

                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        for (
                            int i = 0;
                            i < vm.Actividades.Count;
                            i++)
                        {
                            if (!vm.Actividades[i].Activa)
                                continue;

                            var container =
                                PuntajesItemsControl
                                    .ItemContainerGenerator
                                    .ContainerFromIndex(i)
                                    as FrameworkElement;

                            if (container == null)
                                continue;

                            var tb2 =
                                FindVisualChild<TextBox>(
                                    container);

                            if (tb2 != null)
                            {
                                tb2.Focus();
                                tb2.SelectAll();
                                break;
                            }
                        }
                    }));
            }
            else
            {
                MessageBox.Show(
                    "Se evaluaron todos los alumnos.",
                    "Fin de captura",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        e.Handled = true;
    }

    private void Puntaje_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (DataContext is ParcialesViewModel vm)
            vm.MarkUserEdited();
    }

    private void CalificacionParcial_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            string normalized =
                NormalizeToSingleDecimalRange(
                    tb.Text);

            if (tb.Text != normalized)
            {
                int sel =
                    tb.SelectionStart;

                tb.Text =
                    normalized;

                tb.SelectionStart =
                    Math.Min(
                        sel,
                        tb.Text.Length);
            }
        }

        if (DataContext is ParcialesViewModel vm)
            vm.MarkUserEdited();
    }

    private static string NormalizeToSingleDecimalRange(
        string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        if (
            input.Trim()
                .ToUpperInvariant() == "SC")
        {
            return "SC";
        }

        if (
            input.Trim()
                .ToUpperInvariant() == "S")
        {
            return "S";
        }

        string s =
            input.Trim()
                .Replace(
                    ',',
                    '.');

        var sb =
            new System.Text.StringBuilder();

        foreach (char c in s)
        {
            if (char.IsDigit(c) ||
                c == '.')
            {
                sb.Append(c);
            }
        }

        s =
            sb.ToString();

        int firstDot =
            s.IndexOf('.');

        if (firstDot >= 0)
        {
            s =
                s.Substring(
                    0,
                    firstDot + 1)
                +
                s.Substring(
                    firstDot + 1)
                    .Replace(
                        ".",
                        "");

            int decimals =
                s.Length -
                firstDot -
                1;

            if (decimals > 1)
            {
                s =
                    s.Substring(
                        0,
                        firstDot + 2);
            }
        }

        if (
            double.TryParse(
                s,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double val))
        {
            if (val < 0)
                val = 0;

            if (val > 10)
                val = 10;

            return val % 1 == 0
                ? ((int)val).ToString(
                    System.Globalization.CultureInfo.InvariantCulture)
                : val.ToString(
                    "0.#",
                    System.Globalization.CultureInfo.InvariantCulture);
        }

        return s;
    }

    private static T? FindVisualChild<T>(
        DependencyObject parent)
        where T : DependencyObject
    {
        if (parent == null)
            return null;

        int children =
            System.Windows.Media.VisualTreeHelper
                .GetChildrenCount(parent);

        for (int i = 0; i < children; i++)
        {
            var child =
                System.Windows.Media.VisualTreeHelper
                    .GetChild(
                        parent,
                        i);

            if (child is T t)
                return t;

            var result =
                FindVisualChild<T>(
                    child);

            if (result != null)
                return result;
        }

        return null;
    }

    private void UserControl_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is MainViewModel mainVm)
        {
            DataContext =
                mainVm.ParcialesVm;
        }
    }

    // =========================================================
    // CAMPOS DE CONFIGURACIÓN
    // =========================================================

    private void Numeros_PreviewTextInput(
        object sender,
        TextCompositionEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        string textoPropuesto =
            textBox.Text
                .Remove(
                    textBox.SelectionStart,
                    textBox.SelectionLength)
                .Insert(
                    textBox.SelectionStart,
                    e.Text);

        if (e.Text == ",")
        {
            textoPropuesto =
                textoPropuesto.Replace(
                    ',',
                    '.');
        }

        Regex regex =
            new Regex(
                @"^[0-9]*\.?[0-9]*$");

        e.Handled =
            !regex.IsMatch(
                textoPropuesto);
    }

    private void Numeros_Pasting(
        object sender,
        DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(
                typeof(string)))
        {
            e.CancelCommand();
            return;
        }

        if (sender is not TextBox textBox)
            return;

        string textoAPegar =
            (string)e.DataObject.GetData(
                typeof(string));

        textoAPegar =
            textoAPegar.Replace(
                ',',
                '.');

        string textoPropuesto =
            textBox.Text
                .Remove(
                    textBox.SelectionStart,
                    textBox.SelectionLength)
                .Insert(
                    textBox.SelectionStart,
                    textoAPegar);

        Regex regex =
            new Regex(
                @"^[0-9]*\.?[0-9]*$");

        if (!regex.IsMatch(
                textoPropuesto))
        {
            e.CancelCommand();
        }
    }

    // =========================================================
    // PUNTAJE OBTENIDO
    // =========================================================

    private void PuntajeObtenido_PreviewTextInput(
        object sender,
        TextCompositionEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        string textoPropuesto =
            textBox.Text
                .Remove(
                    textBox.SelectionStart,
                    textBox.SelectionLength)
                .Insert(
                    textBox.SelectionStart,
                    e.Text);

        if (e.Text == ",")
        {
            textoPropuesto =
                textoPropuesto.Replace(
                    ',',
                    '.');
        }

        Regex regex =
            new Regex(
                @"^[0-9]*\.?[0-9]*$");

        if (!regex.IsMatch(
                textoPropuesto))
        {
            e.Handled = true;
            return;
        }

        if (!PuntajeDentroDelMaximo(
                textBox,
                textoPropuesto))
        {
            e.Handled = true;
        }
    }

    private void PuntajeObtenido_Pasting(
        object sender,
        DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(
                typeof(string)))
        {
            e.CancelCommand();
            return;
        }

        if (sender is not TextBox textBox)
            return;

        string textoAPegar =
            (string)e.DataObject.GetData(
                typeof(string));

        textoAPegar =
            textoAPegar.Replace(
                ',',
                '.');

        string textoPropuesto =
            textBox.Text
                .Remove(
                    textBox.SelectionStart,
                    textBox.SelectionLength)
                .Insert(
                    textBox.SelectionStart,
                    textoAPegar);

        Regex regex =
            new Regex(
                @"^[0-9]*\.?[0-9]*$");

        if (!regex.IsMatch(
                textoPropuesto))
        {
            e.CancelCommand();
            return;
        }

        if (!PuntajeDentroDelMaximo(
                textBox,
                textoPropuesto))
        {
            e.CancelCommand();
        }
    }

    private static bool PuntajeDentroDelMaximo(
        TextBox textBox,
        string texto)
    {
        if (
            textBox.DataContext
            is not ActividadParcialEditor actividad)
        {
            return true;
        }

        if (
            !double.TryParse(
                actividad.PuntajeMaximo,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double maximo))
        {
            return true;
        }

        if (maximo <= 0)
            return true;

        if (string.IsNullOrWhiteSpace(texto))
            return true;

        if (
            !double.TryParse(
                texto.Replace(
                    ',',
                    '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double valor))
        {
            return true;
        }

        return valor <= maximo;
    }

    // =========================================================
    // ALUMNOS
    // =========================================================

    private void Alumnos_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (
            sender is ListBox listBox &&
            listBox.SelectedItem != null)
        {
            listBox.ScrollIntoView(
                listBox.SelectedItem);
        }
    }

    private void NumerosEnteros_PreviewTextInput(
        object sender,
        TextCompositionEventArgs e)
    {
        Regex regex =
            new Regex(
                "[^0-9]+");

        e.Handled =
            regex.IsMatch(
                e.Text);
    }

    // =========================================================
    // PROPIEDADES DE ACTIVIDADES
    // =========================================================

    private void ActividadProp_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (sender is not TextBox tb)
            return;

        if (
            tb.Name == "PorcBox" &&
            DataContext is ParcialesViewModel vm)
        {
            var actividad =
                tb.DataContext
                as ActividadParcialEditor;

            if (actividad != null)
            {
                double sumaOtros = 0.0;

                foreach (var a in vm.Actividades)
                {
                    if (
                        ReferenceEquals(
                            a,
                            actividad))
                    {
                        continue;
                    }

                    if (!a.Activa)
                        continue;

                    var text =
                        (a.Porcentaje ??
                         string.Empty)
                            .Trim()
                            .Replace(
                                ',',
                                '.');

                    if (
                        double.TryParse(
                            text,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out double val))
                    {
                        sumaOtros += val;
                    }
                }

                string mine =
                    (tb.Text ??
                     string.Empty)
                        .Trim()
                        .Replace(
                            ',',
                            '.');

                if (
                    double.TryParse(
                        mine,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double mineVal))
                {
                    if (
                        sumaOtros +
                        mineVal >
                        100.0)
                    {
                        var warningModal =
                            new WarningWindow(
                                "La suma de los porcentajes no puede superar el 100%. Por favor, ajusta los valores.");

                        warningModal.Owner =
                            Window.GetWindow(
                                this);

                        warningModal.ShowDialog();

                        tb.Text =
                            string.Empty;

                        tb.Focus();

                        return;
                    }
                }
            }
        }

        if (DataContext is ParcialesViewModel vm2)
            vm2.MarkUserEdited();
    }

    private void ActividadActiva_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox)
            return;

        if (
            checkBox.DataContext
            is not ActividadParcialEditor actividad)
        {
            return;
        }

        if (DataContext is not ParcialesViewModel vm)
            return;

        int indice =
            vm.Actividades.IndexOf(
                actividad);

        // ACTIVIDAD 1 SIEMPRE ACTIVA
        if (indice == 0)
        {
            checkBox.IsChecked = true;
            actividad.Activa = true;
            return;
        }

        // AL DESACTIVAR:
        // desaparece de Puntajes obtenidos
        // y se borra el puntaje actual,
        // incluyendo SC.
        if (!actividad.Activa)
        {
            actividad.PuntajeObtenido =
                string.Empty;

            actividad.RefrescarVista();
        }

        vm.MarkUserEdited();
    }

    // =========================================================
    // NO EVALUADOS
    // =========================================================

    private void WarningNoEvaluados_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (
            DataContext is ParcialesViewModel vm &&
            vm.FaltanPorEvaluar)
        {
            var modal =
                new NoEvaluadosWindow(
                    vm.ListaNoEvaluados);

            modal.Owner =
                Window.GetWindow(
                    this);

            modal.ShowDialog();
        }
    }

    // =========================================================
    // INFORMACIÓN DEL ALUMNO
    // =========================================================

    private void PapelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var button =
            sender as Button;

        var alumno =
            button?.Tag as Alumno;

        var viewModel =
            DataContext as ParcialesViewModel;

        if (
            alumno != null &&
            viewModel?.MainVm != null)
        {
            var datosParciales =
                new Dictionary<
                    string,
                    (
                        string calif,
                        string estado,
                        int faltas,
                        int totalClases
                    )>();

            var evaluaciones =
                new[]
                {
                    "P1",
                    "P2",
                    "P3",
                    "SEM"
                };

            string archivoActual =
                viewModel
                    .MainVm
                    .ArchivoCompletoActual;

            string claveMateria =
                ObtenerClaveMateriaDesdeNombreArchivo(
                    archivoActual);

            int totalClasesAcumuladas = 0;
            int faltasAcumuladas = 0;

            foreach (var eval in evaluaciones)
            {
                string calif =
                    alumno.Calificación[eval];

                string estado =
                    "Sin evaluar";

                int faltas = -1;
                int totalClases = -1;

                if (eval != "SEM")
                {
                    string claveMateriaEval =
                        $"{claveMateria}_{eval}";

                    var materia =
                        new ParcialJsonService()
                            .ObtenerMateria(
                                claveMateriaEval);

                    if (materia != null)
                    {
                        bool asistenciaActiva =
                            false;

                        if (
                            materia.Calificaciones
                                .TryGetValue(
                                    "$CONFIG$",
                                    out var config))
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
                            materia.Calificaciones
                                .TryGetValue(
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
                                    100;

                                if (
                                    porcentajeAsistencia <
                                    80)
                                {
                                    estado =
                                        "NP";
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
                }
                else
                {
                    if (
                        double.TryParse(
                            calif,
                            out double califNum))
                    {
                        if (
                            totalClasesAcumuladas >
                            0)
                        {
                            double porcentajeAsistencia =
                                (double)(
                                    totalClasesAcumuladas -
                                    faltasAcumuladas) /
                                totalClasesAcumuladas *
                                100;

                            if (
                                porcentajeAsistencia <
                                80)
                            {
                                estado =
                                    "NP";
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

                        totalClases =
                            totalClasesAcumuladas;

                        faltas =
                            faltasAcumuladas;
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

            var infoWindow =
                new InfoAlumnoWindow(
                    alumno,
                    datosParciales);

            infoWindow.Owner =
                Window.GetWindow(
                    this);

            infoWindow.ShowDialog();
        }
    }

    private string ObtenerClaveMateriaDesdeNombreArchivo(
        string? rutaCompleta)
    {
        if (
            string.IsNullOrWhiteSpace(
                rutaCompleta))
        {
            return string.Empty;
        }

        try
        {
            var nombre =
                Path.GetFileNameWithoutExtension(
                    rutaCompleta);

            return string.IsNullOrWhiteSpace(
                nombre)
                ? string.Empty
                : nombre
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

    // =========================================================
    // SC
    // =========================================================

    private void CampoSc_GotFocus(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not TextBox tb)
            return;

        if (
            string.Equals(
                tb.Text?.Trim(),
                "SC",
                StringComparison.OrdinalIgnoreCase))
        {
            tb.Clear();
        }
    }

    private void CampoSc_LostFocus(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not TextBox tb)
            return;

        if (string.IsNullOrWhiteSpace(
                tb.Text))
        {
            tb.Text = "SC";
        }
    }
}