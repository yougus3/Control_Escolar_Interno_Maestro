using System.Windows;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _ = new ConfiguracionParcialesService().ObtenerConfiguracion();

        var mainWindow = new MainWindow();

        if ((SystemParameters.PrimaryScreenWidth == 1366 ||
             SystemParameters.PrimaryScreenWidth == 1360) &&
            SystemParameters.PrimaryScreenHeight == 768)
        {
            mainWindow.WindowState = WindowState.Maximized;
        }

        MainWindow = mainWindow;
        mainWindow.Show();
    }
}