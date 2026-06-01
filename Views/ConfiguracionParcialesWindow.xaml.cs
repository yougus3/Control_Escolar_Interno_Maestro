using System.Windows;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views;

public partial class ConfiguracionParcialesWindow : Window
{
    private readonly ConfiguracionParcialesService _servicio = new();
    private ConfiguracionParciales _configuracion = new();

    public ConfiguracionParcialesWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _configuracion = _servicio.ObtenerConfiguracion();

        chkCapturaDirecta.IsChecked = _configuracion.CapturaDirectaHabilitada;
        chkP1.IsChecked = _configuracion.Parcial1Habilitado;
        chkP2.IsChecked = _configuracion.Parcial2Habilitado;
        chkP3.IsChecked = _configuracion.Parcial3Habilitado;
        chkSEM.IsChecked = _configuracion.SemestralHabilitado;
    }

    private void Guardar_Click(object sender, RoutedEventArgs e)
    {
        _configuracion.CapturaDirectaHabilitada = chkCapturaDirecta.IsChecked == true;
        _configuracion.Parcial1Habilitado = chkP1.IsChecked == true;
        _configuracion.Parcial2Habilitado = chkP2.IsChecked == true;
        _configuracion.Parcial3Habilitado = chkP3.IsChecked == true;
        _configuracion.SemestralHabilitado = chkSEM.IsChecked == true;

        _servicio.GuardarConfiguracion(_configuracion);

        DialogResult = true;
        Close();
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}