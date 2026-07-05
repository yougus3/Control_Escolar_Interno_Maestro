using System.Windows;
using System.Windows.Controls;
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
}