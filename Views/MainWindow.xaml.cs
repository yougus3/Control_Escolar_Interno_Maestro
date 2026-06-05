using System.Windows;
using System.Windows.Media;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.ViewModels;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.Fant);
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
            var ventana = new ConfiguracionParcialesWindow(vm)
            {
                Owner = this
            };
    
            ventana.ShowDialog();
        }
    }

    private void Minimizar_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Cerrar_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}