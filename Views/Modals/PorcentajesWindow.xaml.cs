using System.Windows;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views.Modals
{
    public partial class PorcentajesWindow : Window
    {
        public PorcentajesWindow(
            double p1Porc, double p1Max,
            double p2Porc, double p2Max,
            double p3Porc, double p3Max)
        {
            InitializeComponent();

            TbP1Porc.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0}%", p1Porc);
            TbP1Max.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.##}", p1Max);

            TbP2Porc.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0}%", p2Porc);
            TbP2Max.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.##}", p2Max);

            TbP3Porc.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0}%", p3Porc);
            TbP3Max.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.##}", p3Max);

            double total = p1Porc + p2Porc + p3Porc;
            TbTotalPorc.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0}%", total);
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}

