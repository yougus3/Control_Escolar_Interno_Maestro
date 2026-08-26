using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.ViewModels;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views;

public partial class SemestralView : UserControl
{
    public SemestralView()
    {
        InitializeComponent();
    }

    private void InfoAlumnoButton_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        var alumno = btn?.DataContext as Alumno;
        var vm = DataContext as MainViewModel;

        if (alumno == null || vm == null)
            return;

        var datosParciales =
            new Dictionary<string, (string calif, string estado, int faltas, int totalClases)>();

        var evaluaciones = new[] { "P1", "P2", "P3", "SEM" };

        string archivoActual = vm.ArchivoCompletoActual;

        foreach (var eval in evaluaciones)
        {
            string calif = alumno.Calificación[eval];

            string estado = "Sin evaluar";

            int faltas = 0;
            int totalClases = 0;

            string claveMateria = string.Empty;

            if (!string.IsNullOrWhiteSpace(archivoActual))
            {
                try
                {
                    claveMateria =
                        System.IO.Path
                            .GetFileNameWithoutExtension(archivoActual)
                            .Trim()
                            .Replace(' ', '_');
                }
                catch
                {
                }
            }

            string claveMateriaEval = $"{claveMateria}_{eval}";

            var materia =
                new ParcialJsonService()
                    .ObtenerMateria(claveMateriaEval);

            if (materia != null)
            {
                if (materia.Calificaciones.TryGetValue("$CONFIG$", out var config))
                {
                    totalClases =
                        config.TryGetValue("ClasesTotales", out var ct)
                            ? (int)ct
                            : 0;
                }

                if (materia.Calificaciones.TryGetValue(
                        alumno.Matricula,
                        out var capturas))
                {
                    faltas =
                        capturas.TryGetValue(
                            "__Inasistencias__",
                            out var f)
                            ? (int)f
                            : 0;
                }

                if (double.TryParse(calif, out double califNum))
                {
                    if (totalClases > 0)
                    {
                        int asistencias = totalClases - faltas;

                        double porcentajeAsistencia =
                            (double)asistencias /
                            totalClases *
                            100;

                        if (porcentajeAsistencia < 80)
                        {
                            estado = "Reprobado por faltas";
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
                (calif, estado, faltas, totalClases);
        }

        var infoWindow =
            new Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon
                .Views.Modals.InfoAlumnoWindow(
                    alumno,
                    datosParciales);

        infoWindow.Owner =
            Window.GetWindow(this);

        infoWindow.ShowDialog();
    }


    private void SemTextBox_PreviewTextInput(
        object sender,
        TextCompositionEventArgs e)
    {
        if (sender is not TextBox tb)
        {
            e.Handled = true;
            return;
        }

        char c =
            e.Text.Length > 0
                ? e.Text[0]
                : '\0';

        if (!char.IsDigit(c) && c != '.')
        {
            e.Handled = true;
            return;
        }

        string textoPropuesto =
            tb.Text
                .Remove(
                    tb.SelectionStart,
                    tb.SelectionLength)
                .Insert(
                    tb.SelectionStart,
                    e.Text);

        if (textoPropuesto.Count(ch => ch == '.') > 1)
        {
            e.Handled = true;
            return;
        }

        string s =
            textoPropuesto.Replace(',', '.');

        int idx = s.IndexOf('.');

        if (idx >= 0)
        {
            int decimals =
                s.Length - idx - 1;

            if (decimals > 1)
            {
                e.Handled = true;
                return;
            }
        }

        if (double.TryParse(
                s,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double val))
        {
            if (val < 0 || val > 10)
            {
                e.Handled = true;
                return;
            }
        }
    }


    private void SemTextBox_KeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        if (sender is not TextBox currentTextBox)
            return;

        /*
         * Solo avanzar si el campo actual está habilitado.
         */
        if (!currentTextBox.IsEnabled)
            return;

        e.Handled = true;

        /*
         * Obtenemos TODOS los TextBox SEM del ItemsControl.
         * Como el único TextBox editable de cada fila es SEM,
         * el siguiente TextBox corresponde al siguiente alumno.
         */
        var textBoxes =
            FindVisualChildren<TextBox>(
                AlumnosItemsControl)
            .ToList();

        int currentIndex =
            textBoxes.IndexOf(currentTextBox);

        if (currentIndex < 0)
            return;

        /*
         * Buscar el siguiente alumno que sí tenga derecho.
         */
        for (int i = currentIndex + 1;
             i < textBoxes.Count;
             i++)
        {
            var nextTextBox =
                textBoxes[i];

            if (!nextTextBox.IsEnabled)
                continue;

            nextTextBox.Focus();
            nextTextBox.SelectAll();

            return;
        }

        /*
         * No quedan alumnos con SEM editable.
         */
        MessageBox.Show(
            "Se evaluaron todos los alumnos.",
            "Fin de captura",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }


    private static IEnumerable<T> FindVisualChildren<T>(
        DependencyObject depObj)
        where T : DependencyObject
    {
        if (depObj == null)
            yield break;

        for (
            int i = 0;
            i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj);
            i++)
        {
            DependencyObject child =
                System.Windows.Media.VisualTreeHelper.GetChild(
                    depObj,
                    i);

            if (child is T t)
                yield return t;

            foreach (T childOfChild
                in FindVisualChildren<T>(child))
            {
                yield return childOfChild;
            }
        }
    }
} 