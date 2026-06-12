using System.Windows;
using System.Windows.Media.Imaging;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            Uri iconUri = new Uri("pack://application:,,,/logo.ico", UriKind.RelativeOrAbsolute);

            IconBitmapDecoder decoder = new IconBitmapDecoder(
                iconUri,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad
            );

            EventManager.RegisterClassHandler(
                typeof(Window),
                Window.LoadedEvent,
                new RoutedEventHandler((sender, args) =>
                {
                    if (sender is Window window)
                    {
                        window.Icon = decoder.Frames[0];
                    }
                })
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error aplicando icono global: {ex.Message}");
        }

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