using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using QuestPDF.Infrastructure;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon;

public partial class App : Application
{
    protected override void OnStartup(
        StartupEventArgs e)
    {
        base.OnStartup(e);
        QuestPDF.Settings.License = LicenseType.Community;
        try
        {
            // ========================================================
            // ICONO GLOBAL
            // ========================================================

            Uri iconUri =
                new Uri(
                    "pack://application:,,,/logo.ico",
                    UriKind.RelativeOrAbsolute);

            IconBitmapDecoder decoder =
                new IconBitmapDecoder(
                    iconUri,
                    BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.OnLoad);

            EventManager.RegisterClassHandler(
                typeof(Window),
                Window.LoadedEvent,
                new RoutedEventHandler(
                    (sender, args) =>
                    {
                        if (sender is Window window)
                        {
                            window.Icon =
                                decoder.Frames[0];
                        }
                    }));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Error aplicando icono global: {ex.Message}");
        }

        // ========================================================
        // LICENCIA
        // ========================================================

        string dataFolder =
            Path.Combine(
                AppContext.BaseDirectory,
                "Data");

        string licensePath =
            Path.Combine(
                dataFolder,
                "LICENCIA_GMS.lic");

        // ========================================================
        // COMPROBAR QUE EXISTA LA LICENCIA
        // ========================================================

        if (!File.Exists(licensePath))
        {
            MostrarErrorLicencia(
                "No se encontró el archivo de licencia.\n\n" +
                "Coloca LICENCIA_GMS.lic dentro de la carpeta Data.");

            Shutdown();
            return;
        }

        // ========================================================
        // LEER LICENCIA
        // ========================================================

        string licencia;

        try
        {
            licencia =
                File.ReadAllText(
                    licensePath)
                .Trim();
        }
        catch (Exception ex)
        {
            MostrarErrorLicencia(
                "No se pudo leer el archivo de licencia.\n\n" +
                ex.Message);

            Shutdown();
            return;
        }

        if (string.IsNullOrWhiteSpace(licencia))
        {
            MostrarErrorLicencia(
                "El archivo de licencia está vacío.");

            Shutdown();
            return;
        }

        // ========================================================
        // MOSTRAR INFORMACIÓN TEMPORAL
        //
        // TODAVÍA NO SE VERIFICA LA FIRMA AQUÍ.
        // Primero estamos comprobando que CEIM pueda localizar
        // y leer correctamente LICENCIA_GMS.lic.
        // ========================================================

        System.Diagnostics.Debug.WriteLine(
            "======================================");

        System.Diagnostics.Debug.WriteLine(
            "LICENCIA GMS");

        System.Diagnostics.Debug.WriteLine(
            $"Ruta: {licensePath}");

        System.Diagnostics.Debug.WriteLine(
            $"Longitud: {licencia.Length}");

        System.Diagnostics.Debug.WriteLine(
            $"Contenido: {licencia}");

        System.Diagnostics.Debug.WriteLine(
            "======================================");

        // ========================================================
        // CONFIGURACIÓN
        // ========================================================

        _ = new ConfiguracionParcialesService()
            .ObtenerConfiguracion();

        // ========================================================
        // MAIN WINDOW
        // ========================================================

        var mainWindow =
            new MainWindow();

        /*
        if ((SystemParameters.PrimaryScreenWidth == 1366 ||
             SystemParameters.PrimaryScreenWidth == 1360) &&
            SystemParameters.PrimaryScreenHeight == 768)
        {
            mainWindow.WindowState =
                WindowState.Maximized;
        }
        */

        MainWindow =    
            mainWindow;

        mainWindow.Show();
    }

    private static void MostrarErrorLicencia(
        string mensaje)
    {
        MessageBox.Show(
            mensaje,
            "Licencia GMS",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}