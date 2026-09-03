using System;
using System.IO;
using System.Windows;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views;

public partial class PdfViewerWindow : Window
{
    private readonly string _rutaPdf;

    public PdfViewerWindow(
        string rutaPdf)
    {
        InitializeComponent();

        _rutaPdf =
            rutaPdf
            ?? string.Empty;

        TxtNombreArchivo.Text =
            Path.GetFileName(
                _rutaPdf);

        Loaded +=
            PdfViewerWindow_Loaded;
    }

    private async void PdfViewerWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(
                    _rutaPdf))
            {
                throw new InvalidOperationException(
                    "La ruta del PDF está vacía.");
            }

            if (!File.Exists(
                    _rutaPdf))
            {
                throw new FileNotFoundException(
                    "No se encontró el archivo PDF.",
                    _rutaPdf);
            }

            await PdfWebView
                .EnsureCoreWebView2Async();

            PdfWebView.Source =
                new Uri(
                    Path.GetFullPath(
                        _rutaPdf));
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudo mostrar el PDF:\n\n{ex.Message}",
                "Visor PDF",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Close();
        }
    }

    private void Cerrar_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}