using System.IO;
using System.Windows;
using System.Windows.Media;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.ViewModels;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DataContext = new MainViewModel();

        RenderOptions.SetBitmapScalingMode(
            this,
            BitmapScalingMode.Fant);

        //this.MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;
    }

    private void Configuracion_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        var login = new PasswordWindow
        {
            Owner = this
        };

        if (login.ShowDialog() == true)
        {
            var ventana =
                new ConfiguracionParcialesWindow(vm)
                {
                    Owner = this
                };

            ventana.ShowDialog();
        }
    }

    // ============================================================
    // ABRIR RESUMEN DE PORCENTAJES
    // ============================================================

    private void Porcentajes_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        string? rutaCap =
            vm.ArchivoCompletoActual;

        if (string.IsNullOrWhiteSpace(rutaCap) ||
            !File.Exists(rutaCap))
        {
            MessageBox.Show(
                "No hay un CAP cargado actualmente.",
                "Resumen de porcentajes",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        try
        {
            // ====================================================
            // OBTENER CLAVE DE MATERIA
            // ====================================================

            string nombreArchivo =
                Path.GetFileNameWithoutExtension(
                    rutaCap);

            if (string.IsNullOrWhiteSpace(
                    nombreArchivo))
            {
                MessageBox.Show(
                    "No se pudo determinar la clave de la materia.",
                    "Resumen de porcentajes",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            string claveMateria =
                nombreArchivo
                    .Trim()
                    .Replace(' ', '_');

            // ====================================================
            // CARGAR P1, P2 Y P3
            // ====================================================

            var jsonService =
                new ParcialJsonService();

            MateriaParcial? parcial1 =
                jsonService.ObtenerMateria(
                    $"{claveMateria}_P1");

            MateriaParcial? parcial2 =
                jsonService.ObtenerMateria(
                    $"{claveMateria}_P2");

            MateriaParcial? parcial3 =
                jsonService.ObtenerMateria(
                    $"{claveMateria}_P3");

            // ====================================================
            // ABRIR MODAL
            // ====================================================

            var ventana =
                new Modals.PorcentajesWindow(
                    parcial1,
                    parcial2,
                    parcial3)
                {
                    Owner = this
                };

            ventana.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudo abrir el resumen de porcentajes.\n\n{ex.Message}",
                "Resumen de porcentajes",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // ============================================================
    // MINIMIZAR
    // ============================================================

    private void Minimizar_Click(
        object sender,
        RoutedEventArgs e)
    {
        WindowState =
            WindowState.Minimized;
    }

    // ============================================================
    // CERRAR
    // ============================================================

    private void Cerrar_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }

    // ============================================================
    // ARRASTRAR VENTANA
    // ============================================================

    private void MainWindow_MouseLeftButtonDown(
        object sender,
        System.Windows.Input.MouseButtonEventArgs e)
    {
        DragMove();
    }
}