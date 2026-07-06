using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.ViewModels;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views;

public partial class ExtraView : UserControl
{
    public ExtraView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel mainVm)
        {
            if (mainVm.EvaluacionSeleccionada?.ToUpperInvariant() != "EXTRA")
            {
                mainVm.EvaluacionSeleccionada = "EXTRA";
            }
        }
    }

    private void TextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        var tb = sender as TextBox;
        if (tb == null) return;

        var alumno = tb.DataContext as Alumno;
        if (alumno != null && !alumno.TieneDerechoExtra) return;

        if (DataContext is MainViewModel mainVm && !mainVm.IsUpdatingProgrammatically)
        {
            mainVm.TieneCambios = true;
        }

        // LÓGICA DE DETENCIÓN ESTRICTA POR GRUPO
        var groupItem = FindVisualParent<GroupItem>(tb);
        if (groupItem != null)
        {
            var textBoxes = FindVisualChildren<TextBox>(groupItem).ToList();
            int index = textBoxes.IndexOf(tb);
            
            if (index >= 0 && index < textBoxes.Count - 1)
            {
                var nextTb = textBoxes[index + 1];
                nextTb.Focus();
                nextTb.SelectAll();
                e.Handled = true;
            }
        }
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var mainVm = DataContext as MainViewModel;
        if (mainVm != null && mainVm.IsUpdatingProgrammatically) return;

        if (sender is TextBox tb)
        {
            var alumno = tb.DataContext as Alumno;
            if (alumno != null && !alumno.TieneDerechoExtra) return;

            var change = e.Changes.FirstOrDefault();
            string textUpper = tb.Text.ToUpperInvariant();

            if (change != null)
            {
                // Si teclea N (agrega un caracter y queda "N") -> Autocompleta NP
                if (change.AddedLength > 0 && textUpper == "N")
                {
                    tb.Text = "NP";
                    tb.SelectionStart = 2; 
                    
                    if (alumno != null) alumno.Calificación["EXTRA"] = "NP"; 
                    
                    if (mainVm != null) 
                    {
                        mainVm.TieneCambios = true;
                        mainVm.ActualizarConteoEvaluadosExtra();
                    }
                    return;
                }
                
                // Si borra un caracter del "NP" y solo queda "N" -> Se limpia la celda
                if (change.RemovedLength > 0 && textUpper == "N")
                {
                    tb.Text = "";
                    
                    if (alumno != null) alumno.Calificación["EXTRA"] = "";
                    
                    if (mainVm != null) 
                    {
                        mainVm.TieneCambios = true;
                        mainVm.ActualizarConteoEvaluadosExtra();
                    }
                    return;
                }
            }

            // Normalización general si no se autocompletó nada arriba
            if (textUpper == "NP")
            {
                if (tb.Text != "NP")
                {
                    tb.Text = "NP";
                    tb.SelectionStart = 2;
                }
                if (alumno != null) alumno.Calificación["EXTRA"] = "NP";
            }
            else
            {
                string normalized = NormalizeToSingleDecimalRange(tb.Text);
                if (tb.Text != normalized && normalized != "NP")
                {
                    int sel = tb.SelectionStart;
                    tb.Text = normalized;
                    tb.SelectionStart = System.Math.Min(sel, tb.Text.Length);
                }
                if (alumno != null) alumno.Calificación["EXTRA"] = tb.Text;
            }
        }

        if (mainVm != null)
        {
            mainVm.TieneCambios = true;
            mainVm.ActualizarConteoEvaluadosExtra();
        }
    }

    private void WarningNoEvaluados_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel mainVm && mainVm.FaltanPorEvaluarExtra)
        {
            var modal = new Views.Modals.NoEvaluadosWindow(mainVm.ListaNoEvaluadosExtra);
            modal.Owner = Window.GetWindow(this);
            modal.ShowDialog();
        }
    }

    private static string NormalizeToSingleDecimalRange(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        if (input.Trim().ToUpperInvariant() == "NP") return "NP";

        string s = input.Trim().Replace(',', '.');
        var sb = new System.Text.StringBuilder();
        foreach (char c in s)
        {
            if (char.IsDigit(c) || c == '.') sb.Append(c);
        }
        s = sb.ToString();
        
        int firstDot = s.IndexOf('.');
        if (firstDot >= 0)
        {
            s = s.Substring(0, firstDot + 1) + s.Substring(firstDot + 1).Replace(".", "");
            int decimals = s.Length - firstDot - 1;
            if (decimals > 1) s = s.Substring(0, firstDot + 2);
        }

        if (double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val))
        {
            if (val < 0) val = 0;
            if (val > 10) val = 10;
            return val % 1 == 0 ? ((int)val).ToString(System.Globalization.CultureInfo.InvariantCulture) : val.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        }

        return s;
    }

    private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject parentObject = System.Windows.Media.VisualTreeHelper.GetParent(child);
        if (parentObject == null) return null;
        if (parentObject is T parent) return parent;
        return FindVisualParent<T>(parentObject);
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
    {
        if (depObj != null)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                if (child != null && child is T t)
                    yield return t;

                foreach (T childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }
    }
}