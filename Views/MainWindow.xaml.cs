using System.Windows;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.ViewModels;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // Asignamos el DataContext por código para evitar el error del compilador XAML
        this.DataContext = new MainViewModel(); 
    }
}