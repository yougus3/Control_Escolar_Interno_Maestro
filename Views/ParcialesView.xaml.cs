using System;
using System.Collections.Generic;
using System.IO;
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

    private void PuntajeObtenido_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        // When user presses Enter on a puntaje textbox, advance to next activity or next student.
        var vm = DataContext as ParcialesViewModel;
        if (vm == null) return;

        // Find the TextBox and its data context (Actividad)
        var tb = sender as TextBox;
        if (tb == null) return;

        // mark as having changes
        vm.PrepararGuardado(); // ensure calculations and mark saved? We'll instead mark dirty below

        // Find index of current activity
        var actividad = tb.DataContext as ActividadParcialEditor;
        int actividadIndex = -1;
        if (actividad != null)
        {
            actividadIndex = vm.Actividades.IndexOf(actividad);
        }

        // If there is a next active activity for this student, move focus to it
        int nextIndex = -1;
        for (int i = actividadIndex + 1; i < vm.Actividades.Count; i++)
        {
            if (vm.Actividades[i].Activa)
            {
                nextIndex = i; break;
            }
        }

        if (nextIndex >= 0)
        {
            // Move focus to the corresponding TextBox in the items control
            var container = PuntajesItemsControl.ItemContainerGenerator.ContainerFromIndex(nextIndex) as FrameworkElement;
            if (container != null)
            {
                var nextTb = FindVisualChild<TextBox>(container);
                if (nextTb != null)
                {
                    nextTb.Focus();
                    nextTb.SelectAll();
                }
            }
            e.Handled = true;
            return;
        }

        // Otherwise, all active activities filled for this student — move to next student
        var vmMain = vm;
        var alumnos = vmMain.Alumnos;
        if (vmMain.AlumnoSeleccionado != null && alumnos.Any())
        {
            int idx = alumnos.IndexOf(vmMain.AlumnoSeleccionado);
            if (idx < alumnos.Count - 1)
            {
                vmMain.AlumnoSeleccionado = alumnos[idx + 1];
                // set focus to first active activity textbox of new student
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    for (int i = 0; i < vmMain.Actividades.Count; i++)
                    {
                        if (vmMain.Actividades[i].Activa)
                        {
                            var container = PuntajesItemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                            if (container != null)
                            {
                                var tb2 = FindVisualChild<TextBox>(container);
                                if (tb2 != null)
                                {
                                    tb2.Focus(); tb2.SelectAll(); break;
                                }
                            }
                        }
                    }
                }));
            }
            else
            {
                // Last student evaluated
                var info = new MessageBoxResult();
                MessageBox.Show("Se evaluaron todos los alumnos.", "Fin de captura", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        e.Handled = true;
    }

    private void Puntaje_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (DataContext is ParcialesViewModel vm)
        {
            vm.MarkUserEdited();
        }
    }

    // Helper to find child control of specific type
    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) return null;
        int children = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < children; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T t) return t;
            var result = FindVisualChild<T>(child);
            if (result != null) return result;
        }
        return null;
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel mainVm)
        {
            DataContext = mainVm.ParcialesVm;
        }
    }

    private void Numeros_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            string textoPropuesto = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength)
                .Insert(textBox.SelectionStart, e.Text);
            Regex regex = new Regex(@"^[0-9]*\.?[0-9]*$");
            e.Handled = !regex.IsMatch(textoPropuesto);
        }
    }

    private void Numeros_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            string textoAPegar = (string)e.DataObject.GetData(typeof(string));
            if (sender is TextBox textBox)
            {
                string textoPropuesto = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength)
                    .Insert(textBox.SelectionStart, textoAPegar);
                Regex regex = new Regex(@"^[0-9]*\.?[0-9]*$");
                if (!regex.IsMatch(textoPropuesto))
                {
                    e.CancelCommand();
                }
            }
        }
        else
        {
            e.CancelCommand();
        }
    }

    private void Alumnos_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem != null)
        {
            listBox.ScrollIntoView(listBox.SelectedItem);
        }
    }

    private void NumerosEnteros_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        Regex regex = new Regex("[^0-9]+");
        e.Handled = regex.IsMatch(e.Text);
    }

    private void ActividadProp_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (DataContext is ParcialesViewModel vm)
        {
            vm.MarkUserEdited();
        }
    }

    private void ActividadActiva_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ParcialesViewModel vm)
        {
            vm.MarkUserEdited();
        }
    }

    private void PapelButton_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var alumno = button?.Tag as Alumno;
        var viewModel = DataContext as ParcialesViewModel;

        if (alumno != null && viewModel?.MainVm != null)
        {
            var datosParciales = new Dictionary<string, (string calif, string estado, int faltas, int totalClases)>();
            var evaluaciones = new[] { "P1", "P2", "P3", "SEM" };
            string archivoActual = viewModel.MainVm.ArchivoCompletoActual;

            foreach (var eval in evaluaciones)
            {
                string calif = alumno.Calificación[eval];
                string estado = "Sin evaluar";
                int faltas = 0;
                int totalClases = 0;

                string claveMateria = ObtenerClaveMateriaDesdeNombreArchivo(archivoActual);
                string claveMateriaEval = $"{claveMateria}_{eval}";
                var materia = new ParcialJsonService().ObtenerMateria(claveMateriaEval);

                if (materia != null)
                {
                    if (materia.Calificaciones.TryGetValue("$CONFIG$", out var config))
                    {
                        totalClases = config.TryGetValue("ClasesTotales", out var ct) ? (int)ct : 0;
                    }

                    if (materia.Calificaciones.TryGetValue(alumno.Matricula, out var capturas))
                    {
                        faltas = capturas.TryGetValue("__Inasistencias__", out var f) ? (int)f : 0;
                    }

                    if (double.TryParse(calif, out double califNum))
                    {
                        if (totalClases > 0)
                        {
                            int asistencias = totalClases - faltas;
                            double porcentajeAsistencia = (double)asistencias / totalClases * 100;
                            if (porcentajeAsistencia < 80)
                                estado = "Reprobado por faltas";
                            else
                                estado = califNum >= 7.0 ? "Aprobado" : "Reprobado";
                        }
                        else
                        {
                            estado = califNum >= 7.0 ? "Aprobado" : "Reprobado";
                        }
                    }
                }

                datosParciales[eval] = (calif, estado, faltas, totalClases);
            }

            var infoWindow = new InfoAlumnoWindow(alumno, datosParciales);
            infoWindow.Owner = Window.GetWindow(this);
            infoWindow.ShowDialog();
        }
    }

    private string ObtenerClaveMateriaDesdeNombreArchivo(string? rutaCompleta)
    {
        if (string.IsNullOrWhiteSpace(rutaCompleta)) return string.Empty;
        try
        {
            var nombre = Path.GetFileNameWithoutExtension(rutaCompleta);
            return string.IsNullOrWhiteSpace(nombre) ? string.Empty : nombre.Trim().Replace(' ', '_');
        }
        catch
        {
            return string.Empty;
        }
    }
}