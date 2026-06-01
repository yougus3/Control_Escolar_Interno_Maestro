using System.Windows;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.ViewModels;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void Configuracion_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        var login = new PasswordWindow
        {
            Owner = this
        };

        if (login.ShowDialog() == true)
        {
            var config = new ConfiguracionParcialesWindow
            {
                Owner = this
            };

            if (config.ShowDialog() == true)
            {
                vm.RecargarConfiguracionYArchivoActual();
            }
        }
    }
}