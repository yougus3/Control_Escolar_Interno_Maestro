using System.Windows.Controls;
using System.Windows;
using System.Text.RegularExpressions;
using System.Globalization;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.ViewModels;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;
using System.Windows.Input;

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
        if (alumno == null || vm == null) return;

        var datosParciales = new Dictionary<string, (string calif, string estado, int faltas, int totalClases)>();
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
                try { claveMateria = System.IO.Path.GetFileNameWithoutExtension(archivoActual).Trim().Replace(' ', '_'); } catch { }
            }
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

        var infoWindow = new Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views.Modals.InfoAlumnoWindow(alumno, datosParciales);
        infoWindow.Owner = Window.GetWindow(this);
        infoWindow.ShowDialog();
    }

    private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        // Si deseas confirmar cambios inmediatamente
        if (e.EditAction == DataGridEditAction.Commit)
        {
            var dg = sender as DataGrid;
            dg.CommitEdit(DataGridEditingUnit.Row, true);
        }

        // Detect edits on SEM column and persist to local ParcialJsonService
        if (e.Row.Item is Alumno alumno)
        {
            // After edit, the binding updates; persist SEM for this alumno
            var vm = DataContext as MainViewModel;
            if (vm == null) return;

            // Determine clave materia desde archivo seleccionado
            string claveMateria = string.Empty;
            if (!string.IsNullOrWhiteSpace(vm.ArchivoCompletoActual))
            {
                try
                {
                    var nombre = System.IO.Path.GetFileNameWithoutExtension(vm.ArchivoCompletoActual);
                    if (!string.IsNullOrWhiteSpace(nombre)) claveMateria = nombre.Trim().Replace(' ', '_');
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(claveMateria) && !string.IsNullOrWhiteSpace(vm.ArchivoSeleccionado))
            {
                string texto = vm.ArchivoSeleccionado.Trim();
                int indexEspacio = texto.IndexOf(' ');
                if (indexEspacio <= 0) claveMateria = texto.Replace(' ', '_');
                else
                {
                    string clave = texto[..indexEspacio].Trim();
                    string nombre = texto[(indexEspacio + 1)..].Trim();
                    claveMateria = string.IsNullOrWhiteSpace(nombre) ? clave : $"{clave}_{nombre}";
                }
            }

            if (string.IsNullOrWhiteSpace(claveMateria)) return;

            var pj = new ParcialJsonService();
            var materia = pj.ObtenerMateria($"{claveMateria}_SEM");

            if (materia == null) materia = new MateriaParcial();

            // Ensure config present
            if (!materia.Calificaciones.ContainsKey("$CONFIG$"))
            {
                materia.Calificaciones["$CONFIG$"] = new System.Collections.Generic.Dictionary<string, double>(System.StringComparer.OrdinalIgnoreCase) { ["AsistenciaActiva"] = 0, ["ClasesTotales"] = 0 };
            }

            // Save SEM value for this alumno
            double semVal = 0.0;
            if (!double.TryParse(alumno.Calificación["SEM"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out semVal))
            {
                // leave as 0
            }

            if (!materia.Calificaciones.TryGetValue(alumno.Matricula, out var dict))
            {
                dict = new System.Collections.Generic.Dictionary<string, double>(System.StringComparer.OrdinalIgnoreCase);
            }

            dict["SEM"] = semVal;
            materia.Calificaciones[alumno.Matricula] = dict;

            pj.GuardarMateria($"{claveMateria}_SEM", materia);
        }
    }

    private void DataGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
    {
        // Enfocar el control editado y seleccionar texto
        if (e.EditingElement is TextBox tb)
        {
            tb.Focus();
            tb.SelectAll();

            // Quitar manejador previo para evitar duplicados
            tb.KeyDown -= EditingTextBox_KeyDown;
            tb.KeyDown += EditingTextBox_KeyDown;
        }
    }

    private void EditingTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            // Confirmar edición actual
            var tb = sender as TextBox;
            if (tb == null) return;

            var cell = FindParent<DataGridCell>(tb);
            var dg = FindParent<DataGrid>(tb);
            if (dg != null)
            {
                dg.CommitEdit(DataGridEditingUnit.Cell, true);
                dg.CommitEdit(DataGridEditingUnit.Row, true);

                // Mover a la siguiente fila y comenzar edición en la misma columna
                int colIndex = dg.CurrentCell.Column.DisplayIndex;
                int rowIndex = dg.Items.IndexOf(dg.CurrentItem);
                int nextRow = rowIndex + 1;
                if (nextRow < dg.Items.Count)
                {
                    dg.SelectedIndex = nextRow;
                    dg.CurrentCell = new DataGridCellInfo(dg.Items[nextRow], dg.Columns[colIndex]);
                    dg.ScrollIntoView(dg.CurrentItem);
                    dg.BeginEdit();
                }
            }
            e.Handled = true;
        }
    }

    private void SemTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        // Allow only digits and a single dot, and enforce range 0-10 with max 1 decimal visually
        if (!(sender is TextBox tb)) { e.Handled = true; return; }

        char c = e.Text.Length > 0 ? e.Text[0] : '\0';
        if (!char.IsDigit(c) && c != '.') { e.Handled = true; return; }

        string textoPropuesto = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength).Insert(tb.SelectionStart, e.Text);
        // allow empty or single dot only during typing
        if (textoPropuesto.Count(ch => ch == '.') > 1) { e.Handled = true; return; }

        string s = textoPropuesto.Replace(',', '.');

        // If there is a dot, limit decimals to 1
        int idx = s.IndexOf('.');
        if (idx >= 0)
        {
            int decimals = s.Length - idx - 1;
            if (decimals > 1) { e.Handled = true; return; }
        }

        if (double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val))
        {
            if (val < 0 || val > 10) { e.Handled = true; return; }
        }
    }

    private void SemTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            
            var request = new TraversalRequest(FocusNavigationDirection.Next);
            request.Wrapped = false; 

            if (sender is UIElement currentElement)
            {
                bool moved = currentElement.MoveFocus(request);
                
                if (!moved)
                {
                    MessageBox.Show("Se evaluaron todos los alumnos.", "Fin de captura", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }

    private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
    {
        DependencyObject? parent = child;
        while (parent != null)
        {
            if (parent is T typed)
                return typed;
            parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
        }
        return null;
    }
}