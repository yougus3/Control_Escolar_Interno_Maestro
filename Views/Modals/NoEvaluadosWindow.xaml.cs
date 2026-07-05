using System.Collections.Generic;
using System.Windows;
using System.Linq;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.ViewModels;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views.Modals
{
    public partial class NoEvaluadosWindow : Window
    {
        public NoEvaluadosWindow(List<AlumnoFaltante> faltantes)
        {
            InitializeComponent();
            dgFaltantes.ItemsSource = faltantes;

            // Extraemos un pequeño sumario para el Title (Asumiendo que todos pertenecen a la misma materia en sesión)
            var primera = faltantes.FirstOrDefault();
            if (primera != null)
            {
                this.Title = $"Alumnos pendientes: {primera.Materia} - Grupo: {primera.Grupo}";
            }
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}