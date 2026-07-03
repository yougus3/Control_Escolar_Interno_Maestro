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

    private static readonly Regex _numerosRegex = new("^[0-9]$", RegexOptions.Compiled);

    private void SemTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        // Allow only digits and control
        if (!_numerosRegex.IsMatch(e.Text))
        {
            e.Handled = true;
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
