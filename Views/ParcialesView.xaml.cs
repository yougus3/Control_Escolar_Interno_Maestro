using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.ViewModels;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views;

public partial class ParcialesView : UserControl
{
    public ParcialesView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel mainVm)
        {
            DataContext = mainVm.ParcialesVm;
        }
    }

    // Valida la escritura en tiempo real (Permite números y un solo punto decimal)
    private void Numeros_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            // Simulamos cómo quedaría el texto final si aceptamos el nuevo carácter
            string textoPropuesto = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength)
                                                .Insert(textBox.SelectionStart, e.Text);

            // Regex que solo acepta números enteros o decimales con UN solo punto (ej: "9", "9.", "9.5")
            Regex regex = new Regex(@"^[0-9]*\.?[0-9]*$");
            e.Handled = !regex.IsMatch(textoPropuesto);
        }
    }

    // Valida que al pegar con Ctrl+V también se cumpla la regla del decimal
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
    
    private void Alumnos_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // Verificamos que el emisor sea un ListBox y que realmente haya un elemento seleccionado
        if (sender is ListBox listBox && listBox.SelectedItem != null)
        {
            // Forzamos al ListBox a hacer scroll hasta el elemento seleccionado
            listBox.ScrollIntoView(listBox.SelectedItem);
        }
    }
}