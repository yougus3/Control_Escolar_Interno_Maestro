using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views.Modals
{
    public partial class InfoAlumnoWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // Propiedades básicas
        public string Matricula { get; set; }
        public string Nombre { get; set; }
        public string Grupo { get; set; }

        // Foto
        private string _fotoUrl = string.Empty;
        public string FotoUrl
        {
            get => _fotoUrl;
            set { _fotoUrl = value; OnPropertyChanged(); }
        }

        private bool _fotoCargada = false;
        public bool FotoCargada
        {
            get => _fotoCargada;
            set { _fotoCargada = value; OnPropertyChanged(); }
        }

        // P1
        public string P1Calif { get; set; } = "N/A";
        public string P1Asistencia { get; set; } = "N/A";
        public string P1Estado { get; set; } = "Sin evaluar";

        // P2
        public string P2Calif { get; set; } = "N/A";
        public string P2Asistencia { get; set; } = "N/A";
        public string P2Estado { get; set; } = "Sin evaluar";

        // P3
        public string P3Calif { get; set; } = "N/A";
        public string P3Asistencia { get; set; } = "N/A";
        public string P3Estado { get; set; } = "Sin evaluar";

        // SEM
        public string SEMCalif { get; set; } = "N/A";
        public string SEMEstado { get; set; } = "Sin evaluar";

        // Promedio
        public string PromedioFinal { get; set; } = "N/A";

        public InfoAlumnoWindow(Alumno alumno, Dictionary<string, (string calif, string estado, int faltas, int totalClases)> datosParciales)
        {
            InitializeComponent();

            Matricula = alumno.Matricula;
            Nombre = alumno.Nombre;
            Grupo = alumno.Grupo;

            // Cargar foto
            FotoUrl = $"https://www.prefecotemixco.edu.mx/fotos_alumno/{Matricula}.jpg";
            _ = CargarFotoAsync();

            // Cargar datos de parciales
            CargarDatosParciales(datosParciales);

            DataContext = this;
        }

        private async System.Threading.Tasks.Task CargarFotoAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                var response = await client.GetAsync(FotoUrl);
                FotoCargada = response.IsSuccessStatusCode;
            }
            catch
            {
                FotoCargada = false;
            }
        }

        private void CargarDatosParciales(Dictionary<string, (string calif, string estado, int faltas, int totalClases)> datos)
        {
            // P1
            if (datos.TryGetValue("P1", out var p1))
            {
                P1Calif = string.IsNullOrWhiteSpace(p1.calif) ? "N/A" : p1.calif;
                P1Asistencia = $"{p1.faltas} / {p1.totalClases} faltas";
                P1Estado = p1.estado;
            }

            // P2
            if (datos.TryGetValue("P2", out var p2))
            {
                P2Calif = string.IsNullOrWhiteSpace(p2.calif) ? "N/A" : p2.calif;
                P2Asistencia = $"{p2.faltas} / {p2.totalClases} faltas";
                P2Estado = p2.estado;
            }

            // P3
            if (datos.TryGetValue("P3", out var p3))
            {
                P3Calif = string.IsNullOrWhiteSpace(p3.calif) ? "N/A" : p3.calif;
                P3Asistencia = $"{p3.faltas} / {p3.totalClases} faltas";
                P3Estado = p3.estado;
            }

            // SEM
            if (datos.TryGetValue("SEM", out var sem))
            {
                SEMCalif = string.IsNullOrWhiteSpace(sem.calif) ? "N/A" : sem.calif;
                SEMEstado = sem.estado;
            }

            CalcularPromedioFinal();
        }

        private void CalcularPromedioFinal()
        {
            double suma = 0;
            int count = 0;

            if (double.TryParse(P1Calif, out double p1) && p1 >= 0) { suma += p1; count++; }
            if (double.TryParse(P2Calif, out double p2) && p2 >= 0) { suma += p2; count++; }
            if (double.TryParse(P3Calif, out double p3) && p3 >= 0) { suma += p3; count++; }

            if (count > 0)
            {
                double promedio = suma / count;
        
                // Truncar a 1 decimal (sin redondear)
                double promedioTruncado = Math.Truncate(promedio * 10) / 10;
        
                PromedioFinal = promedioTruncado.ToString("0.0");
            }
            else
            {
                PromedioFinal = "N/A";
            }
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}