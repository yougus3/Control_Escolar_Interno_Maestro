using System.Windows;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views.Modals
{
    public partial class WarningWindow : Window
    {
        public WarningWindow(string mensaje)
        {
            InitializeComponent();
            txtMensaje.Text = mensaje;
        }

        private void Entendido_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}