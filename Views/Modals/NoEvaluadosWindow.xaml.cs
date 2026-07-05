using System.Collections.Generic;
using System.Windows;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.ViewModels;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views.Modals
{
    public partial class NoEvaluadosWindow : Window
    {
        public NoEvaluadosWindow(List<AlumnoFaltante> faltantes)
        {
            InitializeComponent();
            dgFaltantes.ItemsSource = faltantes;
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}